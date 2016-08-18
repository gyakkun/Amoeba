using System;
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
            _serviceManager = new ServiceManager();
        }

        public ServiceManager ServiceManager
        {
            get
            {
                return _serviceManager;
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                if (_serviceManager.Startup(e.Args))
                {
                    this.StartupUri = new Uri("Windows/MainWindow.xaml", UriKind.Relative);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

                this.Shutdown();
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
