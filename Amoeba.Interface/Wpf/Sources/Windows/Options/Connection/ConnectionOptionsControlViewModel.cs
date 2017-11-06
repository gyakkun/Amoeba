using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Data;
using Amoeba.Messages;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class ConnectionOptionsControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        private Random _random = new Random();

        public ConnectionOptionsInfo ConnectionOptions { get; } = new ConnectionOptionsInfo();

        public ReactiveProperty<string> SelectedItem { get; private set; }

        public ListCollectionView LocationUrisView => (ListCollectionView)CollectionViewSource.GetDefaultView(this.ConnectionOptions.Custom.LocationUris);
        public ReactiveProperty<string> SelectedLocationUriItem { get; private set; }
        private ListSortInfo _locationUrisSortInfo;
        public ReactiveCommand<string> LocationUrisSortCommand { get; private set; }

        public ReactiveProperty<string> LocationUriInput { get; private set; }

        public ReactiveCommand LocationUriAddCommand { get; private set; }
        public ReactiveCommand LocationUriEditCommand { get; private set; }
        public ReactiveCommand LocationUriDeleteCommand { get; private set; }

        public ListCollectionView ConnectionFiltersView => (ListCollectionView)CollectionViewSource.GetDefaultView(this.ConnectionOptions.Custom.ConnectionFilters);
        public ReactiveProperty<ConnectionFilter> SelectedConnectionFilterItem { get; private set; }

        public ReactiveProperty<string> ConnectionFilterSchemeInput { get; private set; }
        public ReactiveProperty<ConnectionType> ConnectionFilterTypeInput { get; private set; }
        public ReactiveProperty<string> ConnectionFilterProxyUriInput { get; private set; }

        public ReactiveCommand ConnectionFilterUpCommand { get; private set; }
        public ReactiveCommand ConnectionFilterDownCommand { get; private set; }
        public ReactiveCommand ConnectionFilterAddCommand { get; private set; }
        public ReactiveCommand ConnectionFilterEditCommand { get; private set; }
        public ReactiveCommand ConnectionFilterDeleteCommand { get; private set; }

        public ListCollectionView ListenUrisView => (ListCollectionView)CollectionViewSource.GetDefaultView(this.ConnectionOptions.Custom.ListenUris);
        public ReactiveProperty<string> SelectedListenUriItem { get; private set; }
        private ListSortInfo _listenUrisSortInfo;
        public ReactiveCommand<string> ListenUrisSortCommand { get; private set; }

        public ReactiveProperty<string> ListenUriInput { get; private set; }

        public ReactiveCommand ListenUriAddCommand { get; private set; }
        public ReactiveCommand ListenUriEditCommand { get; private set; }
        public ReactiveCommand ListenUriDeleteCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ConnectionOptionsControlViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();
        }

        private void Init()
        {
            {
                this.SelectedItem = new ReactiveProperty<string>().AddTo(_disposable);

                this.SelectedLocationUriItem = new ReactiveProperty<string>().AddTo(_disposable);
                this.SelectedLocationUriItem.Where(n => n != null).Subscribe(n =>
                 {
                     this.LocationUriInput.Value = n;
                 }).AddTo(_disposable);

                this.LocationUrisSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.LocationUrisSortCommand.Subscribe((propertyName) => this.LocationUrisSort(propertyName)).AddTo(_disposable);

                this.LocationUriInput = new ReactiveProperty<string>().AddTo(_disposable);

                this.LocationUriAddCommand = this.LocationUriInput.Select(n => !string.IsNullOrWhiteSpace(n)).ToReactiveCommand().AddTo(_disposable);
                this.LocationUriAddCommand.Subscribe(() => this.LocationUriAdd()).AddTo(_disposable);

                this.LocationUriEditCommand = this.SelectedLocationUriItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.LocationUriEditCommand.Subscribe(() => this.LocationUriEdit()).AddTo(_disposable);

                this.LocationUriDeleteCommand = this.SelectedLocationUriItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.LocationUriDeleteCommand.Subscribe(() => this.LocationUriDelete()).AddTo(_disposable);

                this.SelectedConnectionFilterItem = new ReactiveProperty<ConnectionFilter>();
                this.SelectedConnectionFilterItem.Where(n => n != null).Subscribe(n =>
                {
                    this.ConnectionFilterSchemeInput.Value = n.Scheme;
                    this.ConnectionFilterTypeInput.Value = n.Type;
                    this.ConnectionFilterProxyUriInput.Value = n.ProxyUri;
                }).AddTo(_disposable);

                this.ConnectionFilterSchemeInput = new ReactiveProperty<string>().AddTo(_disposable);
                this.ConnectionFilterTypeInput = new ReactiveProperty<ConnectionType>().AddTo(_disposable);
                this.ConnectionFilterProxyUriInput = new ReactiveProperty<string>().AddTo(_disposable);

                this.ConnectionFilterUpCommand = this.SelectedConnectionFilterItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.ConnectionFilterUpCommand.Subscribe(() => this.ConnectionFilterUp()).AddTo(_disposable);

                this.ConnectionFilterDownCommand = this.SelectedConnectionFilterItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.ConnectionFilterDownCommand.Subscribe(() => this.ConnectionFilterDown()).AddTo(_disposable);

                this.ConnectionFilterAddCommand = Observable.CombineLatest(this.ConnectionFilterSchemeInput, this.ConnectionFilterProxyUriInput).Select(n => n.All(m => !string.IsNullOrWhiteSpace(m))).ToReactiveCommand().AddTo(_disposable);
                this.ConnectionFilterAddCommand.Subscribe(() => this.ConnectionFilterAdd()).AddTo(_disposable);

                this.ConnectionFilterEditCommand = this.SelectedConnectionFilterItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.ConnectionFilterEditCommand.Subscribe(() => this.ConnectionFilterEdit()).AddTo(_disposable);

                this.ConnectionFilterDeleteCommand = this.SelectedConnectionFilterItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.ConnectionFilterDeleteCommand.Subscribe(() => this.ConnectionFilterDelete()).AddTo(_disposable);

                this.SelectedListenUriItem = new ReactiveProperty<string>().AddTo(_disposable);
                this.SelectedListenUriItem.Where(n => n != null).Subscribe(n =>
                {
                    this.ListenUriInput.Value = n;
                }).AddTo(_disposable);

                this.ListenUrisSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.ListenUrisSortCommand.Subscribe((propertyName) => this.ListenUrisSort(propertyName)).AddTo(_disposable);

                this.ListenUriInput = new ReactiveProperty<string>().AddTo(_disposable);

                this.ListenUriAddCommand = this.ListenUriInput.Select(n => !string.IsNullOrWhiteSpace(n)).ToReactiveCommand().AddTo(_disposable);
                this.ListenUriAddCommand.Subscribe(() => this.ListenUriAdd()).AddTo(_disposable);

                this.ListenUriEditCommand = this.SelectedListenUriItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.ListenUriEditCommand.Subscribe(() => this.ListenUriEdit()).AddTo(_disposable);

                this.ListenUriDeleteCommand = this.SelectedListenUriItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.ListenUriDeleteCommand.Subscribe(() => this.ListenUriDelete()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(ConnectionOptionsControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _locationUrisSortInfo = _settings.Load("LocationUrisSortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Uri" });
                _listenUrisSortInfo = _settings.Load("ListenUrisSortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Uri" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            this.GetOptions();

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                this.LocationUrisSort(null);
                this.ListenUrisSort(null);
            }
        }

        private void GetOptions()
        {
            // Tcp
            {
                var config = _serviceManager.Config.Connection.Tcp;
                this.ConnectionOptions.Tcp.ProxyUri = config.ProxyUri;
                this.ConnectionOptions.Tcp.Ipv4IsEnabled = config.Type.HasFlag(TcpConnectionType.Ipv4);
                this.ConnectionOptions.Tcp.Ipv4Port = config.Ipv4Port;
                this.ConnectionOptions.Tcp.Ipv6IsEnabled = config.Type.HasFlag(TcpConnectionType.Ipv6);
                this.ConnectionOptions.Tcp.Ipv6Port = config.Ipv6Port;
            }

            // I2p
            {
                var config = _serviceManager.Config.Connection.I2p;
                this.ConnectionOptions.I2p.IsEnabled = config.IsEnabled;
                this.ConnectionOptions.I2p.SamBridgeUri = config.SamBridgeUri;
            }

            // Custom
            {
                var config = _serviceManager.Config.Connection.Custom;
                this.ConnectionOptions.Custom.LocationUris.AddRange(config.LocationUris);
                this.ConnectionOptions.Custom.ConnectionFilters.AddRange(config.ConnectionFilters);
                this.ConnectionOptions.Custom.ListenUris.AddRange(config.ListenUris);
            }

            // Bandwidth
            {
                var config = _serviceManager.Config.Core.Network;
                this.ConnectionOptions.Bandwidth.ConnectionCountLimit = config.ConnectionCountLimit;
                this.ConnectionOptions.Bandwidth.BandwidthLimit = config.BandwidthLimit;
            }
        }

        public void SetOptions()
        {
            ConnectionConfig connectionConfig;
            {
                TcpConnectionConfig tcpConnectionConfig;
                {
                    var info = this.ConnectionOptions.Tcp;
                    var type = TcpConnectionType.None;
                    if (info.Ipv4IsEnabled) type |= TcpConnectionType.Ipv4;
                    if (info.Ipv6IsEnabled) type |= TcpConnectionType.Ipv6;

                    tcpConnectionConfig = new TcpConnectionConfig(type, info.Ipv4Port, info.Ipv6Port, info.ProxyUri);
                }

                I2pConnectionConfig i2PConnectionConfig;
                {
                    var info = this.ConnectionOptions.I2p;
                    i2PConnectionConfig = new I2pConnectionConfig(info.IsEnabled, info.SamBridgeUri);
                }

                CustomConnectionConfig customConnectionConfig;
                {
                    var info = this.ConnectionOptions.Custom;
                    customConnectionConfig = new CustomConnectionConfig(info.LocationUris, info.ConnectionFilters, info.ListenUris);
                }

                CatharsisConfig catharsisConfig;
                {
                    var catharsisIpv4Config = new CatharsisIpv4Config(null, null);

                    catharsisConfig = new CatharsisConfig(catharsisIpv4Config);
                }

                connectionConfig = new ConnectionConfig(tcpConnectionConfig, i2PConnectionConfig, customConnectionConfig, catharsisConfig);
            }

            NetworkConfig networkConfig;
            {
                var info = this.ConnectionOptions.Bandwidth;
                networkConfig = new NetworkConfig(info.ConnectionCountLimit, info.BandwidthLimit);
            }

            lock (_serviceManager.LockObject)
            {
                var oldConfig = _serviceManager.Config;
                _serviceManager.SetConfig(new ServiceConfig(new CoreConfig(networkConfig, oldConfig.Core.Download), connectionConfig, oldConfig.Message));
            }
        }

        private void LocationUrisSort(string propertyName)
        {
            if (propertyName == null)
            {
                if (!string.IsNullOrEmpty(_locationUrisSortInfo.PropertyName))
                {
                    this.LocationUrisSort(_locationUrisSortInfo.PropertyName, _locationUrisSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_locationUrisSortInfo.PropertyName == propertyName)
                {
                    if (_locationUrisSortInfo.Direction == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                if (!string.IsNullOrEmpty(propertyName))
                {
                    this.LocationUrisSort(propertyName, direction);
                }

                _locationUrisSortInfo.Direction = direction;
                _locationUrisSortInfo.PropertyName = propertyName;
            }
        }

        private void LocationUrisSort(string propertyName, ListSortDirection direction)
        {
            this.LocationUrisView.IsLiveSorting = true;
            this.LocationUrisView.LiveSortingProperties.Clear();
            this.LocationUrisView.SortDescriptions.Clear();

            if (propertyName == "Uri")
            {
                this.LocationUrisView.LiveSortingProperties.Add(null);
                this.LocationUrisView.SortDescriptions.Add(new SortDescription(null, direction));
            }
        }

        private void LocationUriAdd()
        {
            string selectedItem = this.LocationUriInput.Value;
            if (this.ConnectionOptions.Custom.LocationUris.Contains(selectedItem)) return;

            this.ConnectionOptions.Custom.LocationUris.Add(selectedItem);
        }

        private void LocationUriEdit()
        {
            string selectedItem = this.SelectedLocationUriItem.Value;
            if (selectedItem == null) return;

            string value = this.LocationUriInput.Value;
            if (this.ConnectionOptions.Custom.LocationUris.Contains(value)) return;

            int index = this.ConnectionOptions.Custom.LocationUris.IndexOf(selectedItem);
            this.ConnectionOptions.Custom.LocationUris[index] = value;
        }

        private void LocationUriDelete()
        {
            string selectedItem = this.SelectedLocationUriItem.Value;
            if (selectedItem == null) return;

            this.ConnectionOptions.Custom.LocationUris.Remove(selectedItem);
        }

        private void ConnectionFilterUp()
        {
            var selectedItem = this.SelectedConnectionFilterItem.Value;
            if (selectedItem == null) return;

            int index = this.ConnectionOptions.Custom.ConnectionFilters.IndexOf(selectedItem);
            if (index == 0) return;

            this.ConnectionOptions.Custom.ConnectionFilters.Move(index, index - 1);
        }

        private void ConnectionFilterDown()
        {
            var selectedItem = this.SelectedConnectionFilterItem.Value;
            if (selectedItem == null) return;

            int index = this.ConnectionOptions.Custom.ConnectionFilters.IndexOf(selectedItem);
            if (index == this.ConnectionOptions.Custom.ConnectionFilters.Count - 1) return;

            this.ConnectionOptions.Custom.ConnectionFilters.Move(index, index + 1);
        }

        private void ConnectionFilterAdd()
        {
            var selectedItem = new ConnectionFilter(this.ConnectionFilterSchemeInput.Value, this.ConnectionFilterTypeInput.Value, this.ConnectionFilterProxyUriInput.Value);
            if (this.ConnectionOptions.Custom.ConnectionFilters.Contains(selectedItem)) return;

            this.ConnectionOptions.Custom.ConnectionFilters.Add(selectedItem);
        }

        private void ConnectionFilterEdit()
        {
            var selectedItem = this.SelectedConnectionFilterItem.Value;
            if (selectedItem == null) return;

            var value = new ConnectionFilter(this.ConnectionFilterSchemeInput.Value, this.ConnectionFilterTypeInput.Value, this.ConnectionFilterProxyUriInput.Value);
            if (this.ConnectionOptions.Custom.ConnectionFilters.Contains(value)) return;

            int index = this.ConnectionOptions.Custom.ConnectionFilters.IndexOf(selectedItem);
            this.ConnectionOptions.Custom.ConnectionFilters[index] = value;
        }

        private void ConnectionFilterDelete()
        {
            var selectedItem = this.SelectedConnectionFilterItem.Value;
            if (selectedItem == null) return;

            this.ConnectionOptions.Custom.ConnectionFilters.Remove(selectedItem);
        }

        private void ListenUrisSort(string propertyName)
        {
            if (propertyName == null)
            {
                if (!string.IsNullOrEmpty(_listenUrisSortInfo.PropertyName))
                {
                    this.ListenUrisSort(_listenUrisSortInfo.PropertyName, _listenUrisSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_listenUrisSortInfo.PropertyName == propertyName)
                {
                    if (_listenUrisSortInfo.Direction == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                if (!string.IsNullOrEmpty(propertyName))
                {
                    this.ListenUrisSort(propertyName, direction);
                }

                _listenUrisSortInfo.Direction = direction;
                _listenUrisSortInfo.PropertyName = propertyName;
            }
        }

        private void ListenUrisSort(string propertyName, ListSortDirection direction)
        {
            this.ListenUrisView.IsLiveSorting = true;
            this.ListenUrisView.LiveSortingProperties.Clear();
            this.ListenUrisView.SortDescriptions.Clear();

            if (propertyName == "Uri")
            {
                this.ListenUrisView.LiveSortingProperties.Add(null);
                this.ListenUrisView.SortDescriptions.Add(new SortDescription(null, direction));
            }
        }

        private void ListenUriAdd()
        {
            string selectedItem = this.ListenUriInput.Value;
            if (this.ConnectionOptions.Custom.ListenUris.Contains(selectedItem)) return;

            this.ConnectionOptions.Custom.ListenUris.Add(selectedItem);
        }

        private void ListenUriEdit()
        {
            string selectedItem = this.SelectedListenUriItem.Value;
            if (selectedItem == null) return;

            string value = this.ListenUriInput.Value;
            if (this.ConnectionOptions.Custom.ListenUris.Contains(value)) return;

            int index = this.ConnectionOptions.Custom.ListenUris.IndexOf(selectedItem);
            this.ConnectionOptions.Custom.ListenUris[index] = value;
        }

        private void ListenUriDelete()
        {
            string selectedItem = this.SelectedListenUriItem.Value;
            if (selectedItem == null) return;

            this.ConnectionOptions.Custom.ListenUris.Remove(selectedItem);
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save("LocationUrisSortInfo", _locationUrisSortInfo);
                _settings.Save("ListenUrisSortInfo", _listenUrisSortInfo);
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

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
