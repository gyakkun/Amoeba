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
    partial class MainWindow : Window
    {
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;
        private AutoBaseNodeSettingManager _autoBaseNodeSettingManager;

        System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        private WindowState _windowState;

        private Dictionary<string, string> _configrationDirectoryPaths = new Dictionary<string, string>();
        private string _logPath = null;

        private bool _isRun = true;
        
        private Thread _timerThread = null;

        public MainWindow()
        {
            _bufferManager = new BufferManager();

            this.Setting_Log();

            _configrationDirectoryPaths.Add("MainWindow", Path.Combine(App.DirectoryPaths["Configuration"], @"Amoeba/Properties/Settings"));
            _configrationDirectoryPaths.Add("AutoBaseNodeSettingManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Amoeba/AutoBaseNodeSettingManager"));
            _configrationDirectoryPaths.Add("AmoebaManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Library/Net/Amoeba/AmoebaManager"));

            Settings.Instance.Load(_configrationDirectoryPaths["MainWindow"]);

            InitializeComponent();

            _windowState = this.WindowState;

            this.Title = string.Format("Amoeba {0}", App.AmoebaVersion);

            using (FileStream stream = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            this.Setting_Languages();

            System.Drawing.Icon myIcon = new System.Drawing.Icon(Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"));
            _notifyIcon.Icon = new System.Drawing.Icon(myIcon, new System.Drawing.Size(16, 16));
            _notifyIcon.Visible = true;

            this.Setting_Init();

            _notifyIcon.Visible = false;
            _notifyIcon.Click += (object sender2, EventArgs e2) =>
            {
                this.Show();
                this.Activate();
                this.WindowState = _windowState;

                _notifyIcon.Visible = false;
            };

            _timerThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    Stopwatch spaceCheckStopwatch = new Stopwatch();
                    Stopwatch backupStopwatch = new Stopwatch();
                    Stopwatch updateStopwatch = new Stopwatch();
                    spaceCheckStopwatch.Start();
                    backupStopwatch.Start();

                    for (; ; )
                    {
                        Thread.Sleep(1000);
                        if (!_isRun) return;

                        {
                            if (_autoBaseNodeSettingManager.State == ManagerState.Stop
                                && (Settings.Instance.Global_IsStart && Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled))
                            {
                                _autoBaseNodeSettingManager.Start();
                            }
                            else if (_autoBaseNodeSettingManager.State == ManagerState.Start
                                && (!Settings.Instance.Global_IsStart || !Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled))
                            {
                                _autoBaseNodeSettingManager.Stop();
                            }

                            if (_amoebaManager.State == ManagerState.Stop
                                && Settings.Instance.Global_IsStart)
                            {
                                _amoebaManager.Start();

                                Log.Information("Start");
                            }
                            else if (_amoebaManager.State == ManagerState.Start
                                && !Settings.Instance.Global_IsStart)
                            {
                                _amoebaManager.Stop();

                                Log.Information("Stop");
                            }
                        }

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
                                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
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

                        if (backupStopwatch.Elapsed > new TimeSpan(0, 5, 0))
                        {
                            backupStopwatch.Restart();

                            try
                            {
                                _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
                                _amoebaManager.Save(_configrationDirectoryPaths["AmoebaManager"]);
                                Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);
                            }
                            catch (Exception)
                            {

                            }
                        }


                        if (!updateStopwatch.IsRunning && updateStopwatch.Elapsed > new TimeSpan(3, 0, 0, 0))
                        {
                            updateStopwatch.Restart();

                            try
                            {
                                if (Settings.Instance.Global_Update_Option == UpdateOption.AutoCheck
                                   || Settings.Instance.Global_Update_Option == UpdateOption.AutoUpdate)
                                {
                                    _menuItemUpdateCheck_Click(null, null);
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }

                    }
                }
                catch (Exception)
                {

                }
            }));
            _timerThread.Priority = ThreadPriority.Highest;
            _timerThread.Name = "TimerThread";
            _timerThread.Start();
        }

        private static string GetMachineInfomation()
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
                        _logPath = Path.Combine(App.DirectoryPaths["Log"], string.Format("{0}.txt", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                    }
                    else
                    {
                        _logPath = Path.Combine(App.DirectoryPaths["Log"], string.Format("{0}.({1}).txt", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo), logCount));
                    }

                    logCount++;
                } while (File.Exists(_logPath));
            }

            Log.LogEvent += new LogEventHandler((object sender, LogEventArgs e) =>
            {
                lock (_logPath)
                {
                    try
                    {
                        if (e.MessageLevel == LogMessageLevel.Error || e.MessageLevel == LogMessageLevel.Warning)
                        {
                            using (var writer = new StreamWriter(_logPath, true, new UTF8Encoding(false)))
                            {
                                if (isHeaderWrite)
                                {
                                    writer.WriteLine(MainWindow.GetMachineInfomation());
                                    isHeaderWrite = false;
                                }

                                writer.WriteLine(string.Format(
                                    "\r\n--------------------------------------------------------------------------------\r\n\r\n" +
                                    "Time:\t\t{0}\r\n" +
                                    "Level:\t\t{1}\r\n" +
                                    "{2}",
                                    DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"), e.MessageLevel, e.Message));
                                writer.Flush();
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            });

            Log.LogEvent += new LogEventHandler((object sender, LogEventArgs e) =>
            {
                try
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            _logParagraph.Inlines.Add(string.Format("{0} {1}:\t{2}\r\n", DateTime.Now.ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo), e.MessageLevel, e.Message));

                            if (_logParagraph.Inlines.Count > 100)
                            {
                                _logParagraph.Inlines.Remove(_logParagraph.Inlines.FirstInline);
                            }

                            _logRichTextBox.ScrollToEnd();
                        }
                        catch (Exception)
                        {

                        }
                    }), null);
                }
                catch (Exception)
                {

                }
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
                this.WriteLine(message);
            }

            public override void WriteLine(string message)
            {
                try
                {
                    _mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            _mainWindow._logParagraph.Inlines.Add(string.Format("{0} Debug:\t{1}\r\n", DateTime.Now.ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo), message));

                            if (_mainWindow._logParagraph.Inlines.Count > 100)
                            {
                                _mainWindow._logParagraph.Inlines.Remove(_mainWindow._logParagraph.Inlines.FirstInline);
                            }

                            _mainWindow._logRichTextBox.ScrollToEnd();
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

        private void Setting_Init()
        {
            NativeMethods.SetThreadExecutionState(ExecutionState.SystemRequired | ExecutionState.Continuous);

            {
                bool initFlag = false;

                _amoebaManager = new AmoebaManager(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.blocks"), _bufferManager);
                _amoebaManager.Load(_configrationDirectoryPaths["AmoebaManager"]);

                if (_amoebaManager.BaseNode == null || _amoebaManager.BaseNode.Id == null)
                {
                    byte[] buffer = new byte[64];
                    (new RNGCryptoServiceProvider()).GetBytes(buffer);

                    var baseNode = new Node();
                    baseNode.Id = buffer;

                    _amoebaManager.BaseNode = baseNode;
                }

                if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.version")))
                {
                    initFlag = true;

                    {
                        System.Diagnostics.ProcessStartInfo p = new System.Diagnostics.ProcessStartInfo();
                        p.UseShellExecute = true;
                        p.FileName = Path.Combine(App.DirectoryPaths["Core"], "Amoeba.exe");
                        p.Arguments = "Relate on";

                        OperatingSystem osInfo = Environment.OSVersion;

                        if (osInfo.Platform == PlatformID.Win32NT && osInfo.Version.Major >= 6)
                        {
                            p.Verb = "runas";
                        }

                        try
                        {
                            System.Diagnostics.Process.Start(p);
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {

                        }
                    }

                    Settings.Instance.Global_SearchKeywords.Clear();
                    Settings.Instance.Global_SearchKeywords.Add("Box");
                    Settings.Instance.Global_SearchKeywords.Add("Picture");
                    Settings.Instance.Global_SearchKeywords.Add("Movie");
                    Settings.Instance.Global_SearchKeywords.Add("Music");
                    Settings.Instance.Global_SearchKeywords.Add("Archive");
                    Settings.Instance.Global_SearchKeywords.Add("Document");
                    Settings.Instance.Global_SearchKeywords.Add("Executable");

                    Directory.CreateDirectory(Path.Combine(@"..\", "Download"));
                    _amoebaManager.DownloadDirectory = Path.Combine(@"..\", "Download");

                    _amoebaManager.ConnectionCountLimit = 12;
                    _amoebaManager.DownloadingConnectionCountLowerLimit = 3;
                    _amoebaManager.UploadingConnectionCountLowerLimit = 3;

                    Settings.Instance.Global_UploadKeywords.Clear();
                    Settings.Instance.Global_UploadKeywords.Add("Document");

                    SearchItem pictureSearchItem = new SearchItem()
                    {
                        Name = "Picture"
                    };
                    pictureSearchItem.SearchKeywordCollection.Add(new SearchContains<string>()
                    {
                        Contains = true,
                        Value = "Picture",
                    });

                    SearchItem movieSearchItem = new SearchItem()
                    {
                        Name = "Movie"
                    };
                    movieSearchItem.SearchKeywordCollection.Add(new SearchContains<string>()
                    {
                        Contains = true,
                        Value = "Movie",
                    });

                    SearchItem musicSearchItem = new SearchItem()
                    {
                        Name = "Music"
                    };
                    musicSearchItem.SearchKeywordCollection.Add(new SearchContains<string>()
                    {
                        Contains = true,
                        Value = "Music",
                    });

                    SearchItem archiveSearchItem = new SearchItem()
                    {
                        Name = "Archive"
                    };
                    archiveSearchItem.SearchKeywordCollection.Add(new SearchContains<string>()
                    {
                        Contains = true,
                        Value = "Archive",
                    });

                    SearchItem documentSearchItem = new SearchItem()
                    {
                        Name = "Document"
                    };
                    documentSearchItem.SearchKeywordCollection.Add(new SearchContains<string>()
                    {
                        Contains = true,
                        Value = "Document",
                    });

                    SearchItem ExecutableSearchItem = new SearchItem()
                    {
                        Name = "Executable"
                    };
                    ExecutableSearchItem.SearchKeywordCollection.Add(new SearchContains<string>()
                    {
                        Contains = true,
                        Value = "Executable",
                    });

                    Settings.Instance.CacheControl_SearchTreeItem.Items.Clear();
                    Settings.Instance.CacheControl_SearchTreeItem.Items.Add(new SearchTreeItem()
                    {
                        SearchItem = pictureSearchItem
                    });
                    Settings.Instance.CacheControl_SearchTreeItem.Items.Add(new SearchTreeItem()
                    {
                        SearchItem = movieSearchItem
                    });
                    Settings.Instance.CacheControl_SearchTreeItem.Items.Add(new SearchTreeItem()
                    {
                        SearchItem = musicSearchItem
                    });
                    Settings.Instance.CacheControl_SearchTreeItem.Items.Add(new SearchTreeItem()
                    {
                        SearchItem = archiveSearchItem
                    });
                    Settings.Instance.CacheControl_SearchTreeItem.Items.Add(new SearchTreeItem()
                    {
                        SearchItem = documentSearchItem
                    });
                    Settings.Instance.CacheControl_SearchTreeItem.Items.Add(new SearchTreeItem()
                    {
                        SearchItem = ExecutableSearchItem
                    });

                    Random random = new Random();
                    _amoebaManager.ListenUris.Clear();
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
                        ConnectionType = ConnectionType.Socks5Proxy,
                        ProxyUri = "tcp:127.0.0.1:9050",
                        UriCondition = new UriCondition()
                        {
                            Value = @"tor:.*",
                        },
                    };

                    var i2pConnectionFilter = new ConnectionFilter()
                    {
                        ConnectionType = ConnectionType.None,
                        UriCondition = new UriCondition()
                        {
                            Value = @"i2p:.*",
                        },
                    };

                    _amoebaManager.Filters.Clear();
                    _amoebaManager.Filters.Add(ipv4ConnectionFilter);
                    _amoebaManager.Filters.Add(ipv6ConnectionFilter);
                    _amoebaManager.Filters.Add(tcpConnectionFilter);
                    _amoebaManager.Filters.Add(torConnectionFilter);
                    _amoebaManager.Filters.Add(i2pConnectionFilter);
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

                if (initFlag)
                {
                    _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
                    _amoebaManager.Save(_configrationDirectoryPaths["AmoebaManager"]);
                    Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);
                }
            }
        }

        private WebProxy GetProxy()
        {
            var proxyUri = Settings.Instance.Global_Update_ProxyUri;

            if (!string.IsNullOrWhiteSpace(proxyUri))
            {
                string proxyScheme = null;
                string proxyHost = null;
                int proxyPort = -1;

                {
                    Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                    var match = regex.Match(proxyUri);

                    if (match.Success)
                    {
                        proxyScheme = match.Groups[1].Value;
                        proxyHost = match.Groups[2].Value;
                        proxyPort = int.Parse(match.Groups[3].Value);
                    }
                    else
                    {
                        Regex regex2 = new Regex(@"(.*?):(.*)");
                        var match2 = regex2.Match(proxyUri);

                        if (match2.Success)
                        {
                            proxyScheme = match2.Groups[1].Value;
                            proxyHost = match2.Groups[2].Value;
                            proxyPort = 80;
                        }
                    }
                }

                return new WebProxy(proxyHost, proxyPort);
            }

            return null;
        }

        private object _updateLockObject = new object();

        private void UpdateCheck(bool isShow)
        {
            lock (_updateLockObject)
            {
                try
                {
                    Version updateVersion = new Version();

                    if (File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.update")))
                    {
                        using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.update"), new UTF8Encoding(false)))
                        {
                            updateVersion = new Version(reader.ReadLine());
                        }
                    }

                    var url = Settings.Instance.Global_Update_Url;
                    var signature = Settings.Instance.Global_Update_Signature;

                    HttpWebRequest rq = (HttpWebRequest)HttpWebRequest.Create(url);
                    rq.Method = "GET";
                    rq.ContentType = "text/html; charset=UTF-8";
                    rq.UserAgent = "";
                    rq.ReadWriteTimeout = 1000 * 60;
                    rq.Timeout = 1000 * 60;
                    rq.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                    rq.KeepAlive = true;
                    rq.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");
                    rq.Proxy = this.GetProxy();

                    Seed seed;

                    using (HttpWebResponse rs = (HttpWebResponse)rq.GetResponse())
                    using (Stream stream = rs.GetResponseStream())
                    using (StreamReader r = new StreamReader(stream))
                    {
                        seed = AmoebaConverter.FromSeedString(r.ReadLine());
                    }

                    Regex regex3 = new Regex(@"Amoeba ((\d*)\.(\d*)\.(\d*)).*\.zip");
                    var match3 = regex3.Match(seed.Name);

                    if (match3.Success)
                    {
                        var tempVersion = new Version(match3.Groups[1].Value);

                        if (tempVersion <= App.AmoebaVersion)
                        {
                            if (!isShow) return;
                         
                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                            {
                                MessageBox.Show(
                                    this,
                                    LanguagesManager.Instance.MainWindow_LatestVersion_Message,
                                    "Update",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            }), null);
                        }
                        else
                        {
                            if (!isShow && tempVersion <= updateVersion) return;

                            if (!string.IsNullOrWhiteSpace(signature))
                            {
                                if (!seed.VerifyCertificate()) throw new Exception("Update VerifyCertificate");
                                if (!MessageConverter.ToSignatureString(seed.Certificate).StartsWith(signature)) throw new Exception("Update Signature");
                            }

                            bool flag = true;

                            if (Settings.Instance.Global_Update_Option != UpdateOption.AutoUpdate)
                            {
                                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                                {
                                    if (MessageBox.Show(
                                        this,
                                        string.Format(LanguagesManager.Instance.MainWindow_UpdateCheck_Message, Path.GetFileNameWithoutExtension(seed.Name)),
                                        "Update",
                                        MessageBoxButton.OKCancel,
                                        MessageBoxImage.Information) == MessageBoxResult.Cancel)
                                    {
                                        flag = false;
                                    }
                                }), null);
                            }

                            using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.update"), false, new UTF8Encoding(false)))
                            {
                                writer.WriteLine(tempVersion.ToString());
                            }

                            if (flag)
                            {
                                _amoebaManager.Download(seed, App.DirectoryPaths["Update"], 6);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
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

                try
                {
                    sentByteCountList[count] = _amoebaManager.SentByteCount - sentByteCount;
                    sentByteCount = _amoebaManager.SentByteCount;
                    receivedByteCountList[count] = _amoebaManager.ReceivedByteCount - receivedByteCount;
                    receivedByteCount = _amoebaManager.ReceivedByteCount;
                    count++;
                    if (count >= sentByteCountList.Count) count = 0;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            _sendSpeedTextBlock.Text = NetworkConverter.ToSizeString(sentByteCountList.Sum(n => n) / 3) + "/s";
                            _receiveSpeedTextBlock.Text = NetworkConverter.ToSizeString(receivedByteCountList.Sum(n => n) / 3) + "/s";
                        }
                        catch (Exception)
                        {

                        }
                    }), null);

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            TopRelativeDoubleConverter.GetDoubleEvent = (object state) =>
            {
                return this.PointToScreen(new Point(0, 0)).Y;
            };

            LeftRelativeDoubleConverter.GetDoubleEvent = (object state) =>
            {
                return this.PointToScreen(new Point(0, 0)).X;
            };

            CacheControl _cacheControl = new CacheControl(this, _amoebaManager, _bufferManager);
            _cacheControl.Height = Double.NaN;
            _cacheControl.Width = Double.NaN;
            _cacheTabItem.Content = _cacheControl;

            ConnectionControl _connectionControl = new ConnectionControl(_amoebaManager);
            _connectionControl.Height = Double.NaN;
            _connectionControl.Width = Double.NaN;
            _connectionTabItem.Content = _connectionControl;

            DownloadControl _downloadControl = new DownloadControl(_amoebaManager, _bufferManager);
            _downloadControl.Height = Double.NaN;
            _downloadControl.Width = Double.NaN;
            _downloadTabItem.Content = _downloadControl;

            UploadControl _uploadControl = new UploadControl(this, _amoebaManager, _bufferManager);
            _uploadControl.Height = Double.NaN;
            _uploadControl.Width = Double.NaN;
            _uploadTabItem.Content = _uploadControl;

            ShareControl _shareControl = new ShareControl(this, _amoebaManager, _bufferManager);
            _shareControl.Height = Double.NaN;
            _shareControl.Width = Double.NaN;
            _shareTabItem.Content = _shareControl;

            LibraryControl _libraryControl = new LibraryControl(this, _amoebaManager, _bufferManager);
            _libraryControl.Height = Double.NaN;
            _libraryControl.Width = Double.NaN;
            _libraryTabItem.Content = _libraryControl;

            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ConnectionsInformationShow), this);

            if (Settings.Instance.Global_IsStart)
            {
                _menuItemStart_Click(null, null);
            }

            ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
            {
                Thread.Sleep(1000 * 60);

                try
                {
                    if (Settings.Instance.Global_Update_Option == UpdateOption.AutoCheck
                       || Settings.Instance.Global_Update_Option == UpdateOption.AutoUpdate)
                    {
                        _menuItemUpdateCheck_Click(null, null);
                    }
                }
                catch (Exception)
                {

                }
            }));
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            NativeMethods.SetThreadExecutionState(ExecutionState.Continuous);

            _notifyIcon.Visible = false;

            _isRun = false;

            _timerThread.Join();
            _timerThread = null;

            _autoBaseNodeSettingManager.Stop();
            _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
            _autoBaseNodeSettingManager.Dispose();

            _amoebaManager.Stop();
            _amoebaManager.Save(_configrationDirectoryPaths["AmoebaManager"]);
            _amoebaManager.Dispose();

            Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);
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

            _logRichTextBox.ScrollToEnd();
        }

        private void _tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tabItem = _tabControl.SelectedItem as TabItem;

            if ((string)tabItem.Header == LanguagesManager.Instance.MainWindow_Connection)
            {
                App.SelectTab = "Connection";
            }
            else if ((string)tabItem.Header == LanguagesManager.Instance.MainWindow_Cache)
            {
                App.SelectTab = "Search";
            }
            else if ((string)tabItem.Header == LanguagesManager.Instance.MainWindow_Download)
            {
                App.SelectTab = "Download";
            }
            else if ((string)tabItem.Header == LanguagesManager.Instance.MainWindow_Upload)
            {
                App.SelectTab = "Upload";
            }
            else if ((string)tabItem.Header == LanguagesManager.Instance.MainWindow_Share)
            {
                App.SelectTab = "Share";
            }
            else if ((string)tabItem.Header == LanguagesManager.Instance.MainWindow_Library)
            {
                App.SelectTab = "Library";
            }
            else if ((string)tabItem.Header == LanguagesManager.Instance.MainWindow_Log)
            {
                App.SelectTab = "Log";
            }
            else
            {
                App.SelectTab = "";
            }

            _logRichTextBox.ScrollToEnd();
            this.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
        }

        private void _menuItemStart_Click(object sender, RoutedEventArgs e)
        {
            _menuItemStart.IsEnabled = false;
            _menuItemStop.IsEnabled = true;

            Settings.Instance.Global_IsStart = true;
        }

        private void _menuItemStop_Click(object sender, RoutedEventArgs e)
        {
            _menuItemStart.IsEnabled = true;
            _menuItemStop.IsEnabled = false;

            Settings.Instance.Global_IsStart = false;
        }

        private void _menuItemConnectionSetting_Click(object sender, RoutedEventArgs e)
        {
            ConnectionWindow window = new ConnectionWindow(_amoebaManager, _autoBaseNodeSettingManager, _bufferManager);
            window.Owner = this;
            window.ShowDialog();
        }

        private void _menuItemUserInterfaceSetting_Click(object sender, RoutedEventArgs e)
        {
            UserInterfaceWindow window = new UserInterfaceWindow(_bufferManager);
            window.Owner = this;
            window.ShowDialog();
        }

        private void _menuItemUpdateCheck_Click(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
            {
                Thread.CurrentThread.IsBackground = true;

                this.UpdateCheck(sender != null);
            }));
        }

        private void _menuItemDeveloperSiteCheck_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://lyrise.i2p.to/projects/trac/");
        }

        private void _menuItemManualSiteCheck_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://lyrise.web.fc2.com/");
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

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
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

                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                {
                    window.ButtonMessage = LanguagesManager.Instance.ProgressWindow_Ok;
                }), null);
            }));

            window.Owner = this;
            window.ShowDialog();
        }

        private void _menuItemVersionInformation_Click(object sender, RoutedEventArgs e)
        {
            VersionInformationWindow window = new VersionInformationWindow();
            window.Owner = this;
            window.ShowDialog();
        }
    }
}
