using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Omnius.Configuration;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class MainWindowViewModel
    {
        private Settings _settings;

        private CompositeDisposable _disposable = new CompositeDisposable();
        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }
        public ReactiveCommand ClosingCommand { get; }

        public StoreControlViewModel StoreControlViewModel { get; private set; }

        public MainWindowViewModel()
        {
            this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
            this.ClosingCommand = new ReactiveCommand().AddTo(_disposable);
            this.ClosingCommand.Subscribe((_) => this.Close());

            this.StoreControlViewModel = new StoreControlViewModel();

            this.Load();
        }

        private void Close()
        {
            this.Save();

            _disposable.Dispose();
        }

        private void Load()
        {
            var configPath = Path.Combine(EnvironmentConfig.Paths.ConfigPath, "View", "MainWindow");
            if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

            _settings = new Settings(configPath);

            this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
        }

        private void Save()
        {
            _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);
        }
    }
}
