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

namespace Amoeba.Interface
{
    class MainWindowViewModel : SettingsViewModelBase
    {
        private BufferManager _bufferManager;
        private ServiceManager _serviceManager;

        private Settings _settings;

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }
        public ReactiveCommand CloseCommand { get; private set; }
        private CompositeDisposable _disposable = new CompositeDisposable();

        public ObservableCollection<SettingsViewModelBase> ViewModels { get; private set; } = new ObservableCollection<SettingsViewModelBase>();

        private volatile bool _disposed;

        public MainWindowViewModel()
        {

        }

        public override void Load()
        {
            {
                _bufferManager = new BufferManager(1024 * 1024 * 1024, 1024 * 1024 * 256);

                {
                    string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "Service");
                    if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                    _serviceManager = new ServiceManager(configPath, AmoebaEnvironment.Config.Cache.BlocksPath, _bufferManager);
                    _serviceManager.Load();
                }
            }

            {
                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);

                this.CloseCommand = new ReactiveCommand().AddTo(_disposable);
                this.CloseCommand.Subscribe(_ => this.Save()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", "MainWindow");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
            }

            {
                this.ViewModels.Add(new StoreControlViewModel().AddTo(_disposable));
                this.ViewModels.Add(new InfoControlViewModel(_bufferManager, _serviceManager).AddTo(_disposable));

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

            foreach (var viewModel in this.ViewModels)
            {
                viewModel.Save();
            }
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
