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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Prism.Events;
using Omnius.Wpf;

namespace Amoeba.Interface
{
    class MainWindowViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }
        public ReactiveCommand<string> LanguageCommand { get; private set; }
        public ReactiveCommand OptionsCommand { get; private set; }

        public InteractionRequest<Notification> NotificationRequest { get; } = new InteractionRequest<Notification>();

        public DynamicViewModel Config { get; } = new DynamicViewModel();
        public ObservableCollection<ManagerBase> ViewModels { get; private set; } = new ObservableCollection<ManagerBase>();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public MainWindowViewModel()
        {
            this.Load();
        }

        public void Load()
        {
            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "Service");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _serviceManager = new ServiceManager(configPath, AmoebaEnvironment.Config.Cache.BlocksPath, BufferManager.Instance);
                _serviceManager.Load();
                _serviceManager.Start();
            }

            {
                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);

                this.LanguageCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.LanguageCommand.Subscribe((n) => LanguagesManager.Instance.SetCurrentLanguage(n)).AddTo(_disposable);

                this.OptionsCommand = new ReactiveCommand().AddTo(_disposable);
                this.OptionsCommand.Subscribe(() => this.Options()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", "MainWindow");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
                this.Config.SetPairs(_settings.Load("Config", () => new Dictionary<string, object>()));
            }

            {
                this.ViewModels.Add(new CrowdControlViewModel(_serviceManager));
                this.ViewModels.Add(new StoreControlViewModel());
            }
        }

        public void Save()
        {
            _serviceManager.Save();

            _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);
            _settings.Save("Config", this.Config.GetPairs());
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
                _serviceManager.Stop();

                this.Save();

                _serviceManager.Dispose();

                _disposable.Dispose();

                foreach (var viewModel in this.ViewModels)
                {
                    viewModel.Dispose();
                }
            }
        }
    }
}
