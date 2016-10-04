using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using Ionic.Zip;
using Library;

namespace Amoeba
{
    class ServiceManager : ManagerBase
    {
        public Version AmoebaVersion { get; private set; }
        public Dictionary<string, string> Paths { get; private set; }
        public Config Config { get; private set; } = new Config();

        private List<Process> _processList = new List<Process>();

        private volatile bool _disposed;

        public ServiceManager()
        {
            this.AmoebaVersion = new Version(4, 0, 19);

            this.Paths = new Dictionary<string, string>();

            this.Paths["Base"] = @"../";
            this.Paths["Configuration"] = @"../Configuration";
            this.Paths["Update"] = Path.GetFullPath(@"../Update");
            this.Paths["Log"] = @"../Log";
            this.Paths["Input"] = @"../Input";
            this.Paths["Work"] = @"../Work";

            this.Paths["Core"] = @"./";
            this.Paths["Icons"] = "Icons";
            this.Paths["Languages"] = "Languages";
            this.Paths["Settings"] = "Settings";

            foreach (var item in this.Paths.Values)
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

        public bool Startup(string[] args)
        {
            try
            {
                if (args.Length == 2 && args[0] == "Relate")
                {
                    if (args[1] == "on")
                    {
                        try
                        {
                            string extension = ".box";
                            string commandline = "\"" + Path.GetFullPath(Path.Combine(this.Paths["Core"], "Amoeba.exe")) + "\" \"%1\"";
                            string fileType = "Amoeba";
                            string description = "Amoeba Box";
                            string verb = "open";
                            string iconPath = Path.GetFullPath(Path.Combine(this.Paths["Icons"], @"Files/Box.ico"));

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

                        return false;
                    }
                    else if (args[1] == "off")
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

                        return false;
                    }
                }
                else if (args.Length >= 2 && args[0] == "Download")
                {
                    try
                    {
                        if (!Directory.Exists(this.Paths["Input"]))
                            Directory.CreateDirectory(this.Paths["Input"]);

                        using (FileStream stream = ServiceManager.GetUniqueFileStream(Path.Combine(this.Paths["Input"], "seed.txt")))
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            foreach (var item in args.Skip(1))
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
                else if (args.Length == 1 && args[0].EndsWith(".box") && File.Exists(args[0]))
                {
                    try
                    {
                        if (Path.GetExtension(args[0]).ToLower() == ".box")
                        {
                            if (!Directory.Exists(this.Paths["Input"]))
                                Directory.CreateDirectory(this.Paths["Input"]);

                            using (var inStream = new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.Read))
                            using (var outStream = ServiceManager.GetUniqueFileStream(Path.Combine(this.Paths["Input"], Path.GetRandomFileName() + "_temp.box")))
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
                                return false;
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }

                    string updateInformationFilePath = Path.Combine(this.Paths["Configuration"], "Amoeba.update");

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
                                        return false;
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

                // バージョンアップ処理。
                if (File.Exists(Path.Combine(this.Paths["Configuration"], "Amoeba.version")))
                {
                    Version version;

                    using (StreamReader reader = new StreamReader(Path.Combine(this.Paths["Configuration"], "Amoeba.version"), new UTF8Encoding(false)))
                    {
                        version = new Version(reader.ReadLine());
                    }

                    if (version < new Version(4, 0, 0))
                    {
                        throw new NotSupportedException("Not supported configuration.");
                    }
                    if (version <= new Version(4, 0, 2))
                    {
                        {
                            File.Delete(Path.Combine(this.Paths["Configuration"], "Colors.config"));
                        }

                        {
                            var oldPath = Path.Combine(this.Paths["Configuration"], "Settings", "Global_DigitalSignatureCollection.config.gz");
                            var newPath = Path.Combine(this.Paths["Configuration"], "Settings", "Global_DigitalSignatures.config.gz");

                            if (File.Exists(oldPath)) File.Move(oldPath, newPath);
                        }
                    }
                }

                this.Config.Load(this.Paths["Configuration"]);

                this.ShutdownProcesses();

                // アップデート
                {
                    var workDirectioryPath = this.Paths["Work"];

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

                    if (Directory.Exists(this.Paths["Update"]))
                    {
                        Restart:;

                        string zipFilePath = null;

                        {
                            var regex = new Regex(@"Amoeba.*?((\d*)\.(\d*)\.(\d*)).*?\.zip");
                            Version version = this.AmoebaVersion;

                            foreach (var path in Directory.GetFiles(this.Paths["Update"]))
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

                            string updateInformationFilePath = Path.Combine(this.Paths["Configuration"], "Amoeba.update");

                            using (FileStream stream = new FileStream(updateInformationFilePath, FileMode.Create))
                            using (StreamWriter writer = new StreamWriter(stream))
                            {
                                writer.WriteLine(Path.GetFileName(tempUpdateExeFilePath));
                            }

                            return false;
                        }
                    }
                }

                this.StartupProcesses();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

                return false;
            }
        }

        private void StartupProcesses()
        {
            Parallel.ForEach(this.Config.Startup.ProcessSettings, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, item =>
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

        private void ShutdownProcesses()
        {
            Parallel.ForEach(this.Config.Startup.ProcessSettings, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, item =>
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

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
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
    }
}
