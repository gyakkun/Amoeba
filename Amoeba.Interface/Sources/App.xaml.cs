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
                MessageBox.Show(ex.Message);

                this.Shutdown();
                return;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {

        }
    }
}
