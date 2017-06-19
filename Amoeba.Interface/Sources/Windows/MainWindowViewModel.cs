using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Wpf;
using Prism.Events;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Text;
using Omnius.Collections;
using Omnius.Utilities;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading;
using System.Linq;

namespace Amoeba.Interface
{
    class MainWindowViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private TrustManager _trustManager;

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

        private WatchTimer _diskSpaceWatchTimer;

        private WatchTimer _backupTimer;

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public MainWindowViewModel()
        {
            this.Init();
        }

        private void Init()
        {
            SettingsManager.Instance.Load();

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
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", "Trust");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _trustManager = new TrustManager(configPath, _serviceManager);
                _trustManager.Load();
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
                this.ChatControlViewModel = new ChatControlViewModel(_serviceManager);
                this.StoreControlViewModel = new StoreControlViewModel(_serviceManager, _trustManager);
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            this.Setting_TrafficView();
            this.Setting_DiskSpaceWatch();
            this.Setting_BackupTimer();
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

        private void Setting_DiskSpaceWatch()
        {
            _diskSpaceWatchTimer = new WatchTimer(() =>
            {
                var paths = new List<string>();
                paths.Add(AmoebaEnvironment.Config.Cache.BlocksPath);

                bool flag = false;

                foreach (string path in paths)
                {
                    var drive = new DriveInfo(Path.GetFullPath(path));

                    if (drive.AvailableFreeSpace < NetworkConverter.FromSizeString("256MB"))
                    {
                        flag |= true;
                        break;
                    }
                }

                if (_serviceManager.Information.GetValue<long>("Cache_FreeSpace") < NetworkConverter.FromSizeString("1024MB"))
                {
                    flag |= true;
                }

                if (!flag)
                {
                    if (_serviceManager.State == ManagerState.Stop)
                    {
                        _serviceManager.Start();
                        Log.Information("Start");
                    }
                }
                else
                {
                    if (_serviceManager.State == ManagerState.Start)
                    {
                        _serviceManager.Stop();
                        Log.Information("Stop");

                        App.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var viewModel = new ConfirmWindowViewModel(LanguagesManager.Instance.MainWindow_DiskSpaceNotFound_Message);

                            Messenger.Instance.GetEvent<ConfirmWindowShowEvent>()
                                .Publish(viewModel);
                        });
                    }
                }
            });
            _diskSpaceWatchTimer.Start(new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 30));
        }

        private void Setting_BackupTimer()
        {
            var sw = Stopwatch.StartNew();

            _backupTimer = new WatchTimer(() =>
            {
                if ((!Process.GetCurrentProcess().IsActivated() && sw.Elapsed.TotalMinutes > 30)
                    || sw.Elapsed.TotalHours > 3)
                {
                    sw.Restart();

                    Backup.Instance.Run();
                    this.GarbageCollect();
                }
            });
            _backupTimer.Start(new TimeSpan(0, 0, 30));
        }

        private void GarbageCollect()
        {
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        private void Relation()
        {
            Messenger.Instance.GetEvent<RelationWindowShowEvent>()
                .Publish(new RelationWindowViewModel(_trustManager.GetRelationSignatureInfos()));
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

            _trustManager.Save();

            _serviceManager.Save();

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

                _diskSpaceWatchTimer.Stop();
                _diskSpaceWatchTimer.Dispose();

                _backupTimer.Stop();
                _backupTimer.Dispose();

                _serviceManager.Stop();

                this.Save();

                this.CloudControlViewModel.Dispose();
                this.ChatControlViewModel.Dispose();
                this.StoreControlViewModel.Dispose();

                _disposable.Dispose();

                _trustManager.Dispose();

                _serviceManager.Dispose();
            }
        }
    }
}
