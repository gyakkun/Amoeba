using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Ionic.Zip;
using Omnius.Base;
using Omnius.Security;

namespace Amoeba.Interface
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex _mutex;

        private List<Process> _processList = new List<Process>();

        public App()
        {
            NativeMethods.SetThreadExecutionState(NativeMethods.ExecutionState.Continuous);

            CryptoConfig.AddAlgorithm(typeof(SHA256Cng),
                "SHA256",
                "SHA256Cng",
                "System.Security.Cryptography.SHA256",
                "System.Security.Cryptography.SHA256Cng");

            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            Thread.GetDomain().UnhandledException += this.App_UnhandledException;

            this.Setting_Log();
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception == null) return;

            Log.Error(exception);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception);
        }

        private void Setting_Log()
        {
            string logPath = null;
            bool isHeaderWrite = true;

            for (int i = 0; i < 1024; i++)
            {
                if (i == 0)
                {
                    logPath = Path.Combine(AmoebaEnvironment.Paths.LogPath, string.Format("{0}.txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                }
                else
                {
                    logPath = Path.Combine(AmoebaEnvironment.Paths.LogPath, string.Format("{0}.({1}).txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo), i));
                }

                if (!File.Exists(logPath)) break;
            }

            if (logPath == null) return;

            Log.LogEvent += (object sender, LogEventArgs e) =>
            {
                lock (logPath)
                {
                    try
                    {
                        if (e.MessageLevel == LogMessageLevel.Error || e.MessageLevel == LogMessageLevel.Warning)
                        {
                            using (var writer = new StreamWriter(logPath, true, new UTF8Encoding(false)))
                            {
                                if (isHeaderWrite)
                                {
                                    writer.WriteLine(this.GetMachineInfomation());
                                    isHeaderWrite = false;
                                }

                                writer.WriteLine(string.Format(
                                    "\r\n--------------------------------------------------------------------------------\r\n\r\n" +
                                    "Time:\t\t{0}\r\n" +
                                    "Level:\t\t{1}\r\n" +
                                    "{2}",
                                    DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), e.MessageLevel, e.Message));
                                writer.Flush();
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            };
        }

        private string GetMachineInfomation()
        {
            var osInfo = Environment.OSVersion;
            string osName = "";

            if (osInfo.Platform == PlatformID.Win32NT)
            {
                if (osInfo.Version.Major == 4)
                {
                    osName = "Windows NT 4.0";
                }
                else if (osInfo.Version.Major == 5)
                {
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osName = "Windows 2000";
                            break;

                        case 1:
                            osName = "Windows XP";
                            break;

                        case 2:
                            osName = "Windows Server 2003";
                            break;
                    }
                }
                else if (osInfo.Version.Major == 6)
                {
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osName = "Windows Vista";
                            break;

                        case 1:
                            osName = "Windows 7";
                            break;

                        case 2:
                            osName = "Windows 8";
                            break;

                        case 3:
                            osName = "Windows 8.1";
                            break;
                    }
                }
                else if (osInfo.Version.Major == 10)
                {
                    osName = "Windows 10";
                }
            }
            else if (osInfo.Platform == PlatformID.WinCE)
            {
                osName = "Windows CE";
            }
            else if (osInfo.Platform == PlatformID.MacOSX)
            {
                osName = "MacOSX";
            }
            else if (osInfo.Platform == PlatformID.Unix)
            {
                osName = "Unix";
            }

            return string.Format(
                "Amoeba:\t\t{0}\r\n" +
                "OS:\t\t{1} ({2})\r\n" +
                ".NET Framework:\t{3}", AmoebaEnvironment.Version.ToString(3), osName, osInfo.VersionString, Environment.Version);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                string sessionId = NetworkConverter.ToHexString(Sha256.ComputeHash(Path.GetFullPath(Assembly.GetEntryAssembly().Location)));

                // 多重起動防止
                {
                    _mutex = new Mutex(false, sessionId);

                    if (!_mutex.WaitOne(0))
                    {
                        this.Shutdown();

                        return;
                    }
                }

                this.KillProcesses();

                // アップデート
                {
                    // 一時的に作成された"Amoeba.Update.exe"を削除する。
                    {
                        string tempUpdateExeFilePath = Path.Combine(AmoebaEnvironment.Paths.WorkPath, "Amoeba.Update.exe");

                        if (File.Exists(tempUpdateExeFilePath))
                        {
                            File.Delete(tempUpdateExeFilePath);
                        }
                    }

                    if (Directory.Exists(AmoebaEnvironment.Paths.UpdatePath))
                    {
                        string zipFilePath = null;

                        // 最新のバージョンのzipを検索。
                        {
                            var map = new Dictionary<string, Version>();
                            var regex = new Regex(@"Amoeba.+?((\d*)\.(\d*)\.(\d*)).*?\.zip", RegexOptions.Compiled);

                            foreach (string path in Directory.GetFiles(AmoebaEnvironment.Paths.UpdatePath))
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
                            string tempUpdateDirectoryPath = Path.Combine(AmoebaEnvironment.Paths.WorkPath, "Update");

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

                            string tempUpdateExeFilePath = Path.Combine(AmoebaEnvironment.Paths.WorkPath, "Amoeba.Update.exe");

                            File.Copy("Amoeba.Update.exe", tempUpdateExeFilePath);

                            var startInfo = new ProcessStartInfo();
                            startInfo.FileName = Path.GetFullPath(tempUpdateExeFilePath);
                            startInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\"",
                                sessionId,
                                Path.Combine(tempUpdateDirectoryPath, "Core"),
                                Directory.GetCurrentDirectory(),
                                Path.Combine(Directory.GetCurrentDirectory(), "Amoeba.Interface.exe"));
                            startInfo.WorkingDirectory = Path.GetFullPath(Path.GetDirectoryName(tempUpdateExeFilePath));

                            Process.Start(startInfo);

                            this.Shutdown();

                            return;
                        }
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
                        foreach (string path in Directory.GetFiles(AmoebaEnvironment.Paths.TempPath, "*", SearchOption.AllDirectories))
                        {
                            File.Delete(path);
                        }

                        foreach (string path in Directory.GetDirectories(AmoebaEnvironment.Paths.TempPath, "*", SearchOption.AllDirectories))
                        {
                            Directory.Delete(path, true);
                        }
                    }
                    catch (Exception)
                    {

                    }

                    Environment.SetEnvironmentVariable("TMP", Path.GetFullPath(AmoebaEnvironment.Paths.TempPath), EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("TEMP", Path.GetFullPath(AmoebaEnvironment.Paths.TempPath), EnvironmentVariableTarget.Process);
                }

                // Tor起動。
                if (AmoebaEnvironment.Config.Tor != null)
                {
                    var config = AmoebaEnvironment.Config.Tor;

                    var process = new Process();
                    process.StartInfo.FileName = Path.GetFullPath(config.Path);
                    process.StartInfo.Arguments = config.Arguments;
                    process.StartInfo.WorkingDirectory = Path.GetFullPath(config.WorkingDirectory);
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    process.Start();

                    _processList.Add(process);
                }

                this.StartupUri = new Uri("Windows/MainWindow.xaml", UriKind.Relative);
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                this.Shutdown();

                return;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Parallel.ForEach(_processList, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, process =>
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();
                }
                catch (Exception)
                {

                }
            });
        }

        private void KillProcesses()
        {
            var list = new List<string>();
            if (AmoebaEnvironment.Config.Tor != null) list.Add(AmoebaEnvironment.Config.Tor.Path);

            foreach (string path in list)
            {
                foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(path)))
                {
                    try
                    {
                        if (process.MainModule.FileName == Path.GetFullPath(path))
                        {
                            try
                            {
                                process.Kill();
                                process.WaitForExit();
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

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
    }
}
