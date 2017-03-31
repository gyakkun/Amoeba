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
    class MainWindowViewModel : SettingsViewModelBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }
        public ReactiveCommand OptionsCommand { get; private set; }
        private CompositeDisposable _disposable = new CompositeDisposable();

        public InteractionRequest<Notification> NotificationRequest { get; } = new InteractionRequest<Notification>();

        public ObservableCollection<SettingsViewModelBase> ViewModels { get; private set; } = new ObservableCollection<SettingsViewModelBase>();

        private volatile bool _disposed;

        public MainWindowViewModel()
        {

        }

        public override void Load()
        {
            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "Service");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _serviceManager = new ServiceManager(configPath, AmoebaEnvironment.Config.Cache.BlocksPath, BufferManager.Instance);
                _serviceManager.Load();
            }

            {
                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
                this.OptionsCommand = new ReactiveCommand().AddTo(_disposable);
                this.OptionsCommand.Subscribe(() => this.Options()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", "MainWindow");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
                this.SetPairs(_settings.Load("DynamicSettings", () => new Dictionary<string, object>()));
            }

            {
                this.ViewModels.Add(new InfoControlViewModel(_serviceManager));
                this.ViewModels.Add(new StoreControlViewModel());

                foreach (var viewModel in this.ViewModels)
                {
                    viewModel.Load();
                }
            }
        }

        public override void Save()
        {
            _serviceManager.Save();

            _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);
            _settings.Save("DynamicSettings", this.GetPairs(), true);

            foreach (var viewModel in this.ViewModels)
            {
                viewModel.Save();
            }
        }

        private void Options()
        {
            MainWindowMessenger.ShowEvent.GetEvent<PubSubEvent<OptionsWindowViewModel>>()
                .Publish(new OptionsWindowViewModel(_serviceManager));
        }

        public override void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _serviceManager.Dispose();

            _disposable.Dispose();

            foreach (var viewModel in this.ViewModels)
            {
                viewModel.Dispose();
            }
        }
    }
}
