using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Library;
using Library.Net.Amoeba;

namespace Amoeba
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public static string[] Args { get; private set; }
        public static Version AmoebaVersion { get; private set; }
        public static Dictionary<string, string> DirectoryPaths { get; private set; }
        public static string[] UpdateSignature { get; private set; }
        public static Node[] Nodes { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

            App.Args = e.Args;

            App.AmoebaVersion = new Version(0, 1, 0);

            App.DirectoryPaths = new Dictionary<string, string>();
            App.DirectoryPaths["Base"] = @"..\";
            App.DirectoryPaths["Core"] = Directory.GetCurrentDirectory();
            App.DirectoryPaths["Configuration"] = Path.Combine(App.DirectoryPaths["Base"], "Configuration");
            App.DirectoryPaths["Update"] = Path.Combine(App.DirectoryPaths["Base"], "Update");
            App.DirectoryPaths["Log"] = Path.Combine(App.DirectoryPaths["Base"], "Log");
            App.DirectoryPaths["Icons"] = Path.Combine(App.DirectoryPaths["Core"], "Icons");
            App.DirectoryPaths["Languages"] = Path.Combine(App.DirectoryPaths["Core"], "Languages");
            App.DirectoryPaths["Temp"] = Path.Combine(App.DirectoryPaths["Core"], "Temp");

            App.UpdateSignature = new string[] { };
            App.Nodes = new Node[] { };

            foreach (var item in App.DirectoryPaths.Values)
            {
                if (!Directory.Exists(item))
                {
                    Directory.CreateDirectory(item);
                }
            }

            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "UpdateSignature.txt")))
            {
                using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "UpdateSignature.txt"), false, new UTF8Encoding(false)))
                {
                    writer.WriteLine("root@Vf/Y+9l4IznTwEVpjVzo0j+rnJlSLExpHPf5w7Q402F19rP0Kiy+o62I4zUgvEqdIp+j9v6U4IdTffc1PBwyeA==");
                }
            }

            using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "UpdateSignature.txt"), new UTF8Encoding(false)))
            {
                string item = null;
                List<string> list = new List<string>();

                while ((item = reader.ReadLine()) != null)
                {
                    list.Add(item);
                }

                App.UpdateSignature = list.ToArray();
            }

            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Nodes.txt")))
            {
                using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Nodes.txt"), false, new UTF8Encoding(false)))
                {
                }
            }

            using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Nodes.txt"), new UTF8Encoding(false)))
            {
                string item = null;
                List<Node> list = new List<Node>();

                while ((item = reader.ReadLine()) != null)
                {
                    try
                    {
                        list.Add(AmoebaConverter.FromNodeString(item));
                    }
                    catch (Exception)
                    {

                    }
                }

                App.Nodes = list.ToArray();
            }

            if (!Directory.Exists(App.DirectoryPaths["Temp"]))
            {
                Directory.CreateDirectory(App.DirectoryPaths["Temp"]);
            }

            foreach (var path in Directory.GetFiles(App.DirectoryPaths["Temp"], "*", SearchOption.TopDirectoryOnly))
            {
                File.Delete(path);
            }

            foreach (var path in Directory.GetDirectories(App.DirectoryPaths["Temp"], "*", SearchOption.TopDirectoryOnly))
            {
                Directory.Delete(path, true);
            }

            Thread.GetDomain().UnhandledException += new UnhandledExceptionEventHandler(App_UnhandledException);
        }

        void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception == null)
                return;

            Log.Error(exception);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception);
        }
    }
}
