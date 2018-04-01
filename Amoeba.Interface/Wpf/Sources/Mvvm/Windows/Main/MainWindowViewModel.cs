using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Amoeba.Messages;
using Amoeba.Rpc;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Utils;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class MainWindowViewModel : ManagerBase
    {
        private AmoebaInterfaceManager _amoebaInterfaceManager;
        private MessageManager _messageManager;
        private WatchManager _watchManager;

        private Settings _settings;

        private DialogService _dialogService;

        public ReadOnlyReactiveProperty<string> Title { get; private set; }

        public ReactiveCommand RelationCommand { get; private set; }
        public ReactiveCommand OptionsCommand { get; private set; }
        public ReactiveCommand CheckBlocksCommand { get; private set; }
        public ReactiveCommand<string> LanguageCommand { get; private set; }
        public ReactiveCommand WebsiteCommand { get; private set; }
        public ReactiveCommand VersionCommand { get; private set; }

        public ReactiveProperty<bool> IsProgressDialogOpen { get; private set; }

        public ReactiveProperty<decimal> SendingSpeed { get; private set; }
        public ReactiveProperty<decimal> ReceivingSpeed { get; private set; }

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        public CloudControlViewModel CloudControlViewModel { get; private set; }
        public ChatControlViewModel ChatControlViewModel { get; private set; }
        public StoreControlViewModel StoreControlViewModel { get; private set; }
        public UploadControlViewModel StorePublishControlViewModel { get; private set; }
        public SearchControlViewModel SearchControlViewModel { get; private set; }
        public DownloadControlViewModel DownloadControlViewModel { get; private set; }
        public UploadControlViewModel UploadControlViewModel { get; private set; }

        private TaskManager _trafficViewTaskManager;
        private TaskManager _trafficMonitorTaskManager;

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public MainWindowViewModel(DialogService dialogService)
        {
            _dialogService = dialogService;

            this.Init();
        }

        private void Init()
        {
            SettingsManager.Instance.Load();
            LanguagesManager.Instance.SetCurrentLanguage(SettingsManager.Instance.UseLanguage);

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "Service");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _amoebaInterfaceManager = new AmoebaInterfaceManager();
                {
                    var info = UriUtils.Parse(AmoebaEnvironment.Config.Communication.TargetUri);
                    var endpoint = new IPEndPoint(IPAddress.Parse(info.GetValue<string>("Address")), info.GetValue<int>("Port"));

                    _amoebaInterfaceManager.Connect(endpoint, CancellationToken.None);
                    _amoebaInterfaceManager.Load();
                }

                if (_amoebaInterfaceManager.Config.Core.Download.BasePath == null)
                {
                    lock (_amoebaInterfaceManager.LockObject)
                    {
                        var oldConfig = _amoebaInterfaceManager.Config;
                        _amoebaInterfaceManager.SetConfig(new ServiceConfig(new CoreConfig(oldConfig.Core.Network, new DownloadConfig(AmoebaEnvironment.Paths.DownloadsDirectoryPath, oldConfig.Core.Download.ProtectedPercentage)), oldConfig.Connection, oldConfig.Message));
                    }
                }
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "Control", "Message");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _messageManager = new MessageManager(configPath, _amoebaInterfaceManager);
                _messageManager.Load();
            }

            {
                this.Title = SettingsManager.Instance.AccountSetting.ObserveProperty(n => n.DigitalSignature)
                    .Select(n => $"Amoeba {AmoebaEnvironment.Version} - {n.ToString()}").ToReadOnlyReactiveProperty().AddTo(_disposable);

                this.RelationCommand = new ReactiveCommand().AddTo(_disposable);
                this.RelationCommand.Subscribe(() => this.Relation()).AddTo(_disposable);

                this.OptionsCommand = new ReactiveCommand().AddTo(_disposable);
                this.OptionsCommand.Subscribe(() => this.Options()).AddTo(_disposable);

                this.CheckBlocksCommand = new ReactiveCommand().AddTo(_disposable);
                this.CheckBlocksCommand.Subscribe(() => this.CheckBlocks()).AddTo(_disposable);

                this.LanguageCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.LanguageCommand.Subscribe((n) => LanguagesManager.Instance.SetCurrentLanguage(n)).AddTo(_disposable);

                this.WebsiteCommand = new ReactiveCommand().AddTo(_disposable);
                this.WebsiteCommand.Subscribe(() => this.Website()).AddTo(_disposable);

                this.VersionCommand = new ReactiveCommand().AddTo(_disposable);
                this.VersionCommand.Subscribe(() => this.Version()).AddTo(_disposable);

                this.IsProgressDialogOpen = new ReactiveProperty<bool>().AddTo(_disposable);

                this.ReceivingSpeed = new ReactiveProperty<decimal>().AddTo(_disposable);
                this.SendingSpeed = new ReactiveProperty<decimal>().AddTo(_disposable);

                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "View", "MainWindow");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);
                bool isInitialized = _settings.Load("IsInitialized", () => false);
                this.WindowSettings.Value = _settings.Load(nameof(this.WindowSettings), () => new WindowSettings());
                this.DynamicOptions.SetProperties(_settings.Load(nameof(this.DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));

                if (!isInitialized)
                {
                    var cloudUri = @"https://alliance-network.cloud/amoeba/locations.php";

                    if (_dialogService.ShowDialog($"Are you sure you want to connect to \"{cloudUri}\"?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            using (var httpClient = new HttpClient())
                            using (var response = httpClient.GetAsync(cloudUri).Result)
                            using (var stream = response.Content.ReadAsStreamAsync().Result)
                            {
                                var list = new List<Location>();

                                foreach (var line in JsonUtils.Load<IEnumerable<string>>(stream))
                                {
                                    try
                                    {
                                        list.Add(AmoebaConverter.FromLocationString(line));
                                    }
                                    catch (Exception)
                                    {

                                    }
                                }

                                _amoebaInterfaceManager.SetCloudLocations(list);
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }

            {
                this.CloudControlViewModel = new CloudControlViewModel(_amoebaInterfaceManager, _dialogService);
                this.ChatControlViewModel = new ChatControlViewModel(_amoebaInterfaceManager, _messageManager, _dialogService);
                this.StoreControlViewModel = new StoreControlViewModel(_amoebaInterfaceManager, _dialogService);
                this.StorePublishControlViewModel = new UploadControlViewModel(_amoebaInterfaceManager, _dialogService);
                this.SearchControlViewModel = new SearchControlViewModel(_amoebaInterfaceManager, _messageManager, _dialogService);
                this.DownloadControlViewModel = new DownloadControlViewModel(_amoebaInterfaceManager, _dialogService);
                this.UploadControlViewModel = new UploadControlViewModel(_amoebaInterfaceManager, _dialogService);
            }

            {
                _watchManager = new WatchManager(_amoebaInterfaceManager, _dialogService);
            }

            {
                EventHooks.Instance.SaveEvent += this.Save;
            }

            this.Setting_TrafficView();
        }

        private void Setting_TrafficView()
        {
            _trafficMonitorTaskManager = new TaskManager(this.TrafficMonitorThread);
            _trafficMonitorTaskManager.Start();

            _trafficViewTaskManager = new TaskManager(this.TrafficViewThread);
            _trafficViewTaskManager.Start();
        }

        private volatile TrafficInformation _sentInfomation = new TrafficInformation();
        private volatile TrafficInformation _receivedInfomation = new TrafficInformation();

        private class TrafficInformation : ISynchronized
        {
            public long PreviousTraffic { get; set; }
            public int Round { get; set; }
            public decimal[] AverageTraffics { get; } = new decimal[3];
            public object LockObject { get; } = new object();
        }

        private void TrafficViewThread(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (token.WaitHandle.WaitOne(500)) return;

                    var state = _amoebaInterfaceManager.State;

                    App.Current.Dispatcher.Invoke(DispatcherPriority.Send, new TimeSpan(0, 0, 1), new Action(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        try
                        {
                            decimal sentAverageTraffic;

                            lock (_sentInfomation.LockObject)
                            {
                                sentAverageTraffic = _sentInfomation.AverageTraffics.Sum() / _sentInfomation.AverageTraffics.Length;
                            }

                            decimal receivedAverageTraffic;

                            lock (_receivedInfomation.LockObject)
                            {
                                receivedAverageTraffic = _receivedInfomation.AverageTraffics.Sum() / _receivedInfomation.AverageTraffics.Length;
                            }

                            this.SendingSpeed.Value = sentAverageTraffic;
                            this.ReceivingSpeed.Value = receivedAverageTraffic;
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }));
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void TrafficMonitorThread(CancellationToken token)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                while (!token.IsCancellationRequested)
                {
                    Thread.Sleep(((int)Math.Max(2, 1000 - sw.ElapsedMilliseconds)) / 2);
                    if (sw.ElapsedMilliseconds < 1000) continue;

                    long receivedByteCount = _amoebaInterfaceManager.Report.Core.Network.TotalReceivedByteCount;
                    long sentByteCount = _amoebaInterfaceManager.Report.Core.Network.TotalSentByteCount;

                    lock (_sentInfomation.LockObject)
                    {
                        _sentInfomation.AverageTraffics[_sentInfomation.Round++]
                            = ((decimal)(sentByteCount - _sentInfomation.PreviousTraffic)) * 1000 / sw.ElapsedMilliseconds;
                        _sentInfomation.PreviousTraffic = sentByteCount;

                        if (_sentInfomation.Round >= _sentInfomation.AverageTraffics.Length)
                        {
                            _sentInfomation.Round = 0;
                        }
                    }

                    lock (_receivedInfomation.LockObject)
                    {
                        _receivedInfomation.AverageTraffics[_receivedInfomation.Round++]
                            = ((decimal)(receivedByteCount - _receivedInfomation.PreviousTraffic)) * 1000 / sw.ElapsedMilliseconds;
                        _receivedInfomation.PreviousTraffic = receivedByteCount;

                        if (_receivedInfomation.Round >= _receivedInfomation.AverageTraffics.Length)
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

        private void Relation()
        {
            _dialogService.ShowDialog(new RelationWindowViewModel(_messageManager));
        }

        private volatile bool _isRunning_Options = false;

        private void Options()
        {
            if (_isRunning_Options) return;
            _isRunning_Options = true;

            ProgressCircleService.Instance.Increment();

            Task.Run(() =>
            {
                var options = OptionsUtils.GetOptions(_amoebaInterfaceManager);

                ProgressCircleService.Instance.Decrement();

                App.Current.Dispatcher.Invoke(() =>
                {
                    var viewModel = new OptionsWindowViewModel(options, _dialogService);
                    viewModel.Callback += (result) =>
                    {
                        ProgressCircleService.Instance.Increment();

                        Task.Run(() =>
                        {
                            OptionsUtils.SetOptions(result, _amoebaInterfaceManager, _dialogService);

                            ProgressCircleService.Instance.Decrement();
                        });
                    };
                    viewModel.CloseEvent += (sender, e) => _isRunning_Options = false;

                    _dialogService.ShowDialog(viewModel);
                });
            });
        }

        private static class OptionsUtils
        {
            public static OptionsInfo GetOptions(AmoebaInterfaceManager serviceManager)
            {
                try
                {
                    var options = new OptionsInfo();

                    App.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        // Account
                        {
                            var info = SettingsManager.Instance.AccountSetting;
                            options.Account.DigitalSignature = info.DigitalSignature;
                            options.Account.Comment = info.Comment;
                            options.Account.TrustSignatures.AddRange(info.TrustSignatures);
                            options.Account.UntrustSignatures.AddRange(info.UntrustSignatures);
                            options.Account.Tags.AddRange(info.Tags);
                        }

                        // View
                        {
                            options.View.SubscribeSignatures.AddRange(SettingsManager.Instance.SubscribeSignatures);
                        }

                        // Update
                        {
                            var info = SettingsManager.Instance.UpdateSetting;
                            options.Update.IsEnabled = info.IsEnabled;
                            options.Update.Signature = info.Signature;
                        }
                    }));

                    {
                        var config = serviceManager.Config;
                        var cacheSize = serviceManager.Size;

                        // Connection
                        {
                            // Tcp
                            {
                                options.Connection.Tcp.ProxyUri = config.Connection.Tcp.ProxyUri;
                                options.Connection.Tcp.Ipv4IsEnabled = config.Connection.Tcp.Type.HasFlag(TcpConnectionType.Ipv4);
                                options.Connection.Tcp.Ipv4Port = config.Connection.Tcp.Ipv4Port;
                                options.Connection.Tcp.Ipv6IsEnabled = config.Connection.Tcp.Type.HasFlag(TcpConnectionType.Ipv6);
                                options.Connection.Tcp.Ipv6Port = config.Connection.Tcp.Ipv6Port;
                            }

                            // I2p
                            {
                                options.Connection.I2p.IsEnabled = config.Connection.I2p.IsEnabled;
                                options.Connection.I2p.SamBridgeUri = config.Connection.I2p.SamBridgeUri;
                            }

                            // Custom
                            {
                                options.Connection.Custom.LocationUris.AddRange(config.Connection.Custom.LocationUris);
                                options.Connection.Custom.ConnectionFilters.AddRange(config.Connection.Custom.ConnectionFilters);
                                options.Connection.Custom.ListenUris.AddRange(config.Connection.Custom.ListenUris);
                            }

                            // Bandwidth
                            {
                                options.Connection.Bandwidth.ConnectionCountLimit = config.Core.Network.ConnectionCountLimit;
                                options.Connection.Bandwidth.BandwidthLimit = config.Core.Network.BandwidthLimit;
                            }
                        }

                        // Data
                        {
                            // Cache
                            {
                                options.Data.Cache.Size = cacheSize;
                            }

                            // Download
                            {
                                options.Data.Download.DirectoryPath = config.Core.Download.BasePath;
                                options.Data.Download.ProtectedPercentage = config.Core.Download.ProtectedPercentage;
                            }
                        }
                    }

                    return options;
                }
                catch (Exception e)
                {
                    Log.Error(e);

                    throw e;
                }
            }

            public static void SetOptions(OptionsInfo options, AmoebaInterfaceManager serviceManager, DialogService dialogService)
            {
                try
                {
                    bool uploadFlag = false;

                    App.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        // AccountInfo
                        {
                            var info = SettingsManager.Instance.AccountSetting;

                            if (info.Agreement == null || info.DigitalSignature != options.Account.DigitalSignature)
                            {
                                info.Agreement = new Agreement(AgreementAlgorithm.EcDhP521_Sha256);

                                uploadFlag = true;
                            }
                            else if (info.Comment != options.Account.Comment
                                || !CollectionUtils.Equals(info.TrustSignatures, options.Account.TrustSignatures)
                                || !CollectionUtils.Equals(info.UntrustSignatures, options.Account.UntrustSignatures)
                                || !CollectionUtils.Equals(info.Tags, options.Account.Tags))
                            {
                                uploadFlag = true;
                            }

                            info.DigitalSignature = options.Account.DigitalSignature;
                            info.Comment = options.Account.Comment;
                            info.TrustSignatures.Clear();
                            info.TrustSignatures.AddRange(options.Account.TrustSignatures);
                            info.UntrustSignatures.Clear();
                            info.UntrustSignatures.AddRange(options.Account.UntrustSignatures);
                            info.Tags.Clear();
                            info.Tags.AddRange(options.Account.Tags);
                        }

                        // UpdateInfo
                        {
                            var info = SettingsManager.Instance.UpdateSetting;
                            info.IsEnabled = options.Update.IsEnabled;
                            info.Signature = options.Update.Signature;
                        }

                        // SubscribeSignatures
                        {
                            SettingsManager.Instance.SubscribeSignatures.Clear();
                            SettingsManager.Instance.SubscribeSignatures.UnionWith(options.View.SubscribeSignatures);
                        }
                    }));

                    if (uploadFlag)
                    {
                        var info = SettingsManager.Instance.AccountSetting;

                        ProgressCircleService.Instance.Increment();

                        var task = serviceManager.SetProfile(
                            new ProfileContent(info.Comment,
                                null,
                                info.TrustSignatures,
                                info.UntrustSignatures,
                                info.Tags,
                                info.Agreement.GetAgreementPublicKey()),
                            info.DigitalSignature,
                            CancellationToken.None);

                        task.ContinueWith((_) =>
                        {
                            ProgressCircleService.Instance.Decrement();
                        });
                    }

                    // AmoebaInterfaceManager
                    {
                        ServiceConfig serviceConfig;
                        {
                            ConnectionConfig connectionConfig;
                            {
                                TcpConnectionConfig tcpConnectionConfig;
                                {
                                    var type = TcpConnectionType.None;
                                    if (options.Connection.Tcp.Ipv4IsEnabled) type |= TcpConnectionType.Ipv4;
                                    if (options.Connection.Tcp.Ipv6IsEnabled) type |= TcpConnectionType.Ipv6;

                                    tcpConnectionConfig = new TcpConnectionConfig(
                                        type,
                                        options.Connection.Tcp.Ipv4Port,
                                        options.Connection.Tcp.Ipv6Port,
                                        options.Connection.Tcp.ProxyUri);
                                }

                                I2pConnectionConfig i2PConnectionConfig;
                                {
                                    i2PConnectionConfig = new I2pConnectionConfig(
                                        options.Connection.I2p.IsEnabled,
                                        options.Connection.I2p.SamBridgeUri);
                                }

                                CustomConnectionConfig customConnectionConfig;
                                {
                                    customConnectionConfig = new CustomConnectionConfig(
                                        options.Connection.Custom.LocationUris,
                                        options.Connection.Custom.ConnectionFilters,
                                        options.Connection.Custom.ListenUris);
                                }

                                CatharsisConfig catharsisConfig;
                                {
                                    var catharsisIpv4Config = new CatharsisIpv4Config(Array.Empty<string>(), Array.Empty<string>());

                                    catharsisConfig = new CatharsisConfig(catharsisIpv4Config);
                                }

                                connectionConfig = new ConnectionConfig(
                                    tcpConnectionConfig,
                                    i2PConnectionConfig,
                                    customConnectionConfig,
                                    catharsisConfig);
                            }

                            CoreConfig coreConfig;
                            {
                                NetworkConfig networkConfig;
                                {
                                    networkConfig = new NetworkConfig(
                                        options.Connection.Bandwidth.ConnectionCountLimit,
                                        options.Connection.Bandwidth.BandwidthLimit);
                                }

                                DownloadConfig downloadConfig;
                                {
                                    downloadConfig = new DownloadConfig(
                                        options.Data.Download.DirectoryPath,
                                        options.Data.Download.ProtectedPercentage);
                                }

                                coreConfig = new CoreConfig(networkConfig, downloadConfig);
                            }

                            MessageConfig messageConfig;
                            {
                                messageConfig = new MessageConfig(options.View.SubscribeSignatures);
                            }

                            serviceConfig = new ServiceConfig(coreConfig, connectionConfig, messageConfig);
                        }

                        serviceManager.SetConfig(serviceConfig);
                    }

                    // AmoebaInterfaceManager (Resize)
                    {
                        long orginalCacheSize = serviceManager.Size;

                        if (options.Data.Cache.Size < orginalCacheSize)
                        {
                            App.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                if (dialogService.ShowDialog(LanguagesManager.Instance.DataOptionsControl_CacheResize_Message,
                                    MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) == MessageBoxResult.OK)
                                {
                                    Task.Run(() =>
                                    {
                                        ProgressCircleService.Instance.Increment();

                                        serviceManager.Resize(options.Data.Cache.Size);

                                        ProgressCircleService.Instance.Decrement();
                                    });
                                }
                            }));
                        }
                        else if (options.Data.Cache.Size > orginalCacheSize)
                        {
                            ProgressCircleService.Instance.Increment();

                            serviceManager.Resize(options.Data.Cache.Size);

                            ProgressCircleService.Instance.Decrement();
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        private volatile bool _isRunning_CheckBlocks = false;

        private void CheckBlocks()
        {
            if (_isRunning_CheckBlocks) return;
            _isRunning_CheckBlocks = true;

            var viewModel = new CheckBlocksWindowViewModel(_amoebaInterfaceManager);
            viewModel.CloseEvent += (sender, e) => _isRunning_CheckBlocks = false;

            _dialogService.Show(viewModel);
        }

        private void Website()
        {
            Process.Start("https://alliance-network.cloud/");
        }

        private void Version()
        {
            _dialogService.ShowDialog(new VersionWindowViewModel());
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save("IsInitialized", true);
                _settings.Save(nameof(this.WindowSettings), this.WindowSettings.Value);
                _settings.Save(nameof(this.DynamicOptions), this.DynamicOptions.GetProperties(), true);
            });

            _messageManager.Save();

            _amoebaInterfaceManager.Save();

            SettingsManager.Instance.UseLanguage = LanguagesManager.Instance.CurrentLanguage;
            SettingsManager.Instance.Save();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                EventHooks.Instance.SaveEvent -= this.Save;

                _watchManager.Dispose();

                _trafficViewTaskManager.Stop();
                _trafficViewTaskManager.Dispose();

                _trafficMonitorTaskManager.Stop();
                _trafficMonitorTaskManager.Dispose();

                _amoebaInterfaceManager.Stop();

                this.Save();

                this.CloudControlViewModel.Dispose();
                this.ChatControlViewModel.Dispose();
                this.StoreControlViewModel.Dispose();
                this.StorePublishControlViewModel.Dispose();
                this.SearchControlViewModel.Dispose();
                this.DownloadControlViewModel.Dispose();
                this.UploadControlViewModel.Dispose();

                _disposable.Dispose();

                _messageManager.Dispose();

                _amoebaInterfaceManager.Dispose();
            }
        }
    }
}
