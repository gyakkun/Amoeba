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
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Omnius.Security;

namespace Amoeba.Interface
{
    class NameEditWindowViewModel : ManagerBase
    {
        private string _name;

        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;
        public event Action<string> Callback;

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }
        public ReactiveProperty<string> Name { get; private set; }

        public ReactiveCommand OkCommand { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public NameEditWindowViewModel(string name)
        {
            _name = name;

            this.Init();

            this.Name.Value = _name;
        }

        public void Init()
        {
            {
                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
                this.Name = new ReactiveProperty<string>().AddTo(_disposable);

                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);
            }

            {
                string configPath = System.IO.Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(NameEditWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
            }
        }

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void Ok()
        {
            _name = this.Name.Value;

            this.Callback?.Invoke(_name);

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
