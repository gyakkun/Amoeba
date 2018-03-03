using Amoeba.Rpc;
using Ionic.Zip;
using Nett;
using Omnius.Base;
using Omnius.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Amoeba.Interface
{
    class SetupManager : ManagerBase
    {
        private Mutex _mutex;
        private Process _process;

        private readonly object _lockObject = new object();
        private volatile bool _isDisposed;

        static class NativeMethods
        {
            [Flags]
            public enum ExecutionState : uint
            {
                Null = 0,
                SystemRequired = 1,
                DisplayRequired = 2,
                Continuous = 0x80000000,
            }

            [DllImport("kernel32.dll")]
            public extern static ExecutionState SetThreadExecutionState(ExecutionState esFlags);
        }

        public bool Run()
        {
            try
            {
                // スリープを禁止する。
                NativeMethods.SetThreadExecutionState(NativeMethods.ExecutionState.Continuous);

                // カレントディレクトリをexeと同じディレクトリパスへ変更。
                Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

                // ハンドルしていない例外をログ出力させる。
                Thread.GetDomain().UnhandledException += this.Program_UnhandledException;

                string sessionId = NetworkConverter.ToHexString(Sha256.Compute(Path.GetFullPath(Assembly.GetEntryAssembly().Location)));

                // 多重起動防止
                {
                    _mutex = new Mutex(false, sessionId);

                    if (!_mutex.WaitOne(0))
                    {
                        return false;
                    }
                }

                // 既定のフォルダを作成する。
                {
                    foreach (var propertyInfo in typeof(AmoebaEnvironment.EnvironmentPaths).GetProperties())
                    {
                        string path = propertyInfo.GetValue(AmoebaEnvironment.Paths) as string;
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    }
                }

                // Tempフォルダを環境変数に登録。
                {
                    // Tempフォルダ内を掃除。
                    try
                    {
                        foreach (string path in Directory.GetFiles(AmoebaEnvironment.Paths.TempDirectoryPath, "*", SearchOption.AllDirectories))
                        {
                            File.Delete(path);
                        }

                        foreach (string path in Directory.GetDirectories(AmoebaEnvironment.Paths.TempDirectoryPath, "*", SearchOption.AllDirectories))
                        {
                            Directory.Delete(path, true);
                        }
                    }
                    catch (Exception)
                    {

                    }

                    Environment.SetEnvironmentVariable("TMP", Path.GetFullPath(AmoebaEnvironment.Paths.TempDirectoryPath), EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("TEMP", Path.GetFullPath(AmoebaEnvironment.Paths.TempDirectoryPath), EnvironmentVariableTarget.Process);
                }

                // ログファイルを設定する。
                this.Setting_Log();

                // アップデート
                {
                    // 一時的に作成された"Amoeba.Update.exe"を削除する。
                    {
                        string tempUpdateExeFilePath = Path.Combine(AmoebaEnvironment.Paths.WorkDirectoryPath, "Amoeba.Update.exe");

                        if (File.Exists(tempUpdateExeFilePath))
                        {
                            File.Delete(tempUpdateExeFilePath);
                        }
                    }

                    if (Directory.Exists(AmoebaEnvironment.Paths.UpdateDirectoryPath))
                    {
                        string zipFilePath = null;

                        // 最新のバージョンのzipを検索。
                        {
                            var map = new Dictionary<string, Version>();
                            var regex = new Regex(@"Amoeba.+?((\d*)\.(\d*)\.(\d*)).*?\.zip", RegexOptions.Compiled);

                            foreach (string path in Directory.GetFiles(AmoebaEnvironment.Paths.UpdateDirectoryPath))
                            {
                                var match = regex.Match(Path.GetFileName(path));
                                if (!match.Success) continue;

                                var version = new Version(match.Groups[1].Value);
                                if (version < AmoebaEnvironment.Version) continue;

                                map.Add(path, version);
                            }

                            if (map.Count > 0)
                            {
                                var sortedList = map.ToList();
                                sortedList.Sort((x, y) => y.Value.CompareTo(x.Value));

                                zipFilePath = sortedList.First().Key;
                            }
                        }

                        if (zipFilePath != null)
                        {
                            string tempUpdateDirectoryPath = Path.Combine(AmoebaEnvironment.Paths.WorkDirectoryPath, "Update");

                            if (Directory.Exists(tempUpdateDirectoryPath))
                            {
                                Directory.Delete(tempUpdateDirectoryPath, true);
                            }

                            using (var zipfile = new ZipFile(zipFilePath))
                            {
                                zipfile.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
                                zipfile.ExtractAll(tempUpdateDirectoryPath);
                            }

                            if (File.Exists(zipFilePath))
                            {
                                File.Delete(zipFilePath);
                            }

                            string tempUpdateExeFilePath = Path.Combine(AmoebaEnvironment.Paths.WorkDirectoryPath, "Amoeba.Update.exe");

                            File.Copy("Amoeba.Update.exe", tempUpdateExeFilePath);

                            var startInfo = new ProcessStartInfo();
                            startInfo.FileName = Path.GetFullPath(tempUpdateExeFilePath);
                            startInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\"",
                                sessionId,
                                Path.GetFullPath(Path.Combine(tempUpdateDirectoryPath, "Core")),
                                Path.GetFullPath(AmoebaEnvironment.Paths.CoreDirectoryPath),
                                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Amoeba.Interface.exe")));
                            startInfo.WorkingDirectory = Path.GetFullPath(Path.GetDirectoryName(tempUpdateExeFilePath));

                            Process.Start(startInfo);

                            return false;
                        }
                    }
                }

                // マイグレーション
                {
                    if (AmoebaEnvironment.Config.Version <= new Version(5, 0, 60))
                    {
                        try
                        {
                            var basePath = Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, @"Service\Core\Cache");

                            if (!Directory.Exists(Path.Combine(basePath, "Blocks")))
                            {
                                Directory.CreateDirectory(Path.Combine(basePath, "Blocks"));
                            }

                            var renameList = new List<(string oldPath, string newPath)>();
                            renameList.Add((@"CacheInfos.json.gz", @"ContentInfos.json.gz"));
                            renameList.Add((@"Size.json.gz", @"Blocks\Size.json.gz"));
                            renameList.Add((@"ClusterIndex.json.gz", @"Blocks\ClusterIndex.json.gz"));

                            foreach (var (oldPath, newPath) in renameList)
                            {
                                if (File.Exists(Path.Combine(basePath, newPath))
                                    || !File.Exists(Path.Combine(basePath, oldPath))) continue;

                                File.Copy(Path.Combine(basePath, oldPath), Path.Combine(basePath, newPath));
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }
                }

#if !DEBUG
                // デーモンプロセス起動。
                {
                    var daemonExeFilePath = Path.Combine(AmoebaEnvironment.Paths.DaemonDirectoryPath, "Amoeba.Daemon.exe");
                    var daemonConfigFilePath = Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "Daemon.toml");

                    if (!File.Exists(daemonConfigFilePath))
                    {
                        // 「Amoeba/Core/Daemon」のような階層を想定。
                        var basePath = "../../";

                        var config = new DaemonConfig(
                            new Version(0, 0, 0),
                            new DaemonConfig.CommunicationConfig("tcp:127.0.0.1:4040"),
                            new DaemonConfig.CacheConfig(Path.Combine(basePath, "Config", "Cache.blocks")),
                            new DaemonConfig.PathsConfig(
                                Path.Combine(basePath, "Temp"),
                                Path.Combine(basePath, "Config", "Service"),
                                Path.Combine(basePath, "Log")));

                        var tomlSettings = TomlSettings.Create(builder => builder
                            .ConfigureType<Version>(type => type
                                .WithConversionFor<TomlString>(convert => convert
                                    .ToToml(tt => tt.ToString())
                                    .FromToml(ft => Version.Parse(ft.Value)))));

                        Toml.WriteFile(config, daemonConfigFilePath, tomlSettings);
                    }

                    var startInfo = new ProcessStartInfo();
                    startInfo.FileName = daemonExeFilePath;
                    startInfo.Arguments = string.Format("-c \"{0}\"", daemonConfigFilePath);
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = false;

                    try
                    {
                        _process = Process.Start(startInfo);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
#endif

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                return false;
            }
        }

        private void Program_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception == null) return;

            Log.Error(exception);
        }

        private void Setting_Log()
        {
            var now = DateTime.Now;
            string logFilePath = null;
            bool isHeaderWriten = false;

            for (int i = 0; i < 1024; i++)
            {
                if (i == 0)
                {
                    logFilePath = Path.Combine(AmoebaEnvironment.Paths.LogDirectoryPath,
                        string.Format("Interface_{0}.txt", now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                }
                else
                {
                    logFilePath = Path.Combine(AmoebaEnvironment.Paths.LogDirectoryPath,
                        string.Format("Interface_{0}.({1}).txt", now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo), i));
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

                Exception exception = e.Exception;

                while (exception != null)
                {
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    sb.AppendLine(string.Format("Exception:\t\t{0}", exception.GetType().ToString()));
                    if (!string.IsNullOrWhiteSpace(exception.Message)) sb.AppendLine(string.Format("Message:\t\t{0}", exception.Message));
                    if (!string.IsNullOrWhiteSpace(exception.StackTrace)) sb.AppendLine(string.Format("StackTrace:\t\t{0}", exception.StackTrace));

                    exception = exception.InnerException;
                }

                sb.AppendLine();

                return sb.ToString();
            }
        }

        private static string GetMachineInfomation()
        {
            return string.Format(
                "OS:\t\t{0}\r\n" +
                ".NET Framework:\t{1}", System.Runtime.InteropServices.RuntimeInformation.OSDescription, Environment.Version);
        }

        [DataContract]
        public class InterfaceConfig
        {
            public InterfaceConfig() { }

            public InterfaceConfig(Version version, CommunicationConfig communication)
            {
                this.Version = version;
                this.Communication = communication;
            }

            [DataMember(Name = nameof(Version))]
            public Version Version { get; private set; }

            [DataMember(Name = nameof(Communication))]
            public CommunicationConfig Communication { get; private set; }

            [DataContract]
            public class CommunicationConfig
            {
                public CommunicationConfig() { }

                public CommunicationConfig(string targetUri)
                {
                    this.TargetUri = targetUri;
                }

                [DataMember(Name = nameof(TargetUri))]
                public string TargetUri { get; private set; }
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                if (_process != null)
                {
                    _process.Dispose();
                    _process = null;
                }
            }
        }
    }
}
