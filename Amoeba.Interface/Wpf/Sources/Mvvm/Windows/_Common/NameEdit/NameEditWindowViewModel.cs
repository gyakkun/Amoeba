using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class NameEditWindowViewModel : ManagerBase
    {
        private string _name;

        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;
        public event Action<string> Callback;

        public ReactiveProperty<string> Name { get; private set; }
        public ReactiveProperty<int> MaxLength { get; private set; }

        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public NameEditWindowViewModel(string name, int maxLength)
        {
            _name = name;

            this.Init();

            this.Name.Value = _name;
            this.MaxLength.Value = maxLength;
        }

        private void Init()
        {
            {
                this.Name = new ReactiveProperty<string>().AddTo(_disposable);
                this.MaxLength = new ReactiveProperty<int>().AddTo(_disposable);

                this.OkCommand = this.Name.Select(n => !string.IsNullOrWhiteSpace(n)).ToReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);

                this.CancelCommand = new ReactiveCommand().AddTo(_disposable);
                this.CancelCommand.Subscribe(() => this.Cancel()).AddTo(_disposable);
            }

            {
                string configPath = System.IO.Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "View", nameof(NameEditWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                EventHooks.Instance.SaveEvent += this.Save;
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

        private void Cancel()
        {
            this.OnCloseEvent();
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
            });
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                EventHooks.Instance.SaveEvent -= this.Save;

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
