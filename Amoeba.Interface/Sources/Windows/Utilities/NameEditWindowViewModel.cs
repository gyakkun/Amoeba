using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    public class NameEditWindowOkEventArgs : EventArgs
    {
        public string Name { get; private set; }

        public NameEditWindowOkEventArgs(string name)
        {
            this.Name = name;
        }
    }

    class NameEditWindowViewModel : ManagerBase
    {
        private Settings _settings;

        public event EventHandler<NameEditWindowOkEventArgs> OkEvent;
        public event EventHandler<EventArgs> CloseEvent;

        public ReactiveCommand OkCommand { get; private set; }

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }
        public ReactiveProperty<string> Name { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public NameEditWindowViewModel()
        {
            this.Init();
        }

        public void Init()
        {
            {
                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok());

                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
                this.Name = new ReactiveProperty<string>().AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(NameEditWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
            }
        }

        private void OnOkEvent(string name)
        {
            this.OkEvent?.Invoke(this, new NameEditWindowOkEventArgs(name));
        }

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void Ok()
        {
            this.OnOkEvent(this.Name.Value);
            this.OnCloseEvent();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);

                _disposable.Dispose();
            }
        }
    }
}
