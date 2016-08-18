using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using Library;
using Library.Net.Amoeba;

namespace Amoeba
{
    class Program
    {
        private static ServiceManager _serviceManager;

        public static void Main(string[] args)
        {
            _serviceManager = new ServiceManager();

            if (_serviceManager.Startup(args))
            {
                Application.Init();
                MainWindow window = new MainWindow();
                window.Show();
                Application.Run();
            }
        }

        public static ServiceManager ServiceManager
        {
            get
            {
                return _serviceManager;
            }
        }
    }
}
