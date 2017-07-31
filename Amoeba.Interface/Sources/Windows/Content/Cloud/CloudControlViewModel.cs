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
using Omnius.Net.Amoeba;
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
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                this.ConnectionSort(null);
            }

            {
                this.Setting_Log();
            }
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
                    var location = _serviceManager.MyLocation;

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
                            viewModel.SetValue("Name", key);

                            if (matchSizeTypeHashSet.Contains(key))
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
            this.ConnectionInfosView.IsLiveSorting = true;
            this.ConnectionInfosView.LiveSortingProperties.Add(propertyName);
            this.ConnectionInfosView.SortDescriptions.Add(new SortDescription(propertyName, direction));
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

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
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
