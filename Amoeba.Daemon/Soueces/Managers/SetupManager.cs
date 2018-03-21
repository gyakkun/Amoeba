using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Amoeba.Rpc;
using Amoeba.Service;
using CommandLine;
using Nett;
using Omnius.Base;

namespace Amoeba.Daemon
{
    sealed class SetupManager : ManagerBase
    {
        private readonly object _lockObject = new object();
        private volatile bool _isDisposed;

        public void Run()
        {
            // カレントディレクトリをexeと同じディレクトリパスへ変更。
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            // ハンドルしていない例外をログ出力させる。
            AppDomain.CurrentDomain.UnhandledException += this.Program_UnhandledException;
            Thread.GetDomain().UnhandledException += this.Program_UnhandledException;

            // コマンドライン引数を解析。
            var options = CommandLine.Parser.Default.ParseArguments<AmoebaDaemonOptions>(Environment.GetCommandLineArgs())
                .MapResult(
                    (AmoebaDaemonOptions x) => x,
                    errs => null);
            if (options == null) return;

            // Tomlファイルを読み込み。
            DaemonConfig config = null;
            {
#if !DEBUG
                if (File.Exists(options.ConfigFilePath))
                {
                    var tomlSettings = TomlSettings.Create(builder => builder
                        .ConfigureType<Version>(type => type
                            .WithConversionFor<TomlString>(convert => convert
                                .ToToml(tt => tt.ToString())
                                .FromToml(ft => Version.Parse(ft.Value)))));

                    config = Toml.ReadFile<DaemonConfig>(options.ConfigFilePath, tomlSettings);
                }
#else
                var basePath = "../../";

                config = new DaemonConfig(
                    new Version(0, 0, 0),
                    new DaemonConfig.CommunicationConfig("tcp:127.0.0.1:4040"),
                    new DaemonConfig.CacheConfig(Path.Combine("E:", "Test", "Cache.blocks")),
                    new DaemonConfig.PathsConfig(
                        Path.Combine(basePath, "Temp"),
                        Path.Combine(basePath, "Config", "Service"),
                        Path.Combine(basePath, "Log")));
#endif
            }
            if (config == null) return;

            // 既定のフォルダを作成する。
            {
                foreach (var propertyInfo in typeof(DaemonConfig.PathsConfig).GetProperties())
                {
                    string path = propertyInfo.GetValue(config.Paths) as string;
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                }
            }

            // Tempフォルダを環境変数に登録。
            {
                // Tempフォルダ内を掃除。
                try
                {
                    foreach (string path in Directory.GetFiles(config.Paths.TempDirectoryPath, "*", SearchOption.AllDirectories))
                    {
                        File.Delete(path);
                    }

                    foreach (string path in Directory.GetDirectories(config.Paths.TempDirectoryPath, "*", SearchOption.AllDirectories))
                    {
                        Directory.Delete(path, true);
                    }
                }
                catch (Exception)
                {

                }

                Environment.SetEnvironmentVariable("TMP", Path.GetFullPath(config.Paths.TempDirectoryPath), EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("TEMP", Path.GetFullPath(config.Paths.TempDirectoryPath), EnvironmentVariableTarget.Process);
            }

            // ログファイルを設定する。
            this.Setting_Log(config);

            // サービス開始。
            try
            {
                using (var bufferManager = new BufferManager(1024 * 1024 * 1024))
                using (var serviceManager = new ServiceManager(config.Paths.ConfigDirectoryPath, config.Cache.BlocksFilePath, bufferManager))
                {
                    IPEndPoint endpoint;
                    {
                        var info = UriUtils.Parse(config.Communication.ListenUri);
                        endpoint = new IPEndPoint(IPAddress.Parse(info.GetValue<string>("Address")), info.GetValue<int>("Port"));
                    }

                    var tcpListener = new TcpListener(endpoint);
                    tcpListener.Start();

                    using (var socket = tcpListener.AcceptSocket())
                    using (var server = new AmoebaDaemonManager<ServiceManager>(socket, serviceManager, bufferManager))
                    {
                        try
                        {
                            server.Watch();
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);

                            Console.WriteLine(e.Message);
                        }
                    }

                    tcpListener.Stop();
                    tcpListener.Server.Dispose();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);

                Console.WriteLine(e.Message);
            }
        }

        private void Program_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception == null) return;

            Log.Error(exception);
        }

        private void Setting_Log(DaemonConfig config)
        {
            var now = DateTime.Now;
            string logFilePath = null;
            bool isHeaderWriten = false;

            for (int i = 0; i < 1024; i++)
            {
                if (i == 0)
                {
                    logFilePath = Path.Combine(config.Paths.LogDirectoryPath,
                        string.Format("Daemon_{0}.txt", now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                }
                else
                {
                    logFilePath = Path.Combine(config.Paths.LogDirectoryPath,
                        string.Format("Daemon_{0}.({1}).txt", now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo), i));
                }

                if (!File.Exists(logFilePath)) break;
            }

            if (logFilePath == null) return;

            Log.MessageEvent += (sender, e) =>
            {
                if (e.Level == LogMessageLevel.Information) return;
#if !DEBUG
                if (e.Level == LogMessageLevel.Debug) return;
#endif

                lock (_lockObject)
                {
                    try
                    {
                        using (var writer = new StreamWriter(logFilePath, true, new UTF8Encoding(false)))
                        {
                            if (!isHeaderWriten)
                            {
                                writer.WriteLine(GetMachineInfomation());
                                isHeaderWriten = true;
                            }

                            writer.WriteLine(MessageToString(e));
                            writer.Flush();
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            };

            Log.ExceptionEvent += (sender, e) =>
            {
                if (e.Level == LogMessageLevel.Information) return;
#if !DEBUG
                if (e.Level == LogMessageLevel.Debug) return;
#endif

                lock (_lockObject)
                {
                    try
                    {
                        using (var writer = new StreamWriter(logFilePath, true, new UTF8Encoding(false)))
                        {
                            if (!isHeaderWriten)
                            {
                                writer.WriteLine(GetMachineInfomation());
                                isHeaderWriten = true;
                            }

                            writer.WriteLine(ExceptionToString(e));
                            writer.Flush();
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            };

            string MessageToString(LogMessageEventArgs e)
            {
                var sb = new StringBuilder();
                sb.AppendLine("--------------------------------------------------------------------------------");
                sb.AppendLine();
                sb.AppendLine(string.Format("Time:\t\t{0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));
                sb.AppendLine(string.Format("Level:\t\t{0}", e.Level));
                sb.AppendLine(string.Format("Message:\t\t{0}", e.Message));

                sb.AppendLine();

                return sb.ToString();
            }

            string ExceptionToString(LogExceptionEventArgs e)
            {
                var sb = new StringBuilder();
                sb.AppendLine("--------------------------------------------------------------------------------");
                sb.AppendLine(string.Format("Time:\t\t{0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));
                sb.AppendLine(string.Format("Level:\t\t{0}", e.Level));

                var list = new List<Exception>();

                if (e.Exception is AggregateException aggregateException)
                {
                    list.AddRange(aggregateException.InnerExceptions);
                }
                else
                {
                    var exception = e.Exception;

                    while (exception != null)
                    {
                        list.Add(exception);

                        exception = exception.InnerException;
                    }
                }

                foreach (var exception in list)
                {
                    try
                    {
                        sb.AppendLine("--------------------------------------------------------------------------------");
                        sb.AppendLine(string.Format("Exception:\t\t{0}", exception.GetType().ToString()));
                        if (!string.IsNullOrWhiteSpace(exception.Message)) sb.AppendLine(string.Format("Message:\t\t{0}", exception.Message));
                        if (!string.IsNullOrWhiteSpace(exception.StackTrace)) sb.AppendLine(string.Format("StackTrace:\t\t{0}", exception.StackTrace));
                    }
                    catch (Exception)
                    {

                    }
                }

                sb.AppendLine();

                return sb.ToString();
            }
        }

        private static string GetMachineInfomation()
        {
            return string.Format(
                "OS:\t\t{0}\r\n" +
                ".NET Core:\t{1}", System.Runtime.InteropServices.RuntimeInformation.OSDescription, Environment.Version);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {

            }
        }
    }
}
