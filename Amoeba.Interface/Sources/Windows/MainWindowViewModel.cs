using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Prism.Events;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class MainWindowViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        public ReactiveCommand<string> LanguageCommand { get; private set; }
        public ReactiveCommand OptionsCommand { get; private set; }

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }

        public DynamicViewModel Config { get; } = new DynamicViewModel();
        public CrowdControlViewModel CrowdControlViewModel { get; private set; }
        public ChatControlViewModel ChatControlViewModel { get; private set; }
        public StoreControlViewModel StoreControlViewModel { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public MainWindowViewModel()
        {
            this.Init();
        }

        public void Init()
        {
            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "Service");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _serviceManager = new ServiceManager(configPath, AmoebaEnvironment.Config.Cache.BlocksPath, BufferManager.Instance);
                _serviceManager.Load();
                _serviceManager.Start();
            }

            {
                this.LanguageCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.LanguageCommand.Subscribe((n) => LanguagesManager.Instance.SetCurrentLanguage(n)).AddTo(_disposable);

                this.OptionsCommand = new ReactiveCommand().AddTo(_disposable);
                this.OptionsCommand.Subscribe(() => this.Options()).AddTo(_disposable);

                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", "MainWindow");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
                this.Config.SetPairs(_settings.Load("Config", () => new Dictionary<string, object>()));
            }

            {
                this.CrowdControlViewModel = new CrowdControlViewModel(_serviceManager);
                this.ChatControlViewModel = new ChatControlViewModel(_serviceManager);
                this.StoreControlViewModel = new StoreControlViewModel();
            }
        }

        private void Options()
        {
            MainWindowMessenger.ShowEvent.GetEvent<PubSubEvent<OptionsWindowViewModel>>()
                .Publish(new OptionsWindowViewModel(_serviceManager));
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                this.CrowdControlViewModel.Dispose();
                this.ChatControlViewModel.Dispose();
                this.StoreControlViewModel.Dispose();

                _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);
                _settings.Save("Config", this.Config.GetPairs());
                _disposable.Dispose();

                _serviceManager.Stop();
                _serviceManager.Save();
                _serviceManager.Dispose();
            }
        }
    }
}
