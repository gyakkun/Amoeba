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

        public ReactiveProperty<bool> IsProgressDialogOpen { get; private set; }

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        public CloudControlViewModel CloudControlViewModel { get; private set; }
        public ChatControlViewModel ChatControlViewModel { get; private set; }
        public StoreControlViewModel StoreControlViewModel { get; private set; }

        private WatchTimer _diskSpaceWatchTimer;

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public MainWindowViewModel()
        {
            this.Init();
        }

        private void Init()
        {
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

                this.IsProgressDialogOpen = new ReactiveProperty<bool>().AddTo(_disposable);

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
                this.StoreControlViewModel = new StoreControlViewModel(_serviceManager);
            }

            {
                Backup.Instance.SaveEvent += () => this.Save();
            }

            this.Setting_StateWatch();
        }

        private void Setting_StateWatch()
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
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _diskSpaceWatchTimer.Stop();
                _diskSpaceWatchTimer.Dispose();

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
