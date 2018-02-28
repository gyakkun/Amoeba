using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using Omnius.Base;
using Omnius.Configuration;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class VersionWindowViewModel : ManagerBase
    {
        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;

        public ReactiveCommand LicenseCommand { get; private set; }
        public ReactiveCommand CloseCommand { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

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
            }

            {
                string configPath = System.IO.Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "View", nameof(VersionWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);
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
            });
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                Backup.Instance.SaveEvent -= this.Save;

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
