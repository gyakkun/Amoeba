using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Ionic.Zip;
using Library;
using Library.Net.Amoeba;

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
        public static string[] Args { get; private set; }
        public static string SelectTab { get; set; }

        public App()
        {
            //System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

            App.AmoebaVersion = new Version(0, 1, 4);

            App.DirectoryPaths = new Dictionary<string, string>();
            App.DirectoryPaths["Base"] = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            App.DirectoryPaths["Core"] = Path.Combine(App.DirectoryPaths["Base"], "Core");
            Directory.SetCurrentDirectory(App.DirectoryPaths["Core"]);

            App.DirectoryPaths["Configuration"] = Path.Combine(App.DirectoryPaths["Base"], "Configuration");
            App.DirectoryPaths["Update"] = Path.Combine(App.DirectoryPaths["Base"], "Update");
            App.DirectoryPaths["Log"] = Path.Combine(App.DirectoryPaths["Base"], "Log");
            App.DirectoryPaths["Icons"] = Path.Combine(App.DirectoryPaths["Core"], "Icons");
            App.DirectoryPaths["Languages"] = Path.Combine(App.DirectoryPaths["Core"], "Languages");
            App.DirectoryPaths["Temp"] = Path.Combine(App.DirectoryPaths["Base"], "Temp");
            App.DirectoryPaths["Input"] = Path.Combine(App.DirectoryPaths["Base"], "Input");

            App.UpdateSignature = new string[] { };
            App.Nodes = new Node[] { };

            if (Directory.Exists(App.DirectoryPaths["Temp"]))
            {
                Directory.Delete(App.DirectoryPaths["Temp"], true);
            }
            
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
                    writer.WriteLine("kbMq8T1x_bwrJ--Wzwyu");
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

            Thread.GetDomain().UnhandledException += new UnhandledExceptionEventHandler(App_UnhandledException);

            this.Update();
        }

        void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception == null)
                return;

            Log.Error(exception);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            App.Args = e.Args;
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception);
        }

        private static string GetUniqueFilePath(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }

            for (int index = 1; ; index++)
            {
                string text = string.Format(@"{0}\{1} ({2}){3}",
                    Path.GetDirectoryName(path),
                    Path.GetFileNameWithoutExtension(path),
                    index,
                    Path.GetExtension(path));

                if (!File.Exists(text))
                {
                    return text;
                }
            }
        }
        
        private void Update()
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

                            if (version < tempVersion)
                            {
                                version = tempVersion;
                                updatePath = path;
                            }
                        }
                    }
                }

                if (updatePath != null)
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), "Amoeba_Update");

                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);

                    try
                    {
                        using (ZipFile zipfile = new ZipFile(updatePath))
                        {
                            zipfile.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
                            zipfile.UseUnicodeAsNecessary = true;
                            zipfile.ExtractAll(tempPath);
                        }
                    }
                    catch(Exception)
                    {
                        return;
                    }
                    finally
                    {
                        if (File.Exists(updatePath))
                            File.Delete(updatePath);
                    }

                    var tempUpdateExePath = Path.Combine(Path.GetTempPath(), "Library.Update.exe");

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
