using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
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

        // Startup
        private static List<Process> _processList = new List<Process>();

        // Catharsis
        private static List<AddressFilter> _addressFilters = new List<AddressFilter>();
        public static List<AddressFilter> AddressFilters { get { return _addressFilters; } }

        // Colors
        public static AmoebaColors Colors { get; private set; }

        // Cache
        public static string Cache_Path { get; private set; }

        public App()
        {
            {
                OperatingSystem osInfo = Environment.OSVersion;

                // Windows Vista以上。
                if (osInfo.Platform == PlatformID.Win32NT && osInfo.Version.Major >= 6)
                {
                    // SHA512CryptoServiceProviderをデフォルトで使うように設定する。
                    CryptoConfig.AddAlgorithm(typeof(SHA512CryptoServiceProvider),
                        "SHA512",
                        "SHA512CryptoServiceProvider",
                        "System.Security.Cryptography.SHA512",
                        "System.Security.Cryptography.SHA512CryptoServiceProvider");
                }
            }

            App.AmoebaVersion = new Version(2, 0, 53);

            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            App.DirectoryPaths = new Dictionary<string, string>();

            App.DirectoryPaths["Base"] = @"..\";
            App.DirectoryPaths["Configuration"] = Path.Combine(@"..\", "Configuration");
            App.DirectoryPaths["Update"] = Path.GetFullPath(Path.Combine(@"..\", "Update"));
            App.DirectoryPaths["Log"] = Path.Combine(@"..\", "Log");
            App.DirectoryPaths["Input"] = Path.Combine(@"..\", "Input");
            App.DirectoryPaths["Work"] = Path.Combine(@"..\", "Work");

            App.DirectoryPaths["Core"] = @".\";
            App.DirectoryPaths["Icons"] = "Icons";
            App.DirectoryPaths["Languages"] = "Languages";

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

            Thread.GetDomain().UnhandledException += App_UnhandledException;
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
            if (exception == null) return;

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
                        string commandline = "\"" + Path.GetFullPath(Path.Combine(App.DirectoryPaths["Core"], "Amoeba.exe")) + "\" \"%1\"";
                        string fileType = "Amoeba";
                        string description = "Amoeba Box";
                        string verb = "open";
                        string iconPath = Path.GetFullPath(Path.Combine(App.DirectoryPaths["Icons"], "Box.ico"));

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
            if (e.Args.Length >= 2 && e.Args[0] == "Download")
            {
                try
                {
                    if (!Directory.Exists(App.DirectoryPaths["Input"]))
                        Directory.CreateDirectory(App.DirectoryPaths["Input"]);

                    using (FileStream stream = App.GetUniqueFileStream(Path.Combine(App.DirectoryPaths["Input"], "seed.txt")))
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        foreach (var item in e.Args.Skip(1))
                        {
                            if (string.IsNullOrWhiteSpace(item)) continue;
                            writer.WriteLine(item);
                        }
                    }
                }
                catch (Exception)
                {

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

            // 多重起動防止
            {
                Process currentProcess = Process.GetCurrentProcess();

                // 同一パスのプロセスが存在する場合、終了する。
                foreach (Process p in Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    if (p.Id == currentProcess.Id) continue;

                    try
                    {
                        if (p.MainModule.FileName == Path.GetFullPath(Assembly.GetEntryAssembly().Location))
                        {
                            this.Shutdown();

                            return;
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                string updateInformationFilePath = Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.update");

                // アップデート中の場合、終了する。
                if (File.Exists(updateInformationFilePath))
                {
                    using (FileStream stream = new FileStream(updateInformationFilePath, FileMode.Open))
                    using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false)))
                    {
                        var updateExeFilePath = reader.ReadLine();

                        foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(updateExeFilePath)))
                        {
                            try
                            {
                                if (Path.GetFileName(p.MainModule.FileName) == updateExeFilePath)
                                {
                                    this.Shutdown();

                                    return;
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }

                    File.Delete(updateInformationFilePath);
                }
            }

            App.CheckProcess();

            // アップデート
            if (Directory.Exists(App.DirectoryPaths["Update"]))
            {
            Restart: ;

                string zipFilePath = null;

                {
                    Regex regex = new Regex(@"Amoeba ((\d*)\.(\d*)\.(\d*)).*\.zip");
                    Version version = App.AmoebaVersion;

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
                                    zipFilePath = path;
                                }
                                else
                                {
                                    if (File.Exists(path))
                                        File.Delete(path);
                                }
                            }
                        }
                    }
                }

                if (zipFilePath != null)
                {
                    string workDirectioryPath = App.DirectoryPaths["Work"];
                    var tempCoreDirectoryPath = Path.Combine(workDirectioryPath, "Core");

                    if (Directory.Exists(tempCoreDirectoryPath))
                        Directory.Delete(tempCoreDirectoryPath, true);

                    try
                    {
                        using (ZipFile zipfile = new ZipFile(zipFilePath))
                        {
                            zipfile.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
                            zipfile.ExtractAll(tempCoreDirectoryPath);
                        }
                    }
                    catch (Exception)
                    {
                        if (File.Exists(zipFilePath))
                            File.Delete(zipFilePath);

                        goto Restart;
                    }

                    var tempUpdateExeFilePath = Path.Combine(workDirectioryPath, "Library.Update.exe");

                    if (File.Exists(tempUpdateExeFilePath))
                        File.Delete(tempUpdateExeFilePath);

                    File.Copy("Library.Update.exe", tempUpdateExeFilePath);

                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = tempUpdateExeFilePath;
                    startInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\"",
                        Process.GetCurrentProcess().Id,
                        Path.Combine(tempCoreDirectoryPath, "Core"),
                        Directory.GetCurrentDirectory(),
                        Path.Combine(Directory.GetCurrentDirectory(), "Amoeba.exe"),
                        Path.GetFullPath(zipFilePath));
                    startInfo.WorkingDirectory = Path.GetDirectoryName(tempUpdateExeFilePath);

                    var process = Process.Start(startInfo);
                    process.WaitForInputIdle();

                    string updateInformationFilePath = Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.update");

                    using (FileStream stream = new FileStream(updateInformationFilePath, FileMode.Create))
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.WriteLine(Path.GetFileName(tempUpdateExeFilePath));
                    }

                    this.Shutdown();

                    return;
                }
            }

            // バージョンアップ処理。
            if (File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.version")))
            {
                Version version;

                using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.version"), new UTF8Encoding(false)))
                {
                    version = new Version(reader.ReadLine());
                }

                if (version < new Version(2, 0, 20))
                {
                    if (File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.path")))
                    {
                        string cachePath;

                        using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.path"), new UTF8Encoding(false)))
                        {
                            cachePath = reader.ReadLine();
                        }

                        using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.settings"), false, new UTF8Encoding(false)))
                        {
                            writer.WriteLine(string.Format("{0} {1}", "Path", cachePath));
                        }

                        try
                        {
                            File.Delete(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.path"));
                        }
                        catch (Exception)
                        {

                        }
                    }

                    {
                        var oldPath = Path.Combine(App.DirectoryPaths["Configuration"], "Run.xml");
                        var newPath = Path.Combine(App.DirectoryPaths["Configuration"], "Startup.settings");

                        if (File.Exists(oldPath))
                        {
                            try
                            {
                                File.Move(oldPath, newPath);
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }

                if (version < new Version(2, 0, 28))
                {
                    {
                        var torWorkDirectoryPath = Path.Combine(App.DirectoryPaths["Work"], "Tor");

                        if (Directory.Exists(torWorkDirectoryPath))
                        {
                            try
                            {
                                Directory.Delete(torWorkDirectoryPath, true);
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }

                if (version < new Version(2, 0, 52))
                {
                    {
                        // Startup.settingsを初期化。
                        File.Delete(Path.Combine(App.DirectoryPaths["Configuration"], "Startup.settings"));
                    }
                }

                if (version < new Version(2, 0, 53))
                {
                    {
                        // Catharsis.settingsを初期化。
                        File.Delete(Path.Combine(App.DirectoryPaths["Configuration"], "Catharsis.settings"));
                    }
                }
            }

            App.StartupSettings();
            App.CatharsisSettings();
            App.CacheSettings();
            App.ColorsSettings();

            this.StartupUri = new Uri("Windows/MainWindow.xaml", UriKind.Relative);
        }

        private static void CheckProcess()
        {
            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Startup.settings"))) return;

            var runList = new List<RunItem>();

            using (StreamReader r = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Startup.settings"), new UTF8Encoding(false)))
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

                            using (var xmlSubtree = xml.ReadSubtree())
                            {
                                while (xmlSubtree.Read())
                                {
                                    if (xmlSubtree.NodeType == XmlNodeType.Element)
                                    {
                                        if (xmlSubtree.LocalName == "Path")
                                        {
                                            try
                                            {
                                                path = xmlSubtree.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        else if (xml.LocalName == "Arguments")
                                        {
                                            try
                                            {
                                                arguments = xmlSubtree.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        else if (xmlSubtree.LocalName == "WorkingDirectory")
                                        {
                                            try
                                            {
                                                workingDirectory = xmlSubtree.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                    }
                                }
                            }

                            runList.Add(new RunItem()
                            {
                                Path = path,
                                Arguments = arguments,
                                WorkingDirectory = workingDirectory
                            });
                        }
                    }
                }
            }

            Parallel.ForEach(runList, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, item =>
            {
                foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(item.Path)))
                {
                    try
                    {
                        if (p.MainModule.FileName == Path.GetFullPath(item.Path))
                        {
                            try
                            {
                                p.Kill();
                                p.WaitForExit();
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
            });
        }

        private static void StartupSettings()
        {
            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Startup.settings")))
            {
                using (XmlTextWriter xml = new XmlTextWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Startup.settings"), new UTF8Encoding(false)))
                {
                    xml.Formatting = Formatting.Indented;
                    xml.WriteStartDocument();

                    xml.WriteStartElement("Configuration");

                    {
                        xml.WriteStartElement("Process");

                        xml.WriteElementString("Path", @"Assembly\Tor\tor.exe");
                        xml.WriteElementString("Arguments", "-f torrc DataDirectory " + @"..\..\..\Work\Tor");
                        xml.WriteElementString("WorkingDirectory", @"Assembly\Tor");

                        xml.WriteEndElement(); //Process
                    }

                    {
                        xml.WriteStartElement("Process");

                        xml.WriteElementString("Path", @"Assembly\Polipo\polipo.exe");
                        xml.WriteElementString("Arguments", "-c polipo.conf");
                        xml.WriteElementString("WorkingDirectory", @"Assembly\Polipo");

                        xml.WriteEndElement(); //Process
                    }

                    xml.WriteEndElement(); //Configuration

                    xml.WriteEndDocument();
                    xml.Flush();
                }
            }

            var runList = new List<RunItem>();

            using (StreamReader r = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Startup.settings"), new UTF8Encoding(false)))
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

                            using (var xmlSubtree = xml.ReadSubtree())
                            {
                                while (xmlSubtree.Read())
                                {
                                    if (xmlSubtree.NodeType == XmlNodeType.Element)
                                    {
                                        if (xmlSubtree.LocalName == "Path")
                                        {
                                            try
                                            {
                                                path = xmlSubtree.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        else if (xml.LocalName == "Arguments")
                                        {
                                            try
                                            {
                                                arguments = xmlSubtree.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        else if (xmlSubtree.LocalName == "WorkingDirectory")
                                        {
                                            try
                                            {
                                                workingDirectory = xmlSubtree.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                    }
                                }
                            }

                            runList.Add(new RunItem()
                            {
                                Path = path,
                                Arguments = arguments,
                                WorkingDirectory = workingDirectory
                            });
                        }
                    }
                }
            }

            Parallel.ForEach(runList, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, item =>
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = item.Path;
                    process.StartInfo.Arguments = item.Arguments;
                    process.StartInfo.WorkingDirectory = item.WorkingDirectory;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    process.Start();

                    _processList.Add(process);
                }
                catch (Exception)
                {

                }
            });
        }

        private class RunItem
        {
            public string Path { get; set; }
            public string Arguments { get; set; }
            public string WorkingDirectory { get; set; }
        }

        private static void CatharsisSettings()
        {
            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Catharsis.settings")))
            {
                using (XmlTextWriter xml = new XmlTextWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Catharsis.settings"), new UTF8Encoding(false)))
                {
                    xml.Formatting = Formatting.Indented;
                    xml.WriteStartDocument();

                    xml.WriteStartElement("Configuration");

                    {
                        xml.WriteStartElement("Ipv4AddressFilter");

                        {
                            xml.WriteStartElement("Proxy");

                            xml.WriteElementString("Uri", @"tcp:127.0.0.1:18118");

                            xml.WriteEndElement(); //Proxy
                        }

                        {
                            xml.WriteStartElement("Targets");

                            xml.WriteElementString("Url", "http://list.iblocklist.com/lists/bluetack/edu");
                            xml.WriteElementString("Url", "http://list.iblocklist.com/lists/bluetack/level-1");
                            xml.WriteElementString("Url", "http://list.iblocklist.com/lists/bluetack/level-2");
                            xml.WriteElementString("Url", "http://list.iblocklist.com/lists/bluetack/spyware");
                            xml.WriteElementString("Url", "http://list.iblocklist.com/lists/tbg/primary-threats");

                            xml.WriteEndElement(); //Targets
                        }

                        xml.WriteEndElement(); //Ipv4AddressFilter
                    }

                    xml.WriteEndElement(); //Configuration

                    xml.WriteEndDocument();
                    xml.Flush();
                }
            }

            using (StreamReader r = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Catharsis.settings"), new UTF8Encoding(false)))
            using (XmlTextReader xml = new XmlTextReader(r))
            {
                while (xml.Read())
                {
                    if (xml.NodeType == XmlNodeType.Element)
                    {
                        if (xml.LocalName == "Ipv4AddressFilter")
                        {
                            string proxyUri = null;
                            List<string> urls = new List<string>();

                            using (var xmlSubtree = xml.ReadSubtree())
                            {
                                while (xmlSubtree.Read())
                                {
                                    if (xmlSubtree.NodeType == XmlNodeType.Element)
                                    {
                                        if (xmlSubtree.LocalName == "Proxy")
                                        {
                                            using (var xmlSubtree2 = xmlSubtree.ReadSubtree())
                                            {
                                                while (xmlSubtree2.Read())
                                                {
                                                    if (xmlSubtree2.NodeType == XmlNodeType.Element)
                                                    {
                                                        if (xmlSubtree2.LocalName == "Uri")
                                                        {
                                                            try
                                                            {
                                                                proxyUri = xmlSubtree2.ReadString();
                                                            }
                                                            catch (Exception)
                                                            {

                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else if (xmlSubtree.LocalName == "Targets")
                                        {
                                            using (var xmlSubtree2 = xmlSubtree.ReadSubtree())
                                            {
                                                while (xmlSubtree2.Read())
                                                {
                                                    if (xmlSubtree2.NodeType == XmlNodeType.Element)
                                                    {
                                                        if (xmlSubtree2.LocalName == "Url")
                                                        {
                                                            try
                                                            {
                                                                urls.Add(xmlSubtree2.ReadString());
                                                            }
                                                            catch (Exception)
                                                            {

                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            App.AddressFilters.Add(new AddressFilter(proxyUri, urls));
                        }
                    }
                }
            }
        }

        private static void CacheSettings()
        {
            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.settings")))
            {
                var cachePath = Path.Combine(App.DirectoryPaths["Configuration"], "Cache.blocks");

                using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.settings"), false, new UTF8Encoding(false)))
                {
                    writer.WriteLine(string.Format("{0} {1}", "Path", cachePath));
                }
            }

            {
                using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.settings"), new UTF8Encoding(false)))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        var items = line.Split(' ');
                        var name = items[0].Trim();
                        var value = items[1].Trim();

                        if (name == "Path")
                        {
                            App.Cache_Path = value;
                        }
                    }
                }
            }
        }

        private static void ColorsSettings()
        {
            App.Colors = new AmoebaColors();

            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Colors.settings")))
            {
                Type type = typeof(AmoebaColors);

                using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Colors.settings"), false, new UTF8Encoding(false)))
                {
                    var list = type.GetProperties().ToList();
                    list.Sort((x, y) => x.Name.CompareTo(y.Name));

                    foreach (var property in list)
                    {
                        writer.WriteLine(string.Format("{0} {1}", property.Name, (Color)property.GetValue(App.Colors, null)));
                    }
                }
            }

            {
                Type type = typeof(AmoebaColors);

                using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Colors.settings"), new UTF8Encoding(false)))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        var items = line.Split(' ');
                        var name = items[0].Trim();
                        var value = items[1].Trim();

                        var property = type.GetProperty(name);
                        property.SetValue(App.Colors, (Color)ColorConverter.ConvertFromString(value), null);
                    }
                }
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Parallel.ForEach(_processList, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, p =>
            {
                try
                {
                    p.Kill();
                    p.WaitForExit();
                }
                catch (Exception)
                {

                }
            });
        }
    }

    public class AmoebaColors
    {
        public AmoebaColors()
        {
            this.Tree_Hit = System.Windows.Media.Colors.LightPink;
        }

        public System.Windows.Media.Color Tree_Hit { get; set; }
    }
}
