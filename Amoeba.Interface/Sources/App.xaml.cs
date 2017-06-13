using System;
using System.Collections.Generic;
using System.Configuration;
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
        Mutex _mutex;

        public App()
        {
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
            OperatingSystem osInfo = Environment.OSVersion;
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

                // アップデート
                {
                    // 一時的に作成された"Amoeba.Update.exe"を削除する。
                    try
                    {
                        string tempUpdateExeFilePath = Path.Combine(AmoebaEnvironment.Paths.WorkPath, "Amoeba.Update.exe");

                        if (File.Exists(tempUpdateExeFilePath))
                        {
                            File.Delete(tempUpdateExeFilePath);
                        }
                    }
                    catch (Exception)
                    {

                    }

                    if (Directory.Exists(AmoebaEnvironment.Paths.UpdatePath))
                    {
                        string zipFilePath = Directory.GetFiles(AmoebaEnvironment.Paths.UpdatePath)
                            .Where(n => Path.GetFileName(n).StartsWith("Amoeba"))
                            .FirstOrDefault();

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

                {
                    foreach (var propertyInfo in typeof(AmoebaEnvironment.EnvironmentPaths).GetProperties())
                    {
                        string path = propertyInfo.GetValue(AmoebaEnvironment.Paths) as string;
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    }
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

        }
    }
}
