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
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Amoeba.Properties;
using Library;
using Library.Collections;
using Library.Io;
using Library.Net.Amoeba;
using Library.Net.Connections;
using Library.Net.Proxy;
using Library.Net.Upnp;
using Library.Security;

namespace Amoeba.Windows
{
    delegate void DebugLog(string message);

    enum MainWindowTabType
    {
        Information,
        Search,
        Download,
        Upload,
        Share,
        Store,
        Log,
    }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    partial class MainWindow : Window
    {
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;
        private AutoBaseNodeSettingManager _autoBaseNodeSettingManager;
        private OverlayNetworkManager _overlayNetworkManager;
        private TransfarLimitManager _transferLimitManager;
        private CatharsisManager _catharsisManager;

        private Random _random = new Random();

        private System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        private WindowState _windowState;

        private Dictionary<string, string> _configrationDirectoryPaths = new Dictionary<string, string>();
        private string _logPath;

        private volatile bool _closed = false;
        private bool _autoStop;

        private Thread _timerThread;
        private Thread _watchThread;
        private Thread _statusBarThread;
        private Thread _trafficMonitorThread;

        private volatile bool _diskSpaceNotFoundException;
        private volatile bool _cacheSpaceNotFoundException;

        private volatile MainWindowTabType _selectedTab;

        [FlagsAttribute]
        enum ExecutionState : uint
        {
            Null = 0,
            SystemRequired = 1,
            DisplayRequired = 2,
            Continuous = 0x80000000,
        }

        static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public extern static ExecutionState SetThreadExecutionState(ExecutionState esFlags);
        }

        public MainWindow()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                _bufferManager = BufferManager.Instance;

                this.Setting_Log();

                _configrationDirectoryPaths.Add("MainWindow", Path.Combine(App.DirectoryPaths["Configuration"], @"Amoeba/Properties/Settings"));
                _configrationDirectoryPaths.Add("AmoebaManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Library/Net/Amoeba/AmoebaManager"));
                _configrationDirectoryPaths.Add("AutoBaseNodeSettingManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Amoeba/AutoBaseNodeSettingManager"));
                _configrationDirectoryPaths.Add("OverlayNetworkManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Amoeba/OverlayNetworkManager"));
                _configrationDirectoryPaths.Add("TransfarLimitManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Amoeba/TransfarLimitManager"));
                _configrationDirectoryPaths.Add("CatharsisManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Amoeba/CatharsisManager"));

                Settings.Instance.Load(_configrationDirectoryPaths["MainWindow"]);

                InitializeComponent();

                this.Title = string.Format("Amoeba {0}", App.AmoebaVersion);

                {
                    var icon = new BitmapImage();

                    icon.BeginInit();
                    icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    icon.EndInit();
                    if (icon.CanFreeze) icon.Freeze();

                    this.Icon = icon;
                }

                System.Drawing.Icon myIcon = new System.Drawing.Icon(Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"));
                _notifyIcon.Icon = new System.Drawing.Icon(myIcon, new System.Drawing.Size(16, 16));
                _notifyIcon.Visible = true;

                this.Setting_Init();
                this.Setting_Languages();

                _notifyIcon.Visible = false;
                _notifyIcon.Click += (object sender2, EventArgs e2) =>
                {
                    if (_closed) return;

                    try
                    {
                        this.Show();
                        this.Activate();
                        this.WindowState = _windowState;

                        _notifyIcon.Visible = false;
                    }
                    catch (Exception)
                    {

                    }
                };

                _timerThread = new Thread(this.TimerThread);
                _timerThread.Priority = ThreadPriority.Lowest;
                _timerThread.Name = "MainWindow_TimerThread";
                _timerThread.Start();

                _watchThread = new Thread(this.WatchThread);
                _watchThread.Priority = ThreadPriority.Lowest;
                _watchThread.Name = "MainWindow_WatchThread";
                _watchThread.Start();

                _statusBarThread = new Thread(this.StatusBarThread);
                _statusBarThread.Priority = ThreadPriority.Highest;
                _statusBarThread.Name = "MainWindow_StatusBarThread";
                _statusBarThread.Start();

                _trafficMonitorThread = new Thread(this.TrafficMonitorThread);
                _trafficMonitorThread.Priority = ThreadPriority.Highest;
                _trafficMonitorThread.Name = "MainWindow_TrafficMonitorThread";
                _trafficMonitorThread.Start();

                _transferLimitManager.StartEvent += _transferLimitManager_StartEvent;
                _transferLimitManager.StopEvent += _transferLimitManager_StopEvent;

#if !DEBUG
                _logRowDefinition.Height = new GridLength(0);
#endif

                sw.Stop();
                Debug.WriteLine("StartUp {0}", sw.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                Log.Error(e);

                throw;
            }
        }

        public MainWindowTabType SelectedTab
        {
            get
            {
                return _selectedTab;
            }
            private set
            {
                _selectedTab = value;
            }
        }

        void _transferLimitManager_StartEvent(object sender, EventArgs e)
        {
            if (_autoStop && !Settings.Instance.Global_IsStart)
            {
                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                {
                    _startMenuItem_Click(sender, null);
                }));
            }
        }

        void _transferLimitManager_StopEvent(object sender, EventArgs e)
        {
            Log.Information(LanguagesManager.Instance.MainWindow_TransferLimit_Message);

            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
            {
                _stopMenuItem_Click(sender, null);
            }));
        }

        public static void CopyDirectory(string sourceDirectoryPath, string destDirectoryPath)
        {
            if (!Directory.Exists(destDirectoryPath))
            {
                Directory.CreateDirectory(destDirectoryPath);
                File.SetAttributes(destDirectoryPath, File.GetAttributes(sourceDirectoryPath));
            }

            foreach (string file in Directory.GetFiles(sourceDirectoryPath))
            {
                File.Copy(file, Path.Combine(destDirectoryPath, Path.GetFileName(file)), true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDirectoryPath))
            {
                CopyDirectory(dir, Path.Combine(destDirectoryPath, Path.GetFileName(dir)));
            }
        }

        private void TimerThread()
        {
            try
            {
                Stopwatch spaceCheckStopwatch = new Stopwatch();
                Stopwatch backupStopwatch = new Stopwatch();
                Stopwatch updateStopwatch = new Stopwatch();
                Stopwatch uriUpdateStopwatch = new Stopwatch();
                Stopwatch compactionStopwatch = new Stopwatch();
                Stopwatch garbageCollectStopwatch = new Stopwatch();

                spaceCheckStopwatch.Start();
                backupStopwatch.Start();
                updateStopwatch.Start();
                uriUpdateStopwatch.Start();
                compactionStopwatch.Start();
                garbageCollectStopwatch.Start();

                for (; ; )
                {
                    Thread.Sleep(1000);
                    if (_closed) return;

                    {
                        if (_diskSpaceNotFoundException || _cacheSpaceNotFoundException)
                        {
                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                            {
                                _stopMenuItem_Click(null, null);
                                _encodeAndDecodeStopMenuItem_Click(null, null);
                            }));
                        }

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

                        if (_overlayNetworkManager.State == ManagerState.Stop
                            && (Settings.Instance.Global_IsStart && Settings.Instance.Global_I2p_SamBridge_IsEnabled))
                        {
                            _overlayNetworkManager.Start();
                        }
                        else if (_overlayNetworkManager.State == ManagerState.Start
                            && (!Settings.Instance.Global_IsStart || !Settings.Instance.Global_I2p_SamBridge_IsEnabled))
                        {
                            _overlayNetworkManager.Stop();
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

                        if (Settings.Instance.Global_IsConvertStart)
                        {
                            _amoebaManager.EncodeStart();
                            _amoebaManager.DecodeStart();
                        }
                        else if (!Settings.Instance.Global_IsConvertStart)
                        {
                            _amoebaManager.EncodeStop();
                            _amoebaManager.DecodeStop();
                        }

                        if (_diskSpaceNotFoundException)
                        {
                            Log.Warning(LanguagesManager.Instance.MainWindow_DiskSpaceNotFound_Message);

                            this.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                            {
                                MessageBox.Show(
                                    this,
                                    LanguagesManager.Instance.MainWindow_DiskSpaceNotFound_Message,
                                    "Warning",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                            }));

                            _diskSpaceNotFoundException = false;
                        }

                        if (_cacheSpaceNotFoundException)
                        {
                            Log.Warning(LanguagesManager.Instance.MainWindow_CacheSpaceNotFound_Message);

                            this.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                            {
                                MessageBox.Show(
                                    this,
                                    LanguagesManager.Instance.MainWindow_CacheSpaceNotFound_Message,
                                    "Warning",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                            }));

                            _cacheSpaceNotFoundException = false;
                        }
                    }

                    if (Settings.Instance.Global_IsStart && spaceCheckStopwatch.Elapsed.TotalMinutes >= 1)
                    {
                        spaceCheckStopwatch.Restart();

                        try
                        {
                            DriveInfo drive = new DriveInfo(Directory.GetCurrentDirectory());

                            if (drive.AvailableFreeSpace < NetworkConverter.FromSizeString("256MB"))
                            {
                                _diskSpaceNotFoundException = true;
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e);
                        }

                        try
                        {
                            if (!string.IsNullOrWhiteSpace(App.Cache.Path))
                            {
                                DriveInfo drive = new DriveInfo(Path.GetDirectoryName(Path.GetFullPath(App.Cache.Path)));

                                if (drive.AvailableFreeSpace < NetworkConverter.FromSizeString("256MB"))
                                {
                                    _diskSpaceNotFoundException = true;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e);
                        }
                    }

                    if (backupStopwatch.Elapsed.TotalMinutes >= 30)
                    {
                        backupStopwatch.Restart();

                        try
                        {
                            _catharsisManager.Save(_configrationDirectoryPaths["CatharsisManager"]);
                            _transferLimitManager.Save(_configrationDirectoryPaths["TransfarLimitManager"]);
                            _overlayNetworkManager.Save(_configrationDirectoryPaths["OverlayNetworkManager"]);
                            _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
                            _amoebaManager.Save(_configrationDirectoryPaths["AmoebaManager"]);
                            Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e);
                        }
                    }

                    if (updateStopwatch.Elapsed.TotalDays >= 1)
                    {
                        updateStopwatch.Restart();

                        try
                        {
                            if (Settings.Instance.Global_Update_Option == UpdateOption.AutoCheck
                               || Settings.Instance.Global_Update_Option == UpdateOption.AutoUpdate)
                            {
                                _checkUpdateMenuItem_Click(null, null);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e);
                        }
                    }

                    if (uriUpdateStopwatch.Elapsed.TotalHours >= 1)
                    {
                        uriUpdateStopwatch.Restart();

                        try
                        {
                            _autoBaseNodeSettingManager.Update();
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e);
                        }
                    }

                    if (garbageCollectStopwatch.Elapsed.TotalSeconds >= 60)
                    {
                        garbageCollectStopwatch.Restart();

                        try
                        {
                            this.GarbageCollect();
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void GarbageCollect()
        {
            // LargeObjectHeapCompactionModeの設定を試みる。(.net 4.5.1以上で可能)
            try
            {
                var type = typeof(System.Runtime.GCSettings);
                var property = type.GetProperty("LargeObjectHeapCompactionMode", BindingFlags.Static | BindingFlags.Public);

                if (null != property)
                {
                    var Setter = property.GetSetMethod();
                    Setter.Invoke(null, new object[] { /* GCLargeObjectHeapCompactionMode.CompactOnce */ 2 });

                    Debug.WriteLine("Set GCLargeObjectHeapCompactionMode.CompactOnce");
                }
            }
            catch (Exception)
            {

            }

            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception)
            {

            }
        }

        private void WatchThread()
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                for (; ; )
                {
                    Thread.Sleep(1000);
                    if (_closed) return;

                    if (Settings.Instance.Global_IsStart && stopwatch.Elapsed.TotalSeconds >= 120)
                    {
                        stopwatch.Restart();

                        // SearchSignaturesの更新
                        {
                            var searchSignatures = new HashSet<string>();

                            // クリップボード上にあるデータも考慮する。
                            {
                                var storeTreeItems = new List<StoreTreeItem>();
                                storeTreeItems.AddRange(Clipboard.GetStoreTreeItems());

                                {
                                    var storeCategorizeTreeItems = new List<StoreCategorizeTreeItem>();
                                    storeCategorizeTreeItems.AddRange(Clipboard.GetStoreCategorizeTreeItems());

                                    for (int i = 0; i < storeCategorizeTreeItems.Count; i++)
                                    {
                                        storeCategorizeTreeItems.AddRange(storeCategorizeTreeItems[i].Children);
                                        storeTreeItems.AddRange(storeCategorizeTreeItems[i].StoreTreeItems);
                                    }
                                }

                                searchSignatures.UnionWith(storeTreeItems.Select(n => n.Signature));
                            }

                            {
                                var storeTreeItems = new List<StoreTreeItem>();

                                {
                                    var storeCategorizeTreeItems = new List<StoreCategorizeTreeItem>();
                                    storeCategorizeTreeItems.Add(Settings.Instance.StoreDownloadControl_StoreCategorizeTreeItem);

                                    for (int i = 0; i < storeCategorizeTreeItems.Count; i++)
                                    {
                                        storeCategorizeTreeItems.AddRange(storeCategorizeTreeItems[i].Children);
                                        storeTreeItems.AddRange(storeCategorizeTreeItems[i].StoreTreeItems);
                                    }
                                }

                                searchSignatures.UnionWith(storeTreeItems.Select(n => n.Signature));
                            }

                            foreach (var linkItem in Settings.Instance.LinkOptionsWindow_DownloadLinkItems)
                            {
                                searchSignatures.Add(linkItem.Signature);
                                searchSignatures.UnionWith(linkItem.TrustSignatures);
                            }

                            lock (_amoebaManager.ThisLock)
                            {
                                _amoebaManager.SetSearchSignatures(searchSignatures);
                            }
                        }

                        // TrustSignaturesの更新
                        foreach (var item in Settings.Instance.LinkOptionsWindow_DownloadLinkItems)
                        {
                            var link = _amoebaManager.GetLink(item.Signature);
                            if (link == null || CollectionUtilities.Equals(item.TrustSignatures, link.TrustSignatures)) continue;

                            lock (item.ThisLock)
                            {
                                lock (item.TrustSignatures.ThisLock)
                                {
                                    item.TrustSignatures.Clear();
                                    item.TrustSignatures.AddRange(link.TrustSignatures);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void StatusBarThread()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(1000);
                    if (_closed) return;

                    var state = _amoebaManager.State;
                    var encodeState = _amoebaManager.EncodeState;
                    var decodeState = _amoebaManager.DecodeState;

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new TimeSpan(0, 0, 1), new Action(() =>
                    {
                        try
                        {
                            decimal sentAverageTraffic;

                            lock (_sentInfomation.ThisLock)
                            {
                                sentAverageTraffic = _sentInfomation.AverageTrafficList.Sum() / _sentInfomation.AverageTrafficList.Length;
                            }

                            decimal receivedAverageTraffic;

                            lock (_receivedInfomation.ThisLock)
                            {
                                receivedAverageTraffic = _receivedInfomation.AverageTrafficList.Sum() / _receivedInfomation.AverageTrafficList.Length;
                            }

                            _sendSpeedTextBlock.Text = NetworkConverter.ToSizeString(sentAverageTraffic) + "/s";
                            _receiveSpeedTextBlock.Text = NetworkConverter.ToSizeString(receivedAverageTraffic) + "/s";
                        }
                        catch (Exception)
                        {

                        }

                        try
                        {
                            string coreText = null;
                            string convertText = null;

                            if (state == ManagerState.Start) coreText = LanguagesManager.Instance.MainWindow_Running;
                            else coreText = LanguagesManager.Instance.MainWindow_Stopping;

                            if (encodeState == ManagerState.Start && decodeState == ManagerState.Start) convertText = LanguagesManager.Instance.MainWindow_Running;
                            else convertText = LanguagesManager.Instance.MainWindow_Stopping;

                            _stateTextBlock.Text = string.Format(LanguagesManager.Instance.MainWindow_StatesBar, coreText, convertText);
                        }
                        catch (Exception)
                        {

                        }
                    }));
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private TrafficInformation _sentInfomation = new TrafficInformation();
        private TrafficInformation _receivedInfomation = new TrafficInformation();

        private void TrafficMonitorThread()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                while (!_closed)
                {
                    Thread.Sleep(((int)Math.Max(2, 1000 - sw.ElapsedMilliseconds)) / 2);
                    if (sw.ElapsedMilliseconds < 1000) continue;

                    var receivedByteCount = _amoebaManager.ReceivedByteCount;
                    var sentByteCount = _amoebaManager.SentByteCount;

                    lock (_sentInfomation.ThisLock)
                    {
                        _sentInfomation.AverageTrafficList[_sentInfomation.Round++]
                            = ((decimal)(sentByteCount - _sentInfomation.PreviousTraffic)) * 1000 / sw.ElapsedMilliseconds;
                        _sentInfomation.PreviousTraffic = sentByteCount;

                        if (_sentInfomation.Round >= _sentInfomation.AverageTrafficList.Length)
                        {
                            _sentInfomation.Round = 0;
                        }
                    }

                    lock (_receivedInfomation.ThisLock)
                    {
                        _receivedInfomation.AverageTrafficList[_receivedInfomation.Round++]
                            = ((decimal)(receivedByteCount - _receivedInfomation.PreviousTraffic)) * 1000 / sw.ElapsedMilliseconds;
                        _receivedInfomation.PreviousTraffic = receivedByteCount;

                        if (_receivedInfomation.Round >= _receivedInfomation.AverageTrafficList.Length)
                        {
                            _receivedInfomation.Round = 0;
                        }
                    }

                    sw.Restart();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private class TrafficInformation : IThisLock
        {
            private decimal[] _averageTrafficList = new decimal[3];

            private readonly object _thisLock = new object();

            public long PreviousTraffic { get; set; }

            public int Round { get; set; }

            public decimal[] AverageTrafficList
            {
                get
                {
                    return _averageTrafficList;
                }
            }

            #region IThisLock

            public object ThisLock
            {
                get
                {
                    return _thisLock;
                }
            }

            #endregion
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

                        case 2:
                            osName = "Windows 8";
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
                        _logPath = Path.Combine(App.DirectoryPaths["Log"], string.Format("{0}.txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                    }
                    else
                    {
                        _logPath = Path.Combine(App.DirectoryPaths["Log"], string.Format("{0}.({1}).txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo), logCount));
                    }

                    logCount++;
                } while (File.Exists(_logPath));
            }

            Log.LogEvent += (object sender, LogEventArgs e) =>
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
                                    DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), e.MessageLevel, e.Message));
                                writer.Flush();
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            };

            Log.LogEvent += (object sender, LogEventArgs e) =>
            {
                if (e.Exception != null && e.Exception.GetType().ToString() == "Library.Net.Amoeba.SpaceNotFoundException")
                {
                    if (Settings.Instance.Global_IsStart)
                        _cacheSpaceNotFoundException = true;
                }

                try
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        if (_logCheckBox.IsChecked.Value)
                        {
                            try
                            {
                                if (_logListBox.Items.Count > 100)
                                {
                                    _logListBox.Items.RemoveAt(0);
                                }

                                _logListBox.Items.Add(string.Format("{0} {1}:\t{2}", DateTime.Now.ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo), e.MessageLevel, e.Message));
                                _logListBox.GoBottom();
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }));
                }
                catch (Exception)
                {

                }
            };

            Debug.Listeners.Add(new MyTraceListener((string message) =>
            {
                try
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        if (_debugCheckBox.IsChecked.Value)
                        {
                            try
                            {
                                if (_logListBox.Items.Count > 100)
                                {
                                    _logListBox.Items.RemoveAt(0);
                                }

                                _logListBox.Items.Add(string.Format("{0} Debug:\t{1}", DateTime.Now.ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo), message));
                                _logListBox.GoBottom();
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }));
                }
                catch (Exception)
                {

                }
            }));
        }

        private void _logListBox_Loaded(object sender, RoutedEventArgs e)
        {
            _logListBox.GoBottom();
        }

        private class MyTraceListener : TraceListener
        {
            private DebugLog _debugLog;

            public MyTraceListener(DebugLog debugLog)
            {
                _debugLog = debugLog;
            }

            public override void Write(string message)
            {
                this.WriteLine(message);
            }

            public override void WriteLine(string message)
            {
                _debugLog(message);
            }
        }

        private void Setting_Languages()
        {
            foreach (var language in LanguagesManager.Instance.Languages)
            {
                var menuItem = new LanguageMenuItem() { IsCheckable = true, Value = language };

                menuItem.Click += (object sender, RoutedEventArgs e) =>
                {
                    foreach (var item in _languagesMenuItem.Items.Cast<LanguageMenuItem>())
                    {
                        item.IsChecked = false;
                    }

                    menuItem.IsChecked = true;
                };

                menuItem.Checked += (object sender, RoutedEventArgs e) =>
                {
                    Settings.Instance.Global_UseLanguage = (string)menuItem.Value;
                    LanguagesManager.ChangeLanguage((string)menuItem.Value);
                };

                _languagesMenuItem.Items.Add(menuItem);
            }

            {
                var menuItem = _languagesMenuItem.Items.Cast<LanguageMenuItem>().FirstOrDefault(n => n.Value == Settings.Instance.Global_UseLanguage);
                if (menuItem != null) menuItem.IsChecked = true;
            }
        }

        private void Setting_Init()
        {
            NativeMethods.SetThreadExecutionState(ExecutionState.SystemRequired | ExecutionState.Continuous);

            {
                bool initFlag = false;

                _amoebaManager = new AmoebaManager(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.bitmap"), App.Cache.Path, _bufferManager);
                _amoebaManager.Load(_configrationDirectoryPaths["AmoebaManager"]);

                if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.version")))
                {
                    initFlag = true;

                    {
                        System.Diagnostics.ProcessStartInfo p = new System.Diagnostics.ProcessStartInfo();
                        p.UseShellExecute = true;
                        p.FileName = Path.Combine(App.DirectoryPaths["Core"], "Amoeba.exe");
                        p.Arguments = "Relate on";

                        OperatingSystem osInfo = Environment.OSVersion;

                        // Windows Vista以上。
                        if (osInfo.Platform == PlatformID.Win32NT && osInfo.Version >= new Version(6, 0))
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

                    _amoebaManager.ConnectionCountLimit = 32;

                    Settings.Instance.Global_UploadKeywords.Clear();
                    Settings.Instance.Global_UploadKeywords.Add("Document");

                    SearchItem pictureSearchItem = new SearchItem()
                    {
                        Name = "Keyword - \"Picture\""
                    };
                    pictureSearchItem.SearchKeywordCollection.Add(new SearchContains<string>(true, "Picture"));

                    SearchItem movieSearchItem = new SearchItem()
                    {
                        Name = "Keyword - \"Movie\""
                    };
                    movieSearchItem.SearchKeywordCollection.Add(new SearchContains<string>(true, "Movie"));

                    SearchItem musicSearchItem = new SearchItem()
                    {
                        Name = "Keyword - \"Music\""
                    };
                    musicSearchItem.SearchKeywordCollection.Add(new SearchContains<string>(true, "Music"));

                    SearchItem archiveSearchItem = new SearchItem()
                    {
                        Name = "Keyword - \"Archive\""
                    };
                    archiveSearchItem.SearchKeywordCollection.Add(new SearchContains<string>(true, "Archive"));

                    SearchItem documentSearchItem = new SearchItem()
                    {
                        Name = "Keyword - \"Document\""
                    };
                    documentSearchItem.SearchKeywordCollection.Add(new SearchContains<string>(true, "Document"));

                    SearchItem ExecutableSearchItem = new SearchItem()
                    {
                        Name = "Keyword - \"Executable\""
                    };
                    ExecutableSearchItem.SearchKeywordCollection.Add(new SearchContains<string>(true, "Executable"));

                    Settings.Instance.SearchControl_SearchTreeItem.Children.Clear();
                    Settings.Instance.SearchControl_SearchTreeItem.Children.Add(new SearchTreeItem()
                    {
                        SearchItem = pictureSearchItem
                    });
                    Settings.Instance.SearchControl_SearchTreeItem.Children.Add(new SearchTreeItem()
                    {
                        SearchItem = movieSearchItem
                    });
                    Settings.Instance.SearchControl_SearchTreeItem.Children.Add(new SearchTreeItem()
                    {
                        SearchItem = musicSearchItem
                    });
                    Settings.Instance.SearchControl_SearchTreeItem.Children.Add(new SearchTreeItem()
                    {
                        SearchItem = archiveSearchItem
                    });
                    Settings.Instance.SearchControl_SearchTreeItem.Children.Add(new SearchTreeItem()
                    {
                        SearchItem = documentSearchItem
                    });
                    Settings.Instance.SearchControl_SearchTreeItem.Children.Add(new SearchTreeItem()
                    {
                        SearchItem = ExecutableSearchItem
                    });

                    {
                        byte[] buffer = new byte[64];

                        using (var random = RandomNumberGenerator.Create())
                        {
                            random.GetBytes(buffer);
                        }

                        _amoebaManager.SetBaseNode(new Node(buffer, null));
                    }

                    _amoebaManager.ListenUris.Clear();
                    _amoebaManager.ListenUris.Add(string.Format("tcp:{0}:{1}", IPAddress.Any.ToString(), _random.Next(1024, ushort.MaxValue + 1)));
                    _amoebaManager.ListenUris.Add(string.Format("tcp:[{0}]:{1}", IPAddress.IPv6Any.ToString(), _random.Next(1024, ushort.MaxValue + 1)));

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
                        ProxyUri = "tcp:127.0.0.1:19050",
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

                    if (CultureInfo.CurrentUICulture.Name == "ja-JP")
                    {
                        Settings.Instance.Global_UseLanguage = "Japanese";
                    }
                    else
                    {
                        Settings.Instance.Global_UseLanguage = "English";
                    }

                    {
                        Box tempBox = new Box();
                        tempBox.Name = "Temp";
                        tempBox.CreationTime = DateTime.UtcNow;

                        Box box = new Box();
                        box.Name = "Box";
                        box.Boxes.Add(tempBox);
                        box.CreationTime = DateTime.UtcNow;

                        var route = new Route();
                        route.Add(box.Name);

                        Settings.Instance.LibraryControl_Box = box;
                        Settings.Instance.LibraryControl_ExpandedPath.Add(route);
                    }

                    // Links.txtにあるノード情報を追加する。
                    if (File.Exists(Path.Combine(App.DirectoryPaths["Settings"], "Links.txt")))
                    {
                        var list = new List<string>();

                        using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Settings"], "Links.txt"), new UTF8Encoding(false)))
                        {
                            string line;

                            while ((line = reader.ReadLine()) != null)
                            {
                                list.Add(line);
                            }
                        }

                        foreach (var signature in list)
                        {
                            if (Settings.Instance.LinkOptionsWindow_DownloadLinkItems.Any(n => n.Signature == signature)) continue;
                            Settings.Instance.LinkOptionsWindow_DownloadLinkItems.Add(new LinkItem() { Signature = signature });
                        }
                    }

                    // Nodes.txtにあるノード情報を追加する。
                    if (File.Exists(Path.Combine(App.DirectoryPaths["Settings"], "Nodes.txt")))
                    {
                        var list = new List<Node>();

                        using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Settings"], "Nodes.txt"), new UTF8Encoding(false)))
                        {
                            string line;

                            while ((line = reader.ReadLine()) != null)
                            {
                                list.Add(AmoebaConverter.FromNodeString(line));
                            }
                        }

                        _amoebaManager.SetOtherNodes(list);
                    }
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

                _autoBaseNodeSettingManager = new AutoBaseNodeSettingManager(_amoebaManager);
                _autoBaseNodeSettingManager.Load(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);

                _overlayNetworkManager = new OverlayNetworkManager(_amoebaManager, _bufferManager);
                _overlayNetworkManager.Load(_configrationDirectoryPaths["OverlayNetworkManager"]);

                _transferLimitManager = new TransfarLimitManager(_amoebaManager);
                _transferLimitManager.Load(_configrationDirectoryPaths["TransfarLimitManager"]);
                _transferLimitManager.Start();

                _catharsisManager = new CatharsisManager(_amoebaManager, _bufferManager);
                _catharsisManager.Load(_configrationDirectoryPaths["CatharsisManager"]);

                if (initFlag)
                {
                    _catharsisManager.Save(_configrationDirectoryPaths["CatharsisManager"]);
                    _transferLimitManager.Save(_configrationDirectoryPaths["TransfarLimitManager"]);
                    _overlayNetworkManager.Save(_configrationDirectoryPaths["OverlayNetworkManager"]);
                    _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
                    _amoebaManager.Save(_configrationDirectoryPaths["AmoebaManager"]);
                    Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);
                }

                {
                    var amoebaPath = Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba");
                    var libraryPath = Path.Combine(App.DirectoryPaths["Configuration"], "Library");

                    try
                    {
                        if (Directory.Exists(amoebaPath))
                        {
                            if (Directory.Exists(amoebaPath + ".old"))
                                Directory.Delete(amoebaPath + ".old", true);

                            MainWindow.CopyDirectory(amoebaPath, amoebaPath + ".old");
                        }

                        if (Directory.Exists(libraryPath))
                        {
                            if (Directory.Exists(libraryPath + ".old"))
                                Directory.Delete(libraryPath + ".old", true);

                            MainWindow.CopyDirectory(libraryPath, libraryPath + ".old");
                        }
                    }
                    catch (Exception e2)
                    {
                        Log.Warning(e2);
                    }
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
        private Version _updateCancelVersion;

        private void CheckUpdate(bool isLogFlag)
        {
            lock (_updateLockObject)
            {
                try
                {
                    var url = Settings.Instance.Global_Update_Url;
                    var signature = Settings.Instance.Global_Update_Signature;
                    Seed seed;

                    for (int i = 0; ; i++)
                    {
                        try
                        {
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

                            using (HttpWebResponse rs = (HttpWebResponse)rq.GetResponse())
                            using (Stream stream = rs.GetResponseStream())
                            using (StreamReader r = new StreamReader(stream))
                            {
                                seed = AmoebaConverter.FromSeedString(r.ReadLine());
                                if (seed == null) throw new ArgumentNullException();
                            }

                            break;
                        }
                        catch (Exception e)
                        {
                            if (i < 10)
                            {
                                continue;
                            }
                            else
                            {
                                Log.Error(e);

                                return;
                            }
                        }
                    }

                    Regex regex = new Regex(@"Amoeba ((\d*)\.(\d*)\.(\d*)).*\.zip");
                    var match = regex.Match(seed.Name);

                    if (match.Success)
                    {
                        var targetVersion = new Version(match.Groups[1].Value);

                        if (targetVersion <= App.AmoebaVersion)
                        {
                            if (isLogFlag)
                            {
                                Log.Information(string.Format("Check update: {0}", LanguagesManager.Instance.MainWindow_LatestVersion_Message));
                            }
                        }
                        else
                        {
                            if (!isLogFlag && targetVersion == _updateCancelVersion) return;

                            if (!string.IsNullOrWhiteSpace(signature))
                            {
                                if (!seed.VerifyCertificate()) throw new Exception("Update VerifyCertificate");
                                if (seed.Certificate.ToString() != signature) throw new Exception("Update Signature");
                            }

                            {
                                foreach (var information in _amoebaManager.DownloadingInformation)
                                {
                                    if (information.Contains("Seed") && ((DownloadState)information["State"]) != DownloadState.Completed
                                        && information.Contains("Path") && ((string)information["Path"]) == App.DirectoryPaths["Update"])
                                    {
                                        var tempSeed = (Seed)information["Seed"];

                                        if (seed == tempSeed) return;
                                    }
                                }

                                foreach (var path in Directory.GetFiles(App.DirectoryPaths["Update"]))
                                {
                                    string name = Path.GetFileName(path);

                                    if (name.StartsWith("Amoeba"))
                                    {
                                        var match2 = regex.Match(name);

                                        if (match2.Success)
                                        {
                                            var tempVersion = new Version(match2.Groups[1].Value);

                                            if (targetVersion <= tempVersion) return;
                                        }
                                    }
                                }
                            }

                            Log.Information(string.Format("Check update: {0}", seed.Name));

                            bool flag = true;

                            if (Settings.Instance.Global_Update_Option != UpdateOption.AutoUpdate)
                            {
                                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                                {
                                    if (MessageBox.Show(
                                        this,
                                        string.Format(LanguagesManager.Instance.MainWindow_CheckUpdate_Message, Path.GetFileNameWithoutExtension(seed.Name)),
                                        "Update",
                                        MessageBoxButton.OKCancel,
                                        MessageBoxImage.Information) == MessageBoxResult.Cancel)
                                    {
                                        flag = false;
                                    }
                                }));
                            }

                            if (flag)
                            {
                                _amoebaManager.Download(seed, App.DirectoryPaths["Update"], 6);

                                Log.Information(string.Format("Download: {0}", seed.Name));
                            }
                            else
                            {
                                _updateCancelVersion = targetVersion;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowPosition.Move(this);

            _windowState = this.WindowState;

            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            TopRelativeDoubleConverter.GetDoubleEvent = (object state) =>
            {
                return this.PointToScreen(new Point(0, 0)).Y;
            };

            LeftRelativeDoubleConverter.GetDoubleEvent = (object state) =>
            {
                return this.PointToScreen(new Point(0, 0)).X;
            };

            InformationControl informationControl = new InformationControl(_amoebaManager, _bufferManager);
            informationControl.Height = Double.NaN;
            informationControl.Width = Double.NaN;
            _informationTabItem.Content = informationControl;

            SearchControl searchControl = new SearchControl(_amoebaManager, _bufferManager);
            searchControl.Height = Double.NaN;
            searchControl.Width = Double.NaN;
            _searchTabItem.Content = searchControl;

            DownloadControl downloadControl = new DownloadControl(_amoebaManager, _bufferManager);
            downloadControl.Height = Double.NaN;
            downloadControl.Width = Double.NaN;
            _downloadTabItem.Content = downloadControl;

            UploadControl uploadControl = new UploadControl(_amoebaManager, _bufferManager);
            uploadControl.Height = Double.NaN;
            uploadControl.Width = Double.NaN;
            _uploadTabItem.Content = uploadControl;

            ShareControl shareControl = new ShareControl(_amoebaManager, _bufferManager);
            shareControl.Height = Double.NaN;
            shareControl.Width = Double.NaN;
            _shareTabItem.Content = shareControl;

            StoreControl storeControl = new StoreControl(_amoebaManager, _bufferManager);
            storeControl.Height = Double.NaN;
            storeControl.Width = Double.NaN;
            _storeTabItem.Content = storeControl;

            if (Settings.Instance.Global_IsStart)
            {
                _startMenuItem_Click(null, null);
            }

            if (Settings.Instance.Global_IsConvertStart)
            {
                _encodeAndDecodeStartMenuItem_Click(null, null);
            }

            if (Settings.Instance.Global_Update_Option == UpdateOption.AutoCheck
               || Settings.Instance.Global_Update_Option == UpdateOption.AutoUpdate)
            {
                _checkUpdateMenuItem_Click(null, null);
            }

            this.GarbageCollect();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_closed) return;

            if (MessageBox.Show(
                this,
                LanguagesManager.Instance.MainWindow_Close_Message,
                "Close",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information) == MessageBoxResult.No)
            {
                e.Cancel = true;

                return;
            }

            _closed = true;

            e.Cancel = true;

            var thread = new Thread(() =>
            {
                try
                {
                    Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        this.WindowState = System.Windows.WindowState.Minimized;
                    }));

                    _timerThread.Join();
                    _timerThread = null;

                    _watchThread.Join();
                    _watchThread = null;

                    _statusBarThread.Join();
                    _statusBarThread = null;

                    _trafficMonitorThread.Join();
                    _trafficMonitorThread = null;

                    _catharsisManager.Save(_configrationDirectoryPaths["CatharsisManager"]);
                    _catharsisManager.Dispose();

                    _transferLimitManager.Stop();
                    _transferLimitManager.Save(_configrationDirectoryPaths["TransfarLimitManager"]);
                    _transferLimitManager.Dispose();

                    _autoBaseNodeSettingManager.Stop();
                    _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
                    _autoBaseNodeSettingManager.Dispose();

                    _overlayNetworkManager.Stop();
                    _overlayNetworkManager.Save(_configrationDirectoryPaths["OverlayNetworkManager"]);
                    _overlayNetworkManager.Dispose();

                    _amoebaManager.EncodeStop();
                    _amoebaManager.DecodeStop();
                    _amoebaManager.Stop();
                    _amoebaManager.Save(_configrationDirectoryPaths["AmoebaManager"]);
                    _amoebaManager.Dispose();

                    NativeMethods.SetThreadExecutionState(ExecutionState.Continuous);
                    _notifyIcon.Visible = false;

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        this.Close();
                    }));
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            });
            thread.Priority = ThreadPriority.Highest;
            thread.Name = "MainWindow_CloseThread";
            thread.Start();
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

        private void _tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != _tabControl) return;

            if (_tabControl.SelectedItem == _informationTabItem)
            {
                this.SelectedTab = MainWindowTabType.Information;
            }
            else if (_tabControl.SelectedItem == _searchTabItem)
            {
                this.SelectedTab = MainWindowTabType.Search;
            }
            else if (_tabControl.SelectedItem == _downloadTabItem)
            {
                this.SelectedTab = MainWindowTabType.Download;
            }
            else if (_tabControl.SelectedItem == _uploadTabItem)
            {
                this.SelectedTab = MainWindowTabType.Upload;
            }
            else if (_tabControl.SelectedItem == _shareTabItem)
            {
                this.SelectedTab = MainWindowTabType.Share;
            }
            else if (_tabControl.SelectedItem == _storeTabItem)
            {
                this.SelectedTab = MainWindowTabType.Store;
            }
            else if (_tabControl.SelectedItem == _logTabItem)
            {
                this.SelectedTab = MainWindowTabType.Log;
            }
            else
            {
                this.SelectedTab = 0;
            }

            this.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
        }

        private void _coreMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            _updateBaseNodeMenuItem.IsEnabled = Settings.Instance.Global_IsStart && _updateBaseNodeMenuItem_IsEnabled
                && (Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled || Settings.Instance.Global_I2p_SamBridge_IsEnabled);
        }

        private void _startMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _startMenuItem.IsEnabled = false;
            _stopMenuItem.IsEnabled = true;

            Settings.Instance.Global_IsStart = true;
        }

        private void _stopMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null) _autoStop = (sender.GetType() == typeof(TransfarLimitManager));

            _startMenuItem.IsEnabled = true;
            _stopMenuItem.IsEnabled = false;

            Settings.Instance.Global_IsStart = false;
        }

        private void _linkOptionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LinkOptionsWindow window = new LinkOptionsWindow(_amoebaManager);
            window.Owner = this;
            window.ShowDialog();
        }

        volatile bool _updateBaseNodeMenuItem_IsEnabled = true;

        private void _updateBaseNodeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_updateBaseNodeMenuItem_IsEnabled) return;
            _updateBaseNodeMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem((object state) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
#if DEBUG
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
#endif

                    _autoBaseNodeSettingManager.Update();
                    _overlayNetworkManager.Restart();

#if DEBUG
                    sw.Stop();
                    Debug.WriteLine(sw.Elapsed.ToString());
#endif
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                finally
                {
                    _updateBaseNodeMenuItem_IsEnabled = true;
                }
            });
        }

        private void _cacheMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            _checkInternalBlocksMenuItem.IsEnabled = _checkInternalBlocksMenuItem_IsEnabled;
            _checkExternalBlocksMenuItem.IsEnabled = _checkExternalBlocksMenuItem_IsEnabled;
        }

        volatile bool _checkInternalBlocksMenuItem_IsEnabled = true;

        private void _checkInternalBlocksMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_checkInternalBlocksMenuItem_IsEnabled) return;
            _checkInternalBlocksMenuItem_IsEnabled = false;

            var window = new ProgressWindow(true);
            window.Owner = this;
            window.Title = string.Format(LanguagesManager.Instance.ProgressWindow_Title, LanguagesManager.Instance.MainWindow_CheckInternalBlocks_Message);
            window.Message = string.Format(LanguagesManager.Instance.MainWindow_CheckBlocks_State, 0, 0, 0);
            window.ButtonMessage = LanguagesManager.Instance.ProgressWindow_Cancel;

            ThreadPool.QueueUserWorkItem((object wstate) =>
            {
                bool flag = false;

                window.Closed += (object sender2, EventArgs e2) =>
                {
                    flag = true;
                };

                _amoebaManager.CheckInternalBlocks((object sender2, int badBlockCount, int checkedBlockCount, int blockCount, out bool isStop) =>
                {
                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        try
                        {
                            window.Value = 100 * ((double)checkedBlockCount / (double)blockCount);
                            window.Message = string.Format(LanguagesManager.Instance.MainWindow_CheckBlocks_State, badBlockCount, checkedBlockCount, blockCount);
                        }
                        catch (Exception)
                        {

                        }
                    }));

                    isStop = flag;
                });

                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                {
                    try
                    {
                        window.ButtonMessage = LanguagesManager.Instance.ProgressWindow_Ok;
                    }
                    catch (Exception)
                    {

                    }
                }));
            });

            window.Closed += (object sender2, EventArgs e2) =>
            {
                _checkInternalBlocksMenuItem_IsEnabled = true;
            };

            window.Owner = this;
            window.Show();
        }

        volatile bool _checkExternalBlocksMenuItem_IsEnabled = true;

        private void _checkExternalBlocksMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_checkExternalBlocksMenuItem_IsEnabled) return;
            _checkExternalBlocksMenuItem_IsEnabled = false;

            var window = new ProgressWindow(true);
            window.Owner = this;
            window.Title = string.Format(LanguagesManager.Instance.ProgressWindow_Title, LanguagesManager.Instance.MainWindow_CheckExternalBlocks_Message);
            window.Message = string.Format(LanguagesManager.Instance.MainWindow_CheckBlocks_State, 0, 0, 0);
            window.ButtonMessage = LanguagesManager.Instance.ProgressWindow_Cancel;

            ThreadPool.QueueUserWorkItem((object wstate) =>
            {
                bool flag = false;

                window.Closed += (object sender2, EventArgs e2) =>
                {
                    flag = true;
                };

                _amoebaManager.CheckExternalBlocks((object sender2, int badBlockCount, int checkedBlockCount, int blockCount, out bool isStop) =>
                {
                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        try
                        {
                            window.Value = 100 * ((double)checkedBlockCount / (double)blockCount);
                            window.Message = string.Format(LanguagesManager.Instance.MainWindow_CheckBlocks_State, badBlockCount, checkedBlockCount, blockCount);
                        }
                        catch (Exception)
                        {

                        }
                    }));

                    isStop = flag;
                });

                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                {
                    try
                    {
                        window.ButtonMessage = LanguagesManager.Instance.ProgressWindow_Ok;
                    }
                    catch (Exception)
                    {

                    }
                }));
            });

            window.Closed += (object sender2, EventArgs e2) =>
            {
                _checkExternalBlocksMenuItem_IsEnabled = true;
            };

            window.Owner = this;
            window.Show();
        }

        private void _encodeAndDecodeStartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _encodeAndDecodeStartMenuItem.IsEnabled = false;
            _encodeAndDecodeStopMenuItem.IsEnabled = true;

            Settings.Instance.Global_IsConvertStart = true;
        }

        private void _encodeAndDecodeStopMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _encodeAndDecodeStartMenuItem.IsEnabled = true;
            _encodeAndDecodeStopMenuItem.IsEnabled = false;

            Settings.Instance.Global_IsConvertStart = false;
        }

        private void _optionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OptionsWindow window = new OptionsWindow(
                _amoebaManager,
                _autoBaseNodeSettingManager,
                _overlayNetworkManager,
                _transferLimitManager,
                _bufferManager);

            window.Owner = this;
            window.ShowDialog();
        }

        private void _helpMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            _checkUpdateMenuItem.IsEnabled = _checkUpdateMenuItem_IsEnabled;
        }

        private void _manualSiteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://lyrise.web.fc2.com/index.html");
        }

        private void _developerSiteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Alliance-Network");
        }

        volatile bool _checkUpdateMenuItem_IsEnabled = true;

        private void _checkUpdateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_checkUpdateMenuItem_IsEnabled) return;
            _checkUpdateMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem((object state) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    this.CheckUpdate(sender != null);
                }
                catch (Exception)
                {

                }
                finally
                {
                    _checkUpdateMenuItem_IsEnabled = true;
                }
            });
        }

        private void _versionInformationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            VersionInformationWindow window = new VersionInformationWindow();
            window.Owner = this;
            window.ShowDialog();
        }

        private void _logListBoxCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var line in _logListBox.SelectedItems.Cast<string>())
            {
                sb.AppendLine(line);
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\n', '\r'));
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_logTabItem.IsSelected)
            {
                _logListBoxCopyMenuItem_Click(null, null);
            }
        }
    }
}
