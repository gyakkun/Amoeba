using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Windows.Data;
using Amoeba.Messages;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Utilities;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class CloudControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;

        private Settings _settings;

        private DialogService _dialogService;

        public ListCollectionView ConnectionInfosView => (ListCollectionView)CollectionViewSource.GetDefaultView(_connectionInfos.Values);
        private ObservableSimpleDictionary<byte[], DynamicOptions> _connectionInfos = new ObservableSimpleDictionary<byte[], DynamicOptions>(new ByteArrayEqualityComparer());
        public ObservableCollection<object> ConnectionSelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _connectionSortInfo;
        public ReactiveCommand<string> ConnectionSortCommand { get; private set; }

        public ReactiveCommand ConnectionCopyCommand { get; private set; }
        public ReactiveCommand ConnectionPasteCommand { get; private set; }

        public CloudStateInfo State { get; } = new CloudStateInfo();
        public ObservableSimpleDictionary<string, DynamicOptions> StateInfos { get; } = new ObservableSimpleDictionary<string, DynamicOptions>();
        public ObservableCollection<object> StateSelectedItems { get; } = new ObservableCollection<object>();

        public ReactiveCommand StateCopyCommand { get; private set; }

        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();
        public ObservableCollection<object> LogSelectedItems { get; } = new ObservableCollection<object>();

        public ReactiveCommand LogCopyCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public CloudControlViewModel(ServiceManager serviceManager, DialogService dialogService)
        {
            _serviceManager = serviceManager;
            _dialogService = dialogService;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();
        }

        private void Init()
        {
            {
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

                _connectionSortInfo = _settings.Load("ConnectionSortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Type" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }
            {
                this.Setting_Log();
            }
        }

        private static IEnumerable<(string, object)> GetPropertyNameAndValuePairs<T>(T value)
        {
            var tempList = new List<(string, object)>();
            {
                var type = typeof(T);

                foreach (var info in type.GetProperties())
                {
                    tempList.Add((info.Name, info.GetValue(value)));
                }
            }

            return tempList.ToArray();
        }

        private void WatchThread(CancellationToken token)
        {
            var matchSizeTypeHashSet = new HashSet<string>()
            {
                "Service.Core.Cache.FreeSpace",
                "Service.Core.Cache.LockSpace",
                "Service.Core.Cache.UsingSpace",
                "Service.Core.Network.TotalReceivedByteCount",
                "Service.Core.Network.TotalSentByteCount",
            };

            for (; ; )
            {
                {
                    var dic = new Dictionary<byte[], NetworkConnectionReport>(new ByteArrayEqualityComparer());

                    foreach (var report in _serviceManager.GetNetworkConnectionReports())
                    {
                        dic.Add(report.Id, report);
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

                        foreach (var (key, report) in dic)
                        {
                            DynamicOptions viewModel;

                            if (!_connectionInfos.TryGetValue(key, out viewModel))
                            {
                                viewModel = new DynamicOptions();
                                _connectionInfos[key] = viewModel;
                            }

                            foreach (var (name, value) in GetPropertyNameAndValuePairs(report))
                            {
                                viewModel.SetValue(name, value);
                            }
                        }

                        this.ConnectionSort();
                    });
                }

                {
                    var location = _serviceManager.Report.Core.Network.MyLocation;

                    App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (token.IsCancellationRequested) return;

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
                    var serviceReport = _serviceManager.Report;

                    var tempList = new List<(string, object)>();
                    {
                        foreach (var (name, value) in GetPropertyNameAndValuePairs(serviceReport.Connection.Tcp))
                        {
                            tempList.Add(("Service.Connection.Tcp." + name, value));
                        }

                        foreach (var (name, value) in GetPropertyNameAndValuePairs(serviceReport.Connection.Custom))
                        {
                            tempList.Add(("Service.Connection.Custom." + name, value));
                        }

                        foreach (var (name, value) in GetPropertyNameAndValuePairs(serviceReport.Core.Network))
                        {
                            tempList.Add(("Service.Core.Network." + name, value));
                        }

                        foreach (var (name, value) in GetPropertyNameAndValuePairs(serviceReport.Core.Cache))
                        {
                            tempList.Add(("Service.Core.Cache." + name, value));
                        }
                    }

                    App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        foreach (var (i, (name, value)) in tempList.Select((n, i) => (i, n)))
                        {
                            DynamicOptions viewModel;

                            if (!this.StateInfos.TryGetValue(name, out viewModel))
                            {
                                viewModel = new DynamicOptions();
                                this.StateInfos[name] = viewModel;
                            }

                            viewModel.SetValue("Index", i);
                            viewModel.SetValue("Name", name);

                            if (matchSizeTypeHashSet.Contains(name))
                            {
                                viewModel.SetValue("Value", NetworkConverter.ToSizeString((long)value));
                            }
                            else
                            {
                                viewModel.SetValue("Value", value.ToString());
                            }
                        }
                    });
                }

                if (token.WaitHandle.WaitOne(1000)) return;
            }
        }

        private void ConnectionSort(string propertyName)
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

            _connectionSortInfo.Direction = direction;
            _connectionSortInfo.PropertyName = propertyName;

            this.ConnectionSort();
        }

        private void ConnectionSort()
        {
            if (this.ConnectionInfosView.SortDescriptions.Count != 0) this.ConnectionInfosView.SortDescriptions.Clear();
            _connectionInfos.Sort((x, y) => this.ConnectionSort(x, y, _connectionSortInfo.PropertyName, _connectionSortInfo.Direction));
        }

        private int ConnectionSort(DynamicOptions x, DynamicOptions y, string propertyName, ListSortDirection direction)
        {
            int a = direction == ListSortDirection.Ascending ? 1 : -1;

            if (propertyName == "Type")
            {
                int c = a * x.GetValue<SessionType>("Type").CompareTo(y.GetValue<SessionType>("Type"));
                if (c != 0) return c;
                c = a * x.GetValue<string>("Uri").CompareTo(y.GetValue<string>("Uri"));
                return c;
            }
            else if (propertyName == "Uri")
            {
                int c = a * x.GetValue<string>("Uri").CompareTo(y.GetValue<string>("Uri"));
                return c;
            }
            else if (propertyName == "Priority")
            {
                int c = a * x.GetValue<long>("Priority").CompareTo(y.GetValue<long>("Priority"));
                if (c != 0) return c;
                c = a * x.GetValue<string>("Uri").CompareTo(y.GetValue<string>("Uri"));
                return c;
            }
            else if (propertyName == "ReceivedByteCount")
            {
                int c = a * x.GetValue<long>("ReceivedByteCount").CompareTo(y.GetValue<long>("ReceivedByteCount"));
                if (c != 0) return c;
                c = a * x.GetValue<string>("Uri").CompareTo(y.GetValue<string>("Uri"));
                return c;
            }
            else if (propertyName == "SentByteCount")
            {
                int c = a * x.GetValue<long>("SentByteCount").CompareTo(y.GetValue<long>("SentByteCount"));
                if (c != 0) return c;
                c = a * x.GetValue<string>("Uri").CompareTo(y.GetValue<string>("Uri"));
                return c;
            }

            return 0;
        }

        private void ConnectionCopy()
        {
            var list = new List<Location>();

            foreach (var value in this.ConnectionSelectedItems.OfType<DynamicOptions>())
            {
                list.Add(value.GetValue<Location>("Location"));
            }

            Clipboard.SetLocations(list);
        }

        private void ConnectionPaste()
        {
            _serviceManager.SetCloudLocations(Clipboard.GetLocations());
        }

        private void Setting_Log()
        {
            Log.MessageEvent += (sender, e) =>
            {
#if !DEBUG
                if (e.Level == LogMessageLevel.Debug) return;
#endif

                try
                {
                    App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            if (this.Logs.Count > 100)
                            {
                                this.Logs.RemoveAt(this.Logs.Count - 1);
                            }

                            this.Logs.Insert(0, string.Format("{0} {1}:\t{2}", DateTime.Now.ToString(LanguagesManager.Instance.Global_DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo), e.Level, e.Message));
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

        private void StateCopy()
        {
            var sb = new StringBuilder();

            foreach (var value in this.StateSelectedItems.OfType<DynamicOptions>())
            {
                sb.AppendLine($"{value.GetValue<string>("Name")}: {value.GetValue<string>("Value")}");
            }

            Clipboard.SetText(sb.ToString());
        }

        private void LogCopy()
        {
            var sb = new StringBuilder();

            foreach (string line in this.LogSelectedItems.OfType<string>())
            {
                sb.AppendLine(line);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save("ConnectionSortInfo", _connectionSortInfo);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
            });
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                Backup.Instance.SaveEvent -= this.Save;

                _watchTaskManager.Stop();
                _watchTaskManager.Dispose();

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
