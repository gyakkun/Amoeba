using System;
using System.IO;
using System.Reactive.Disposables;
using Amoeba.Rpc;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class UpdateOptionsControlViewModel : ManagerBase
    {
        private DialogService _dialogService;

        private Settings _settings;

        public UpdateOptionsInfo Options { get; }

        public ReactiveProperty<string> SelectedItem { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public UpdateOptionsControlViewModel(UpdateOptionsInfo options, DialogService dialogService)
        {
            _dialogService = dialogService;

            this.Options = options;

            this.Init();
        }

        private void Init()
        {
            {
                this.SelectedItem = new ReactiveProperty<string>().AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "View", nameof(UpdateOptionsControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                EventHooks.Instance.SaveEvent += this.Save;
            }
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
