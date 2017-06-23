using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class VersionWindowViewModel : ManagerBase
    {
        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }

        public ReactiveCommand LicenseCommand { get; private set; }
        public ReactiveCommand CloseCommand { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public VersionWindowViewModel()
        {
            this.Init();
        }

        private void Init()
        {
            {
                this.CloseCommand = new ReactiveCommand().AddTo(_disposable);
                this.CloseCommand.Subscribe(() => this.Close()).AddTo(_disposable);

                this.LicenseCommand = new ReactiveCommand().AddTo(_disposable);
                this.LicenseCommand.Subscribe(() => this.License()).AddTo(_disposable);

                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
            }

            {
                string configPath = System.IO.Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(VersionWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }
        }

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void License()
        {
            try
            {
                Process.Start("https://github.com/Alliance-Network/Amoeba/blob/master/LICENSE");
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void Close()
        {
            this.OnCloseEvent();
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);
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
