using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Amoeba.Properties;
using Library;
using Library.Io;
using Library.Net.Amoeba;
using Library.Net.Connection;
using Library.Net.Proxy;
using Library.Net.Upnp;
using Library.Security;

namespace Amoeba.Windows
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    partial class MainWindow : Window, IDisposable
    {
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;
        private AutoBaseNodeSettingManager _autoBaseNodeSettingManager;

        System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        private WindowState _windowState;

        private Dictionary<string, string> _configrationDirectoryPaths = new Dictionary<string, string>();
        private string _logPath = null;
        private FileStream _lockStream = null;

        private bool _disposed = false;

        public MainWindow()
        {
            try
            {
                _lockStream = new FileStream(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.lock"), FileMode.Create);
            }
            catch (IOException)
            {
                this.Dispose();
            }

            _bufferManager = new BufferManager();

            this.Setting_Log();

            _configrationDirectoryPaths.Add("MainWindow", Path.Combine(App.DirectoryPaths["Configuration"], @"Amoeba/Properties/Settings"));
            _configrationDirectoryPaths.Add("AutoBaseNodeSettingManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Amoeba/AutoBaseNodeSettingManager"));
            _configrationDirectoryPaths.Add("AmoebaManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Library/Net/Amoeba/AmoebaManager"));

            Settings.Instance.Load(_configrationDirectoryPaths["MainWindow"]);

            InitializeComponent();

            _windowState = this.WindowState;

            this.Title = string.Format("Amoeba {0} α", App.AmoebaVersion);

            using (FileStream stream = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            this.Setting_DebugLog();
            this.Setting_Languages();
        }

        ~MainWindow()
        {
            this.Dispose(false);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            this.Dispose();
        }

        public static void CopyDirectory(string sourceDirName, string destDirName)
        {
            if (!System.IO.Directory.Exists(destDirName))
            {
                System.IO.Directory.CreateDirectory(destDirName);
                System.IO.File.SetAttributes(destDirName, System.IO.File.GetAttributes(sourceDirName));
            }

            if (destDirName[destDirName.Length - 1] != System.IO.Path.DirectorySeparatorChar)
            {
                destDirName = destDirName + System.IO.Path.DirectorySeparatorChar;
            }

            foreach (string file in System.IO.Directory.GetFiles(sourceDirName))
            {
                System.IO.File.Copy(file, destDirName + System.IO.Path.GetFileName(file), true);
            }

            foreach (string dir in System.IO.Directory.GetDirectories(sourceDirName))
            {
                CopyDirectory(dir, destDirName + System.IO.Path.GetFileName(dir));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            NativeMethods.SetThreadExecutionState(ExecutionState.SystemRequired | ExecutionState.Continuous);

            _amoebaManager = new AmoebaManager(Path.Combine(App.DirectoryPaths["Configuration"], "cache.blocks"), App.DirectoryPaths["Temp"], _bufferManager);
            _amoebaManager.Load(_configrationDirectoryPaths["AmoebaManager"]);

            if (_amoebaManager.BaseNode == null || _amoebaManager.BaseNode.Id == null)
            {
                byte[] buffer = new byte[64];
                (new RNGCryptoServiceProvider()).GetBytes(buffer);

                var baseNode = new Node();
                baseNode.Id = buffer;

                _amoebaManager.BaseNode = baseNode;
            }

            _amoebaManager.SetOtherNodes(App.Nodes);

            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.version")))
            {
                _amoebaManager.SearchKeywords.Clear();
                _amoebaManager.SearchKeywords.Add(new Keyword()
                {
                    HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512,
                    Value = "global"
                });
                _amoebaManager.SearchKeywords.Add(new Keyword()
                {
                    HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512,
                    Value = "picture"
                });
                _amoebaManager.SearchKeywords.Add(new Keyword()
                {
                    HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512,
                    Value = "movie"
                });
                _amoebaManager.SearchKeywords.Add(new Keyword()
                {
                    HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512,
                    Value = "music"
                });
                _amoebaManager.SearchKeywords.Add(new Keyword()
                {
                    HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512,
                    Value = "archive"
                });
                _amoebaManager.SearchKeywords.Add(new Keyword()
                {
                    HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512,
                    Value = "document"
                });
                _amoebaManager.SearchKeywords.Add(new Keyword()
                {
                    HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512,
                    Value = "executable"
                });

                Directory.CreateDirectory(Path.Combine(App.DirectoryPaths["Base"], "Download"));
                _amoebaManager.DownloadDirectory = Path.Combine(App.DirectoryPaths["Base"], "Download");

                _amoebaManager.ConnectionCountLimit = 32;
                _amoebaManager.DownloadingConnectionCountLowerLimit = 6;
                _amoebaManager.UploadingConnectionCountLowerLimit = 12;

                Settings.Instance.Global_UploadKeywords.Add("global");

                SearchItem imageSearchItem = new SearchItem()
                {
                    Name = "Picture"
                };
                imageSearchItem.SearchNameRegexCollection.Add(new SearchContains<string>()
                {
                    Contains = true,
                    Value = @"(.*)\.(jpeg|jpg|png|jp2|gif|bmp)$"
                });

                SearchItem videoSearchItem = new SearchItem()
                {
                    Name = "Movie"
                };
                videoSearchItem.SearchNameRegexCollection.Add(new SearchContains<string>()
                {
                    Contains = true,
                    Value = @"(.*)\.(mpeg|mpg|avi|divx|asf|wmv|rm|ogm|mov|flv|mp4|wav|mid|aif)$"
                });

                SearchItem audioSearchItem = new SearchItem()
                {
                    Name = "Music"
                };
                audioSearchItem.SearchNameRegexCollection.Add(new SearchContains<string>()
                {
                    Contains = true,
                    Value = @"(.*)\.(mp3|ogg|wav|mid|mod|flac|sid)$"
                });

                SearchItem archiveSearchItem = new SearchItem()
                {
                    Name = "Archive"
                };
                archiveSearchItem.SearchNameRegexCollection.Add(new SearchContains<string>()
                {
                    Contains = true,
                    Value = @"(.*)\.(zip|rar|gz|arj|ace|bz|tar|tgz|txz|7z|lzh|lhz|iso)$"
                });

                SearchItem documentSearchItem = new SearchItem()
                {
                    Name = "Document"
                };
                documentSearchItem.SearchNameRegexCollection.Add(new SearchContains<string>()
                {
                    Contains = true,
                    Value = @"(.*)\.(dov|txt|pdf|dvi|ps|odt|sxw|rtf|pdb|psw)$"
                });

                SearchItem ExecutableSearchItem = new SearchItem()
                {
                    Name = "Executable"
                };
                ExecutableSearchItem.SearchNameRegexCollection.Add(new SearchContains<string>()
                {
                    Contains = true,
                    Value = @"(.*)\.(exe|vbs|jar|sh|bat|bin|scr)$"
                });

                Settings.Instance.SearchControl_SearchTreeItem.Items.Clear();
                Settings.Instance.SearchControl_SearchTreeItem.Items.Add(new SearchTreeItem()
                {
                    SearchItem = imageSearchItem
                });
                Settings.Instance.SearchControl_SearchTreeItem.Items.Add(new SearchTreeItem()
                {
                    SearchItem = videoSearchItem
                });
                Settings.Instance.SearchControl_SearchTreeItem.Items.Add(new SearchTreeItem()
                {
                    SearchItem = archiveSearchItem
                });
                Settings.Instance.SearchControl_SearchTreeItem.Items.Add(new SearchTreeItem()
                {
                    SearchItem = documentSearchItem
                });
                Settings.Instance.SearchControl_SearchTreeItem.Items.Add(new SearchTreeItem()
                {
                    SearchItem = audioSearchItem
                });
                Settings.Instance.SearchControl_SearchTreeItem.Items.Add(new SearchTreeItem()
                {
                    SearchItem = ExecutableSearchItem
                });

                Random random = new Random();
                _amoebaManager.ListenUris.Add(string.Format("tcp:{0}:{1}", IPAddress.Any.ToString(), random.Next(1024, 65536)));
                _amoebaManager.ListenUris.Add(string.Format("tcp:[{0}]:{1}", IPAddress.IPv6Any.ToString(), random.Next(1024, 65536)));

                var ipv4ConnectionFilter = new ConnectionFilter()
                {
                    ConnectionType = ConnectionType.Tcp,
                    UriCondition = new UriCondition()
                    {
                        Value = @"tcp:([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3}).*",
                    },
                };

                var ipv6ConnectionFilter = new ConnectionFilter()
                {
                    ConnectionType = ConnectionType.Tcp,
                    UriCondition = new UriCondition()
                    {
                        Value = @"tcp:\[(\d|:)*\].*",
                    },
                };

                var tcpConnectionFilter = new ConnectionFilter()
                {
                    ConnectionType = ConnectionType.Tcp,
                    UriCondition = new UriCondition()
                    {
                        Value = @"tcp:.*",
                    },
                };

                var torConnectionFilter = new ConnectionFilter()
                {
                    ConnectionType = ConnectionType.None,
                    ProxyUri = "tcp:127.0.0.1:9050",
                    UriCondition = new UriCondition()
                    {
                        Value = @"tor:.*",
                    },
                };

                _amoebaManager.Filters.Add(ipv4ConnectionFilter);
                _amoebaManager.Filters.Add(ipv6ConnectionFilter);
                _amoebaManager.Filters.Add(tcpConnectionFilter);
                _amoebaManager.Filters.Add(torConnectionFilter);
            }
            else
            {
                Version version;

                using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.version"), new UTF8Encoding(false)))
                {
                    version = new Version(reader.ReadLine());
                }
            }

            using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.version"), false, new UTF8Encoding(false)))
            {
                writer.WriteLine(App.AmoebaVersion.ToString());
            }

#if DEBUG
            if (File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Debug_NodeId.txt")))
            {
                using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Debug_NodeId.txt"), new UTF8Encoding(false)))
                {
                    var baseNode = new Node();
                    byte[] buffer = new byte[64];

                    byte b = byte.Parse(reader.ReadLine());

                    for (int i = 0; i < 64; i++)
                    {
                        buffer[i] = b;
                    }

                    baseNode.Id = buffer;
                    baseNode.Uris.AddRange(_amoebaManager.BaseNode.Uris);

                    _amoebaManager.BaseNode = baseNode;
                }
            }
#endif

            _autoBaseNodeSettingManager = new AutoBaseNodeSettingManager(_amoebaManager);
            _autoBaseNodeSettingManager.Load(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);

            SearchControl _searchControl = new SearchControl(_amoebaManager, _bufferManager);
            _searchControl.Height = Double.NaN;
            _searchControl.Width = Double.NaN;
            _searchTabItem.Content = _searchControl;

            ConnectionControl _connectionControl = new ConnectionControl(_amoebaManager);
            _connectionControl.Height = Double.NaN;
            _connectionControl.Width = Double.NaN;
            _connectionTabItem.Content = _connectionControl;

            DownloadControl _downloadControl = new DownloadControl(_amoebaManager, _bufferManager);
            _downloadControl.Height = Double.NaN;
            _downloadControl.Width = Double.NaN;
            _downloadTabItem.Content = _downloadControl;

            UploadControl _uploadControl = new UploadControl(_amoebaManager, _bufferManager);
            _uploadControl.Height = Double.NaN;
            _uploadControl.Width = Double.NaN;
            _uploadTabItem.Content = _uploadControl;

            ShareControl _shareControl = new ShareControl(_amoebaManager, _bufferManager);
            _shareControl.Height = Double.NaN;
            _shareControl.Width = Double.NaN;
            _shareTabItem.Content = _shareControl;

            LibraryControl _libraryControl = new LibraryControl(_amoebaManager, _bufferManager);
            _libraryControl.Height = Double.NaN;
            _libraryControl.Width = Double.NaN;
            _libraryTabItem.Content = _libraryControl;

            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ConnectionsInformationShow), this);
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.Timer), this);

            if (Settings.Instance.Global_IsStart)
            {
                _menuItemStart_Click(null, null);
            }

            _notifyIcon.Click += (object sender2, EventArgs e2) =>
            {
                this.Show();
                this.Activate();
                this.WindowState = _windowState;

                _notifyIcon.Visible = false;
            };

            System.Drawing.Icon myIcon = new System.Drawing.Icon(Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"));
            _notifyIcon.Visible = false;
            _notifyIcon.Icon = new System.Drawing.Icon(myIcon, new System.Drawing.Size(16, 16));
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();

                _notifyIcon.Visible = true;
            }
            else
            {
                _windowState = this.WindowState;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            NativeMethods.SetThreadExecutionState(ExecutionState.Continuous);

            _notifyIcon.Visible = false;

            _autoBaseNodeSettingManager.Stop();
            _autoBaseNodeSettingManager.Dispose();

            _amoebaManager.Save(_configrationDirectoryPaths["AmoebaManager"]);
            Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);

            _amoebaManager.Dispose();

            this.Dispose();
        }

        public static string GetUniqueFilePath(string path)
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

        public static string GetUniqueDirectoryPath(string path)
        {
            if (!Directory.Exists(path))
            {
                return path;
            }

            for (int index = 1; ; index++)
            {
                string text = string.Format(@"{0} ({1})",
                    path,
                    index);

                if (!Directory.Exists(text))
                {
                    return text;
                }
            }
        }

        private void ConnectionsInformationShow(object state)
        {
            long sentByteCount = 0;
            long receivedByteCount = 0;
            List<long> sentByteCountList = new List<long>(new long[3]);
            List<long> receivedByteCountList = new List<long>(new long[3]);
            int count = 0;

            for (; ; )
            {
                Thread.Sleep(1000);
                if (_disposed) return;

                try
                {
                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            sentByteCountList[count] = _amoebaManager.SentByteCount - sentByteCount;
                            sentByteCount = _amoebaManager.SentByteCount;
                            receivedByteCountList[count] = _amoebaManager.ReceivedByteCount - receivedByteCount;
                            receivedByteCount = _amoebaManager.ReceivedByteCount;
                            count++;
                            if (count >= sentByteCountList.Count) count = 0;

                            _sendSpeedTextBlock.Text = NetworkConverter.ToSizeString(sentByteCountList.Sum(n => n) / 3) + "/s";
                            _receiveSpeedTextBlock.Text = NetworkConverter.ToSizeString(receivedByteCountList.Sum(n => n) / 3) + "/s";
                        }
                        catch (Exception)
                        {

                        }
                    }), null);

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            if (_amoebaManager.State == ManagerState.Start)
                            {
                                _stateTextBlock.Text = "Start";
                            }
                            else
                            {
                                _stateTextBlock.Text = "Stop";
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }), null);
                }
                catch (Exception)
                {

                }
            }
        }

        private void Timer(object state)
        {
            Stopwatch spaceCheckStopwatch = new Stopwatch();
            Stopwatch backupStopwatch = new Stopwatch();
            Stopwatch updateStopwatch = new Stopwatch();
            spaceCheckStopwatch.Start();
            backupStopwatch.Start();
            updateStopwatch.Start();

            for (; ; )
            {
                Thread.Sleep(1000);
                if (_disposed) return;

                if (spaceCheckStopwatch.Elapsed > new TimeSpan(0, 1, 0))
                {
                    spaceCheckStopwatch.Restart();

                    try
                    {
                        DriveInfo drive = new DriveInfo(Directory.GetCurrentDirectory());

                        if (drive.AvailableFreeSpace < NetworkConverter.FromSizeString("256MB"))
                        {
                            if (_amoebaManager.State == ManagerState.Start)
                            {
                                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<object>(delegate(object state2)
                                {
                                    _menuItemStop_Click(null, null);
                                }), null);

                                Log.Warning(LanguagesManager.Instance.MainWindow_SpaceNotFound);
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                if (updateStopwatch.Elapsed > new TimeSpan(0, 5, 0))
                {
                    updateStopwatch.Restart();

                    if (Settings.Instance.Global_AutoUpdate_IsEnabled)
                    {
                        try
                        {
                            Regex regex = new Regex(@"Amoeba ((\d*)\.(\d*)\.(\d*)).*\.zip");
                            Version version = App.AmoebaVersion;
                            Seed updateKey = null;

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
                                    }
                                }
                            }

                            foreach (var key in _amoebaManager.Seeds)
                            {
                                if (key.Name.StartsWith("Amoeba") && App.UpdateSignature.Contains(MessageConverter.ToSignatureString(key.Certificate)))
                                {
                                    var match = regex.Match(key.Name);

                                    if (match.Success)
                                    {
                                        var tempVersion = new Version(match.Groups[1].Value);

                                        if (version < tempVersion)
                                        {
                                            version = tempVersion;
                                            updateKey = key;
                                        }
                                    }
                                }
                            }

                            if (updateKey != null)
                            {
                                _amoebaManager.Download(updateKey, App.DirectoryPaths["Update"]);
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                if (backupStopwatch.Elapsed > new TimeSpan(0, 5, 0))
                {
                    backupStopwatch.Restart();

                    try
                    {
                        _amoebaManager.Save(_configrationDirectoryPaths["AmoebaManager"]);
                        Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        private void Setting_Languages()
        {
            foreach (var item in LanguagesManager.Instance.Languages)
            {
                var menuItem = new MenuItem() { IsCheckable = true, Header = item };

                menuItem.Click += new RoutedEventHandler((object sender, RoutedEventArgs e) =>
                {
                    foreach (var item3 in _menuItemLanguages.Items.Cast<MenuItem>())
                    {
                        item3.IsChecked = false;
                    }

                    menuItem.IsChecked = true;
                });

                menuItem.Checked += new RoutedEventHandler((object sender, RoutedEventArgs e) =>
                {
                    Settings.Instance.Global_UseLanguage = (string)menuItem.Header;
                    LanguagesManager.ChangeLanguage((string)menuItem.Header);
                });

                _menuItemLanguages.Items.Add(menuItem);
            }

            var menuItem2 = _menuItemLanguages.Items.Cast<MenuItem>().FirstOrDefault(n => (string)n.Header == Settings.Instance.Global_UseLanguage);
            if (menuItem2 != null) menuItem2.IsChecked = true;
        }

        private void Setting_Log()
        {
            Directory.CreateDirectory(App.DirectoryPaths["Log"]);
            int logCount = 0;
            bool isHeaderWrite = true;

            if (_logPath == null)
            {
                do
                {
                    if (logCount == 0)
                    {
                        _logPath = Path.Combine(App.DirectoryPaths["Log"], string.Format("{0}.txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")));
                    }
                    else
                    {
                        _logPath = Path.Combine(App.DirectoryPaths["Log"], string.Format("{0}.({1}).txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"), logCount));
                    }

                    logCount++;
                } while (File.Exists(_logPath));
            }

            Log.LogEvent += new LogEventHandler((object sender, LogEventArgs e) =>
            {
                using (DeadlockMonitor.Lock(_logPath))
                {
                    if (e.MessageLevel == LogMessageLevel.Error || e.MessageLevel == LogMessageLevel.Warning)
                    {
                        using (var writer = new StreamWriter(_logPath, true, new UTF8Encoding(false)))
                        {
                            if (isHeaderWrite)
                            {
                                writer.WriteLine(this.GetMachineInfomation());
                                isHeaderWrite = false;
                            }

                            writer.WriteLine(string.Format(
                                "\r\n--------------------------------------------------------------------------------\r\n\r\n" +
                                "Time:\t\t{0}\r\n" +
                                "Level:\t\t{1}\r\n" +
                                "{2}",
                                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), e.MessageLevel, e.Message));
                            writer.Flush();
                        }
                    }
                }
            });
        }

        private void Setting_DebugLog()
        {
            Log.LogEvent += new LogEventHandler((object sender, LogEventArgs e) =>
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<object>(delegate(object state2)
                {
                    if (_logParagraph.Inlines.Count > 100)
                    {
                        _logParagraph.Inlines.Remove(_logParagraph.Inlines.FirstInline);
                    }

                    _logParagraph.Inlines.Add(string.Format("Log:\t{0} {1}:\t{2}\r\n",
                    DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), e.MessageLevel, e.Message));
                    _logRichTextBox.ScrollToEnd();
                }), null);
            });

            Debug.Listeners.Add(new MyTraceListener(this));
        }

        private class MyTraceListener : TraceListener
        {
            MainWindow _mainWindow;

            public MyTraceListener(MainWindow mainWindow)
            {
                _mainWindow = mainWindow;
            }

            public override void Write(string message)
            {
                _mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<object>(delegate(object state2)
                {
                    if (_mainWindow._logParagraph.Inlines.Count > 100)
                    {
                        _mainWindow._logParagraph.Inlines.Remove(_mainWindow._logParagraph.Inlines.FirstInline);
                    }

                    _mainWindow._logParagraph.Inlines.Add(
                        string.Format("Debug:\t{0} {1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), message));
                    _mainWindow._logRichTextBox.ScrollToEnd();
                }), null);
            }

            public override void WriteLine(string message)
            {
                _mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<object>(delegate(object state2)
                {
                    if (_mainWindow._logParagraph.Inlines.Count > 100)
                    {
                        _mainWindow._logParagraph.Inlines.Remove(_mainWindow._logParagraph.Inlines.FirstInline);
                    }

                    _mainWindow._logParagraph.Inlines.Add(
                        string.Format("Debug:\t{0} {1}\r\n", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), message));
                    _mainWindow._logRichTextBox.ScrollToEnd();
                }), null);
            }
        }

        private string GetMachineInfomation()
        {
            OperatingSystem osInfo = Environment.OSVersion;
            string osName = "";

            if (osInfo.Platform == PlatformID.Win32NT)
            {
                if (osInfo.Version.Major == 4)
                {
                    osName = "Windows NT 4.0";
                }
                else if (osInfo.Version.Major == 5)
                {
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osName = "Windows 2000";
                            break;

                        case 1:
                            osName = "Windows XP";
                            break;

                        case 2:
                            osName = "Windows Server 2003";
                            break;
                    }
                }
                else if (osInfo.Version.Major == 6)
                {
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osName = "Windows Vista";
                            break;

                        case 1:
                            osName = "Windows 7";
                            break;
                    }
                }
            }
            else if (osInfo.Platform == PlatformID.WinCE)
            {
                osName = "Windows CE";
            }
            else if (osInfo.Platform == PlatformID.MacOSX)
            {
                osName = "MacOSX";
            }
            else if (osInfo.Platform == PlatformID.Unix)
            {
                osName = "Unix";
            }

            return string.Format(
                "Amoeba:\t\t{0}\r\n" +
                "OS:\t\t{1} ({2})\r\n" +
                ".NET Framework:\t{3}", App.AmoebaVersion.ToString(3), osName, osInfo.VersionString, Environment.Version);
        }

        private void _menuItemStart_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
                {
                    _autoBaseNodeSettingManager.Start();
                }));
            }

            _amoebaManager.Start();

            _menuItemStart.IsEnabled = false;
            _menuItemStop.IsEnabled = true;

            Settings.Instance.Global_IsStart = true;
            Log.Information("Start");
        }

        private void _menuItemStop_Click(object sender, RoutedEventArgs e)
        {
            _autoBaseNodeSettingManager.Stop();

            _amoebaManager.Stop();

            _menuItemStart.IsEnabled = true;
            _menuItemStop.IsEnabled = false;

            Settings.Instance.Global_IsStart = false;
            Log.Information("Stop");
        }

        private void _menuItemSignatureSetting_Click(object sender, RoutedEventArgs e)
        {
            SignatureWindow window = new SignatureWindow(_bufferManager);
            window.ShowDialog();
        }

        private void _menuItemConnectionsSetting_Click(object sender, RoutedEventArgs e)
        {
            ConnectionsWindow window = new ConnectionsWindow(_amoebaManager, _autoBaseNodeSettingManager, _bufferManager);
            window.ShowDialog();
        }

        private void _menuItemVersionInformation_Click(object sender, RoutedEventArgs e)
        {
            VersionInformationWindow window = new VersionInformationWindow();
            window.ShowDialog();
        }

        private void _menuItemCheckingBlocks_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProgressWindow(true);
            window.Owner = this;
            window.Message1 = LanguagesManager.Instance.MainWindow_CheckingBlocks_Message;
            window.Message2 = string.Format(LanguagesManager.Instance.MainWindow_CheckingBlocks_State, 0, 0, 0);
            window.ButtonMessage = LanguagesManager.Instance.ProgressWindow_Cancel;

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                _amoebaManager.CheckBlocks((object sender2, int badBlockCount, int checkedBlockCount, int blockCount, out bool isStop) =>
                  {
                      bool flag = false;

                      this.Dispatcher.Invoke(DispatcherPriority.Background, new Action<object>(delegate(object state2)
                      {
                          try
                          {
                              window.Value = 100 * ((double)checkedBlockCount / (double)blockCount);
                          }
                          catch (Exception)
                          {

                          }

                          window.Message2 = string.Format(LanguagesManager.Instance.MainWindow_CheckingBlocks_State, badBlockCount, checkedBlockCount, blockCount);
                          if (window.DialogResult == true) flag = true;
                      }), null);

                      isStop = flag;
                  });

                this.Dispatcher.Invoke(DispatcherPriority.Background, new Action<object>(delegate(object state2)
                {
                    window.ButtonMessage = LanguagesManager.Instance.ProgressWindow_Ok;
                }), null);
            }));

            window.ShowDialog();
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    this.Close();

                    if (_lockStream != null)
                    {
                        _lockStream.Close();
                        _lockStream = null;
                    }
                }

                _disposed = true;
            }
        }

        #region IDisposable メンバ

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
