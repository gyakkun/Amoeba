using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Utilities;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel;
using System.Windows.Data;

namespace Amoeba.Interface
{
    class CloudControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;

        private Settings _settings;

        public ICollectionView ConnectionInfosView => CollectionViewSource.GetDefaultView(_connectionInfos);
        public ObservableDictionary<byte[], DynamicOptions> _connectionInfos;

        public ObservableCollection<object> ConnectionSelectedItems { get; } = new ObservableCollection<object>();

        private ListSortInfo _connectionSortInfo;
        public ReactiveCommand<string> ConnectionSortCommand { get; private set; }

        public ReactiveCommand ConnectionCopyCommand { get; private set; }
        public ReactiveCommand ConnectionPasteCommand { get; private set; }

        public CloudStateInfo State { get; } = new CloudStateInfo();
        public ObservableDictionary<string, DynamicOptions> StateInfos { get; } = new ObservableDictionary<string, DynamicOptions>();
        public ObservableCollection<object> StateSelectedItems { get; } = new ObservableCollection<object>();

        public ReactiveCommand StateCopyCommand { get; private set; }

        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();
        public ObservableCollection<object> LogSelectedItems { get; } = new ObservableCollection<object>();

        public ReactiveCommand LogCopyCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public CloudControlViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();
        }

        private void Init()
        {
            {
                _connectionInfos = new ObservableDictionary<byte[], DynamicOptions>(new ByteArrayEqualityComparer());

                this.ConnectionSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.ConnectionSortCommand.Subscribe((propertyName) => this.ConnectionSort(propertyName)).AddTo(_disposable);

                this.ConnectionCopyCommand = this.ConnectionSelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.ConnectionCopyCommand.Subscribe(() => this.ConnectionCopy()).AddTo(_disposable);

                this.ConnectionPasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.ConnectionPasteCommand.Subscribe(() => this.ConnectionPaste()).AddTo(_disposable);

                this.StateCopyCommand = this.StateSelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.StateCopyCommand.Subscribe(() => this.StateCopy()).AddTo(_disposable);

                this.LogCopyCommand = this.LogSelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.LogCopyCommand.Subscribe(() => this.LogCopy()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(CloudControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _connectionSortInfo = _settings.Load("ConnectionSortInfo", () => new ListSortInfo());
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                this.ConnectionSort(null);
                this.Setting_Log();
            }
        }

        private void Setting_Log()
        {
            string logPath = null;
            bool isHeaderWrite = true;

            for (int i = 0; i < 1024; i++)
            {
                if (i == 0)
                {
                    logPath = Path.Combine(AmoebaEnvironment.Paths.LogPath, string.Format("{0}.txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                }
                else
                {
                    logPath = Path.Combine(AmoebaEnvironment.Paths.LogPath, string.Format("{0}.({1}).txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo), i));
                }

                if (!File.Exists(logPath)) break;
            }

            if (logPath == null) return;

            Log.LogEvent += (object sender, LogEventArgs e) =>
            {
                lock (logPath)
                {
                    try
                    {
                        if (e.MessageLevel == LogMessageLevel.Error || e.MessageLevel == LogMessageLevel.Warning)
                        {
                            using (var writer = new StreamWriter(logPath, true, new UTF8Encoding(false)))
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
                    catch (Exception)
                    {

                    }
                }
            };

            Log.LogEvent += (object sender, LogEventArgs e) =>
            {
                try
                {
                    App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            if (this.Logs.Count > 100)
                            {
                                this.Logs.RemoveAt(0);
                            }

                            this.Logs.Add(string.Format("{0} {1}:\t{2}", DateTime.Now.ToString(LanguagesManager.Instance.Global_DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo), e.MessageLevel, e.Message));
                        }
                        catch (Exception)
                        {

                        }
                    });
                }
                catch (Exception)
                {

                }
            };
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

                        case 2:
                            osName = "Windows 8";
                            break;

                        case 3:
                            osName = "Windows 8.1";
                            break;
                    }
                }
                else if (osInfo.Version.Major == 10)
                {
                    osName = "Windows 10";
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
                ".NET Framework:\t{3}", AmoebaEnvironment.Version.ToString(3), osName, osInfo.VersionString, Environment.Version);
        }

        private void WatchThread(CancellationToken token)
        {
            var matchSizeTypeHashSet = new HashSet<string>()
            {
                "Cache_FreeSpace",
                "Cache_LockSpace",
                "Cache_UsingSpace",
                "Network_ReceivedByteCount",
                "Network_SentByteCount",
            };

            for (;;)
            {
                {
                    var dic = new Dictionary<byte[], Information>(new ByteArrayEqualityComparer());

                    foreach (var info in _serviceManager.GetConnectionInformations())
                    {
                        dic.Add(info.GetValue<byte[]>("Id"), info);
                    }

                    App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        foreach (var key in _connectionInfos.Keys.ToArray())
                        {
                            if (!dic.ContainsKey(key))
                            {
                                _connectionInfos.Remove(key);
                            }
                        }

                        foreach (var (key, info) in dic)
                        {
                            DynamicOptions viewModel;

                            if (!_connectionInfos.TryGetValue(key, out viewModel))
                            {
                                viewModel = new DynamicOptions();
                                _connectionInfos[key] = viewModel;
                            }

                            foreach (var (name, value) in info)
                            {
                                viewModel.SetValue(name, value);
                            }
                        }
                    });
                }

                {
                    App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        var location = _serviceManager.MyLocation;

                        if (location.Uris.Count() > 0)
                        {
                            this.State.Location = AmoebaConverter.ToLocationString(location);
                        }
                        else
                        {
                            this.State.Location = null;
                        }
                    });
                }

                {
                    var information = _serviceManager.Information;

                    App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        foreach (var (i, key, value) in information.Select((item, i) => (i, item.Key, item.Value)))
                        {
                            DynamicOptions viewModel;

                            if (!this.StateInfos.TryGetValue(key, out viewModel))
                            {
                                viewModel = new DynamicOptions();
                                this.StateInfos[key] = viewModel;
                            }

                            viewModel.SetValue("Index", i);

                            if (matchSizeTypeHashSet.Contains(key))
                            {
                                viewModel.SetValue("Content", NetworkConverter.ToSizeString((long)value));
                            }
                            else
                            {
                                viewModel.SetValue("Content", value.ToString());
                            }
                        }
                    });
                }

                if (token.WaitHandle.WaitOne(1000)) return;
            }
        }

        private void ConnectionSort(string propertyName)
        {
            if (propertyName == null)
            {
                this.ConnectionInfosView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(_connectionSortInfo.PropertyName))
                {
                    this.ConnectionSort(_connectionSortInfo.PropertyName, _connectionSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_connectionSortInfo.PropertyName == propertyName)
                {
                    if (_connectionSortInfo.Direction == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                this.ConnectionInfosView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(propertyName))
                {
                    this.ConnectionSort(propertyName, direction);
                }

                _connectionSortInfo.Direction = direction;
                _connectionSortInfo.PropertyName = propertyName;
            }
        }

        private void ConnectionSort(string propertyName, ListSortDirection direction)
        {
            switch (propertyName)
            {
                case "Type":
                    this.ConnectionInfosView.SortDescriptions.Add(new SortDescription("Value.Type", direction));
                    break;
                case "Id":
                    this.ConnectionInfosView.SortDescriptions.Add(new SortDescription("Value.Id", direction));
                    break;
                case "Priority":
                    this.ConnectionInfosView.SortDescriptions.Add(new SortDescription("Value.Priority", direction));
                    break;
                case "ReceivedByteCount":
                    this.ConnectionInfosView.SortDescriptions.Add(new SortDescription("Value.ReceivedByteCount", direction));
                    break;
                case "SentByteCount":
                    this.ConnectionInfosView.SortDescriptions.Add(new SortDescription("Value.SentByteCount", direction));
                    break;
            }
        }

        private void ConnectionCopy()
        {
            var list = new List<Location>();

            foreach (var (key, value) in this.ConnectionSelectedItems.Cast<KeyValuePair<byte[], DynamicOptions>>())
            {
                list.Add(value.GetValue<Location>("Location"));
            }

            Clipboard.SetLocations(list);
        }

        private void ConnectionPaste()
        {
            _serviceManager.SetCloudLocations(Clipboard.GetLocations());
        }

        private void StateCopy()
        {
            var sb = new StringBuilder();

            foreach (var (key, value) in this.StateSelectedItems.Cast<KeyValuePair<string, DynamicOptions>>())
            {
                sb.AppendLine($"{key}: {value.GetValue<string>("Content")}");
            }

            Clipboard.SetText(sb.ToString());
        }

        private void LogCopy()
        {
            var sb = new StringBuilder();

            foreach (string line in this.LogSelectedItems.Cast<string>())
            {
                sb.AppendLine(line);
            }

            Clipboard.SetText(sb.ToString());
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _watchTaskManager.Stop();
                _watchTaskManager.Dispose();

                _settings.Save("Version", 0);
                _settings.Save("ConnectionSortInfo", _connectionSortInfo);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
                _disposable.Dispose();
            }
        }
    }
}
