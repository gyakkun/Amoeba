using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
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
using Library.Security;

namespace Amoeba
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    partial class App : Application
    {
        public static Version AmoebaVersion { get; private set; }
        public static Dictionary<string, string> DirectoryPaths { get; private set; }

        // Startup
        private static List<Process> _processList = new List<Process>();

        // Catharsis
        public static CatharsisSettings Catharsis { get; private set; }

        // Colors
        public static ColorsSettings Colors { get; private set; }

        // Cache
        public static CacheSettings Cache { get; private set; }

        App()
        {
            App.AmoebaVersion = new Version(3, 0, 44);

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

            App.DirectoryPaths = new Dictionary<string, string>();

            App.DirectoryPaths["Base"] = @"../";
            App.DirectoryPaths["Configuration"] = @"../Configuration";
            App.DirectoryPaths["Update"] = Path.GetFullPath(@"../Update");
            App.DirectoryPaths["Log"] = @"../Log";
            App.DirectoryPaths["Input"] = @"../Input";
            App.DirectoryPaths["Work"] = @"../Work";

            App.DirectoryPaths["Core"] = @"./";
            App.DirectoryPaths["Icons"] = "Icons";
            App.DirectoryPaths["Languages"] = "Languages";
            App.DirectoryPaths["Settings"] = "Settings";

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

            Thread.GetDomain().UnhandledException += this.App_UnhandledException;
        }

        private static string GetUniqueFilePath(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }

            for (int index = 1; ; index++)
            {
                string text = string.Format(@"{0}/{1} ({2}){3}",
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
                    return new FileStream(path, FileMode.CreateNew);
                }
                catch (DirectoryNotFoundException)
                {
                    throw;
                }
                catch (IOException)
                {

                }
            }

            for (int index = 1, count = 0; ; index++)
            {
                string text = string.Format(
                    @"{0}/{1} ({2}){3}",
                    Path.GetDirectoryName(path),
                    Path.GetFileNameWithoutExtension(path),
                    index,
                    Path.GetExtension(path));

                if (!File.Exists(text))
                {
                    try
                    {
                        return new FileStream(text, FileMode.CreateNew);
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
            try
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
                            string iconPath = Path.GetFullPath(Path.Combine(App.DirectoryPaths["Icons"], @"Files/Box.ico"));

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
                else if (e.Args.Length >= 2 && e.Args[0] == "Download")
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

                            using (var inStream = new FileStream(e.Args[0], FileMode.Open, FileAccess.Read, FileShare.Read))
                            using (var outStream = App.GetUniqueFileStream(Path.Combine(App.DirectoryPaths["Input"], Path.GetRandomFileName() + "_temp.box")))
                            {
                                byte[] buffer = new byte[1024 * 4];

                                int length = 0;

                                while ((length = inStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    outStream.Write(buffer, 0, length);
                                }
                            }
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

                App.ShutdownProcesses();

                // アップデート
                {
                    var workDirectioryPath = App.DirectoryPaths["Work"];

                    // 一時的に作成された"Library.Update.exe"を削除する。
                    try
                    {
                        var tempUpdateExeFilePath = Path.Combine(workDirectioryPath, "Library.Update.exe");

                        if (File.Exists(tempUpdateExeFilePath))
                            File.Delete(tempUpdateExeFilePath);
                    }
                    catch (Exception)
                    {

                    }

                    if (Directory.Exists(App.DirectoryPaths["Update"]))
                    {
                        Restart:;

                        string zipFilePath = null;

                        {
                            var regex = new Regex(@"Amoeba.*?((\d*)\.(\d*)\.(\d*)).*?\.zip");
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

                                        if (version <= tempVersion)
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
                            var tempUpdateDirectoryPath = Path.Combine(workDirectioryPath, "Update");

                            if (Directory.Exists(tempUpdateDirectoryPath))
                                Directory.Delete(tempUpdateDirectoryPath, true);

                            try
                            {
                                using (ZipFile zipfile = new ZipFile(zipFilePath))
                                {
                                    zipfile.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
                                    zipfile.ExtractAll(tempUpdateDirectoryPath);
                                }
                            }
                            catch (Exception)
                            {
                                if (File.Exists(zipFilePath))
                                    File.Delete(zipFilePath);

                                goto Restart;
                            }

                            var tempUpdateExeFilePath = Path.Combine(workDirectioryPath, "Library.Update.exe");

                            File.Copy("Library.Update.exe", tempUpdateExeFilePath);

                            var startInfo = new ProcessStartInfo();
                            startInfo.FileName = Path.GetFullPath(tempUpdateExeFilePath);
                            startInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\"",
                                Process.GetCurrentProcess().Id,
                                Path.Combine(tempUpdateDirectoryPath, "Core"),
                                Directory.GetCurrentDirectory(),
                                Path.Combine(Directory.GetCurrentDirectory(), "Amoeba.exe"),
                                Path.GetFullPath(zipFilePath));
                            startInfo.WorkingDirectory = Path.GetFullPath(Path.GetDirectoryName(tempUpdateExeFilePath));

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
                }

                // バージョンアップ処理。
                if (File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.version")))
                {
                    Version version;

                    using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.version"), new UTF8Encoding(false)))
                    {
                        version = new Version(reader.ReadLine());
                    }

                    if (version < new Version(3, 0, 0))
                    {
                        throw new NotSupportedException("Not supported configuration.");
                    }
                }

                App.StartupProcesses();

                App.CatharsisSettings();
                App.CacheSettings();
                App.ColorsSettings();

                this.StartupUri = new Uri("Windows/MainWindow.xaml", UriKind.Relative);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

                this.Shutdown();
            }
        }

        private static void StartupProcesses()
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

                        xml.WriteElementString("Path", @"Assemblies/Tor/tor.exe");
                        xml.WriteElementString("Arguments", "-f torrc DataDirectory " + @"../../../Work/Tor");
                        xml.WriteElementString("WorkingDirectory", @"Assemblies/Tor");

                        xml.WriteEndElement(); //Process
                    }

                    {
                        xml.WriteStartElement("Process");

                        xml.WriteElementString("Path", @"Assemblies/Polipo/polipo.exe");
                        xml.WriteElementString("Arguments", "-c polipo.conf");
                        xml.WriteElementString("WorkingDirectory", @"Assemblies/Polipo");

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
                    var process = new Process();
                    process.StartInfo.FileName = Path.GetFullPath(item.Path);
                    process.StartInfo.Arguments = item.Arguments;
                    process.StartInfo.WorkingDirectory = Path.GetFullPath(item.WorkingDirectory);
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

        private static void ShutdownProcesses()
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

        private class RunItem
        {
            public string Path { get; set; }
            public string Arguments { get; set; }
            public string WorkingDirectory { get; set; }
        }

        private static void CatharsisSettings()
        {
            App.Catharsis = new CatharsisSettings();

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

                            // https://www.iblocklist.com/lists.php
                            // 政府系IP、反P2P系企業IPを選択的にブロック。
                            xml.WriteComment(@"<Url>http://list.iblocklist.com/lists/bluetack/level-1</Url>");
                            xml.WriteComment(@"<Url>http://list.iblocklist.com/lists/tbg/primary-threats</Url>");

                            xml.WriteElementString("Path", @"Catharsis_Ipv4.txt");

                            xml.WriteEndElement(); //Targets
                        }

                        xml.WriteEndElement(); //Ipv4AddressFilter
                    }

                    xml.WriteEndElement(); //Configuration

                    xml.WriteEndDocument();
                    xml.Flush();
                }
            }

            var ipv4AddressFilters = new List<Ipv4AddressFilter>();

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
                            var urls = new List<string>();
                            var paths = new List<string>();

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
                                                        else if (xmlSubtree2.LocalName == "Path")
                                                        {
                                                            try
                                                            {
                                                                paths.Add(xmlSubtree2.ReadString());
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

                            App.Catharsis.Ipv4AddressFilters.Add(new Ipv4AddressFilter(proxyUri, urls, paths));
                        }
                    }
                }
            }
        }

        private static void CacheSettings()
        {
            App.Cache = new CacheSettings();

            // Initialize
            {
                App.Cache.Path = Path.Combine(App.DirectoryPaths["Configuration"], "Cache.blocks");
            }

            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.settings")))
            {
                using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.settings"), false, new UTF8Encoding(false)))
                {
                    writer.WriteLine(string.Format("{0} {1}", "Path", App.Cache.Path));
                }
            }

            {
                using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.settings"), new UTF8Encoding(false)))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        var index = line.IndexOf(' ');
                        var name = line.Substring(0, index);
                        var value = line.Substring(index + 1);

                        if (name == "Path")
                        {
                            App.Cache.Path = value;
                        }
                    }
                }
            }
        }

        private static void ColorsSettings()
        {
            App.Colors = new ColorsSettings();

            // Initialize
            {
                App.Colors.Tree_Hit = System.Windows.Media.Colors.LightPink;
            }

            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Colors.settings")))
            {
                Type type = typeof(ColorsSettings);

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
                Type type = typeof(ColorsSettings);

                using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Colors.settings"), new UTF8Encoding(false)))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        var index = line.IndexOf(' ');
                        var name = line.Substring(0, index);
                        var value = line.Substring(index + 1);

                        var property = type.GetProperty(name);
                        if (property == null) continue;

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

    class CatharsisSettings
    {
        private List<Ipv4AddressFilter> _ipv4AddressFilters;

        public List<Ipv4AddressFilter> Ipv4AddressFilters
        {
            get
            {
                if (_ipv4AddressFilters == null)
                    _ipv4AddressFilters = new List<Ipv4AddressFilter>();

                return _ipv4AddressFilters;
            }
        }
    }

    class ColorsSettings
    {
        public System.Windows.Media.Color Tree_Hit { get; set; }
    }

    class CacheSettings
    {
        public string Path { get; set; }
    }
}
