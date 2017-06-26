using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Threading;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class MainWindowViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private MessageManager _messageManager;
        private ControlManager _controlManager;

        private Settings _settings;

        public ReactiveProperty<string> Title { get; private set; }

        public ReactiveCommand RelationCommand { get; private set; }
        public ReactiveCommand OptionsCommand { get; private set; }
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

        private TaskManager _trafficViewTaskManager;
        private TaskManager _trafficMonitorTaskManager;

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public MainWindowViewModel()
        {
            this.Init();
        }

        private void Init()
        {
            SettingsManager.Instance.Load();
            LanguagesManager.Instance.SetCurrentLanguage(SettingsManager.Instance.UseLanguage);

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "Service");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _serviceManager = new ServiceManager(configPath, AmoebaEnvironment.Config.Cache.BlocksPath, BufferManager.Instance);
                _serviceManager.Load();

                if (_serviceManager.BasePath == null)
                {
                    _serviceManager.BasePath = AmoebaEnvironment.Paths.DownloadsPath;
                }
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", "Message");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _messageManager = new MessageManager(configPath, _serviceManager);
                _messageManager.Load();
            }

            {
                this.Title = SettingsManager.Instance.AccountInfo.ObserveProperty(n => n.DigitalSignature)
                    .Select(n => $"Amoeba {AmoebaEnvironment.Version} - {n.ToString()}").ToReactiveProperty().AddTo(_disposable);

                this.RelationCommand = new ReactiveCommand().AddTo(_disposable);
                this.RelationCommand.Subscribe(() => this.Relation()).AddTo(_disposable);

                this.OptionsCommand = new ReactiveCommand().AddTo(_disposable);
                this.OptionsCommand.Subscribe(() => this.Options()).AddTo(_disposable);

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
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", "MainWindow");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                this.CloudControlViewModel = new CloudControlViewModel(_serviceManager);
                this.ChatControlViewModel = new ChatControlViewModel(_serviceManager, _messageManager);
                this.StoreControlViewModel = new StoreControlViewModel(_serviceManager, _messageManager);
            }

            {
                _controlManager = new ControlManager(_serviceManager);
            }

            {
                Backup.Instance.SaveEvent += this.Save;
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

                    var state = _serviceManager.State;

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

                    long receivedByteCount = _serviceManager.Information.GetValue<long>("Network_ReceivedByteCount");
                    long sentByteCount = _serviceManager.Information.GetValue<long>("Network_SentByteCount");

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
            Messenger.Instance.GetEvent<RelationWindowShowEvent>()
                .Publish(new RelationWindowViewModel(_messageManager.GetRelationSignatureInfos()));
        }

        private void Options()
        {
            Messenger.Instance.GetEvent<OptionsWindowShowEvent>()
                .Publish(new OptionsWindowViewModel(_serviceManager));
        }

        private void Website()
        {
            Process.Start("http://alliance-network.cloud/");
        }

        private void Version()
        {
            Messenger.Instance.GetEvent<VersionWindowShowEvent>()
                   .Publish(new VersionWindowViewModel());
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
            });

            _messageManager.Save();

            _serviceManager.Save();

            SettingsManager.Instance.UseLanguage = LanguagesManager.Instance.CurrentLanguage;
            SettingsManager.Instance.Save();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                Backup.Instance.SaveEvent -= this.Save;

                _trafficViewTaskManager.Stop();
                _trafficViewTaskManager.Dispose();

                _trafficMonitorTaskManager.Stop();
                _trafficMonitorTaskManager.Dispose();

                _serviceManager.Stop();

                this.Save();

                _controlManager.Dispose();

                this.CloudControlViewModel.Dispose();
                this.ChatControlViewModel.Dispose();
                this.StoreControlViewModel.Dispose();

                _disposable.Dispose();

                _messageManager.Dispose();

                _serviceManager.Dispose();
            }
        }
    }
}
