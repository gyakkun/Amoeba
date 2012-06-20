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
using System.Xml;
using Ionic.Zip;
using Library;
using Library.Io;
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
        public static string SelectTab { get; set; }
        private FileStream _lockStream = null;
        private List<Process> _processList = new List<Process>();

        public App()
        {
            //System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

            App.AmoebaVersion = new Version(0, 1, 11);

            App.DirectoryPaths = new Dictionary<string, string>();
            App.DirectoryPaths["Base"] = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            App.DirectoryPaths["Core"] = Path.Combine(App.DirectoryPaths["Base"], "Core");
            Directory.SetCurrentDirectory(App.DirectoryPaths["Core"]);

            App.DirectoryPaths["Configuration"] = Path.Combine(App.DirectoryPaths["Base"], "Configuration");
            App.DirectoryPaths["Update"] = Path.Combine(App.DirectoryPaths["Base"], "Update");
            App.DirectoryPaths["Log"] = Path.Combine(App.DirectoryPaths["Base"], "Log");
            App.DirectoryPaths["Icons"] = Path.Combine(App.DirectoryPaths["Core"], "Icons");
            App.DirectoryPaths["Languages"] = Path.Combine(App.DirectoryPaths["Core"], "Languages");
            App.DirectoryPaths["Input"] = Path.Combine(App.DirectoryPaths["Base"], "Input");

            foreach (var item in App.DirectoryPaths.Values)
            {
                try
                {
                    if (!Directory.Exists(item))
                    {
                        Directory.CreateDirectory(item);
                    }
                }
                catch (Exception)
                {

                }
            }

            Thread.GetDomain().UnhandledException += new UnhandledExceptionEventHandler(App_UnhandledException);
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

        private static FileStream GetUniqueFileStream(string path)
        {
            if (!File.Exists(path))
            {
                try
                {
                    FileStream fs = new FileStream(path, FileMode.CreateNew);
                    return fs;
                }
                catch (DirectoryNotFoundException)
                {
                    throw;
                }
                catch (IOException)
                {
                    throw;
                }
            }

            for (int index = 1, count = 0; ; index++)
            {
                string text = string.Format(
                    @"{0}\{1} ({2}){3}",
                    Path.GetDirectoryName(path),
                    Path.GetFileNameWithoutExtension(path),
                    index,
                    Path.GetExtension(path));

                if (!File.Exists(text))
                {
                    try
                    {
                        FileStream fs = new FileStream(text, FileMode.CreateNew);
                        return fs;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        throw;
                    }
                    catch (IOException)
                    {
                        count++;
                        if (count > 1024)
                        {
                            throw;
                        }
                    }
                }
            }
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

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 2 && e.Args[0] == "Relate")
            {
                if (e.Args[1] == "on")
                {
                    try
                    {
                        string extension = ".box";
                        string commandline = "\"" + Path.Combine(App.DirectoryPaths["Core"], "Amoeba.exe") + "\" \"%1\"";
                        string fileType = "Amoeba";
                        string description = "Amoeba Box";
                        string verb = "open";
                        string iconPath = Path.Combine(App.DirectoryPaths["Icons"], "Box.ico");

                        using (var regkey = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(extension))
                        {
                            regkey.SetValue("", fileType);
                        }

                        using (var shellkey = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(fileType))
                        {
                            shellkey.SetValue("", description);

                            using (var shellkey2 = shellkey.CreateSubKey("shell\\" + verb))
                            {
                                using (var shellkey3 = shellkey2.CreateSubKey("command"))
                                {
                                    shellkey3.SetValue("", commandline);
                                    shellkey3.Close();
                                }
                            }
                        }

                        using (var iconkey = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(fileType + "\\DefaultIcon"))
                        {
                            iconkey.SetValue("", "\"" + iconPath + "\"");
                        }
                    }
                    catch (Exception)
                    {

                    }

                    this.Shutdown();

                    return;
                }
                else if (e.Args[1] == "off")
                {
                    try
                    {
                        string extension = ".box";
                        string fileType = "Amoeba";

                        Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(extension);
                        Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(fileType);
                    }
                    catch (Exception)
                    {

                    }

                    this.Shutdown();

                    return;
                }
            }
            else if (e.Args.Length == 1 && e.Args[0].EndsWith(".box") && File.Exists(e.Args[0]))
            {
                try
                {
                    if (Path.GetExtension(e.Args[0]).ToLower() == ".box")
                    {
                        if (!Directory.Exists(App.DirectoryPaths["Input"]))
                            Directory.CreateDirectory(App.DirectoryPaths["Input"]);

                        File.Copy(e.Args[0], App.GetUniqueFilePath(Path.Combine(App.DirectoryPaths["Input"], Path.GetRandomFileName() + "_temp.box")));
                    }
                }
                catch (Exception)
                {

                }
            }
            else if (e.Args.Length >= 1 && e.Args[0].StartsWith("Seed@"))
            {
                try
                {
                    if (!Directory.Exists(App.DirectoryPaths["Input"]))
                        Directory.CreateDirectory(App.DirectoryPaths["Input"]);

                    using (FileStream stream = App.GetUniqueFileStream(Path.Combine(App.DirectoryPaths["Input"], "seed.txt")))
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        foreach (var item in e.Args)
                        {
                            if (item == null || !item.StartsWith("Seed@")) continue;
                            writer.WriteLine(item);
                        }
                    }
                }
                catch (Exception)
                {

                }
            }

            try
            {
                _lockStream = new FileStream(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.lock"), FileMode.Create);
            }
            catch (IOException)
            {
                this.Shutdown();

                return;
            }

            // Update
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
                        catch (Exception)
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

                        return;
                    }
                }
            }

            this.Setting();

            this.StartupUri = new Uri("Windows/MainWindow.xaml", UriKind.Relative);
        }

        private void Setting()
        {
            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Run.xml")))
            {
                using (XmlTextWriter xml = new XmlTextWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Run.xml"), new UTF8Encoding(false)))
                {
                    xml.Formatting = Formatting.Indented;
                    xml.WriteStartDocument();

                    xml.WriteStartElement("Configuration");

                    {
                        xml.WriteStartElement("Process");
                        xml.WriteElementString("Path", @"Tor\tor.exe");
                        xml.WriteElementString("Arguments", "-f torrc");
                        xml.WriteElementString("WorkingDirectory", "Tor");

                        xml.WriteEndElement(); //Process
                    }

                    xml.WriteEndElement(); //Configuration

                    xml.WriteEndDocument();
                    xml.Flush();
                }
            }

            using (StreamReader r = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Run.xml"), new UTF8Encoding(false)))
            using (XmlTextReader xml = new XmlTextReader(r))
            {
                while (xml.Read())
                {
                    if (xml.NodeType == XmlNodeType.Element)
                    {
                        if (xml.LocalName == "Process")
                        {
                            string path = null;
                            string arguments = null;
                            string workingDirectory = null;

                            using (var xmlReader = xml.ReadSubtree())
                            {
                                while (xmlReader.Read())
                                {
                                    if (xmlReader.NodeType == XmlNodeType.Element)
                                    {
                                        if (xmlReader.LocalName == "Path")
                                        {
                                            try
                                            {
                                                path = xmlReader.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        else if (xml.LocalName == "Arguments")
                                        {
                                            try
                                            {
                                                arguments = xmlReader.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        else if (xmlReader.LocalName == "WorkingDirectory")
                                        {
                                            try
                                            {
                                                workingDirectory = xmlReader.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                    }
                                }
                            }

                            foreach (var p in Process.GetProcesses())
                            {
                                try
                                {
                                    if (p.MainModule.FileName == Path.GetFullPath(path))
                                    {
                                        try
                                        {
                                            p.Kill();
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

                            try
                            {
                                Process process = new Process();
                                process.StartInfo.FileName = path;
                                process.StartInfo.Arguments = arguments;
                                process.StartInfo.WorkingDirectory = workingDirectory;
                                process.StartInfo.CreateNoWindow = true;
                                process.StartInfo.UseShellExecute = false;
                                process.Start();

                                _processList.Add(process);
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (_lockStream != null)
            {
                _lockStream.Close();
                _lockStream = null;
            }

            foreach (var p in _processList)
            {
                try
                {
                    p.Kill();
                }
                catch (Exception)
                {

                }
            }
        }
    }
}
