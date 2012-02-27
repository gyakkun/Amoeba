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
using System.Text.RegularExpressions;
using Ionic.Zip;

namespace Amoeba
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public static Version AmoebaVersion { get; private set; }
        public static Dictionary<string, string> DirectoryPaths { get; private set; }
        public static string[] UpdateSignature { get; private set; }
        public static Node[] Nodes { get; private set; }

        public App()
        {
            App.AmoebaVersion = new Version(0, 1, 0);

            App.DirectoryPaths = new Dictionary<string, string>();
            App.DirectoryPaths["Base"] = @"..\";
            App.DirectoryPaths["Core"] = @".\";
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
                    writer.WriteLine("0cfQjFzmkLaXEhjXVHzWtdHT+4VBbUKChW3OnUvpAtPOJOqxTLd3m22cIQwWc4VftWZYu7DNynFhvARqlBLHtg==");
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

            this.AmoebaUpdate();
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

        private void AmoebaUpdate()
        {
            if (Directory.Exists(App.DirectoryPaths["Update"]))
            {
                Regex regex = new Regex(@"Amoeba ((\d*)\.(\d*)\.(\d*)).*\.zip");
                Version version = App.AmoebaVersion;
                string updatePath = null;

                foreach (var path in Directory.GetFiles(App.DirectoryPaths["Update"]))
                {
                    string name = Path.GetFileName(path);

                    if (name.StartsWith("Amoeba"))
                    {
                        var match = regex.Match(name);

                        if (match.Success)
                        {
                            var tempVersion = new Version(match.Groups[1].Value);
                            version = (version < tempVersion) ? tempVersion : version;
                            updatePath = path;
                        }
                    }
                }

                if (updatePath != null)
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), "Amoeba_Update");
                    var tempUpdateExePath = Path.Combine(Path.GetTempPath(), "Library.Update.exe");

                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);

                    using (ZipFile zipfile = new ZipFile(updatePath))
                    {
                        zipfile.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
                        zipfile.UseUnicodeAsNecessary = true;
                        zipfile.ExtractAll(tempPath);
                    }

                    if (File.Exists(tempUpdateExePath))
                        File.Delete(tempUpdateExePath);

                    File.Copy("Library.Update.exe", tempUpdateExePath);

                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = tempUpdateExePath;
                    startInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\"",
                        Process.GetCurrentProcess().Id,
                        Path.Combine(tempPath, "Core"),
                        Directory.GetCurrentDirectory(),
                        Path.Combine(Directory.GetCurrentDirectory(), "Amoeba.exe"));
                    startInfo.WorkingDirectory = Path.GetDirectoryName(startInfo.FileName);

                    Process.Start(startInfo);

                    this.Shutdown();
                }
            }
        }
    }
}
