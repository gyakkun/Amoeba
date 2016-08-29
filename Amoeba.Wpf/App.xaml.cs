using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using Library;

namespace Amoeba
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    partial class App : Application
    {
        private ServiceManager _serviceManager;

        public App()
        {
            {
                var currentProcess = Process.GetCurrentProcess();

                currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
                currentProcess.SetMemoryPriority(4);
            }

            {
                OperatingSystem osInfo = System.Environment.OSVersion;

                // Windows Vista以上。
                if (osInfo.Platform == PlatformID.Win32NT && osInfo.Version >= new Version(6, 0))
                {
                    // SHA256Cngをデフォルトで使うように設定する。
                    CryptoConfig.AddAlgorithm(typeof(SHA256Cng),
                        "SHA256",
                        "SHA256Cng",
                        "System.Security.Cryptography.SHA256",
                        "System.Security.Cryptography.SHA256Cng");
                }
                else
                {
                    // SHA256Managedをデフォルトで使うように設定する。
                    CryptoConfig.AddAlgorithm(typeof(SHA256Managed),
                        "SHA256",
                        "SHA256Managed",
                        "System.Security.Cryptography.SHA256",
                        "System.Security.Cryptography.SHA256Managed");
                }
            }

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

        public ServiceManager ServiceManager
        {
            get
            {
                return _serviceManager;
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var serviceManager = new ServiceManager();

            if (!serviceManager.Startup(e.Args))
            {
                serviceManager.Dispose();
                serviceManager = null;

                this.Shutdown();
            }
            else
            {
                this.StartupUri = new Uri("Windows/MainWindow.xaml", UriKind.Relative);

                _serviceManager = serviceManager;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (_serviceManager != null)
            {
                _serviceManager.Dispose();
                _serviceManager = null;
            }
        }
    }
}
