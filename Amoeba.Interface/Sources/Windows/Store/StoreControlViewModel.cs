using System;
using System.Collections.Generic;
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
using System.Collections.ObjectModel;
using Omnius.Utilities;
using Omnius.Security;
using Prism.Events;
using Prism.Interactivity.InteractionRequest;

namespace Amoeba.Interface
{
    class StoreControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TrustManager _trustManager;

        private Settings _settings;

        public StoreSearchControlViewModel StoreSearchControlViewModel { get; private set; }
        public StoreSubscribeControlViewModel StoreSubscribeControlViewModel { get; private set; }
        public StorePublishControlViewModel StorePublishControlViewModel { get; private set; }
        public StoreStateControlViewModel StoreStateControlViewModel { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public StoreControlViewModel(ServiceManager serviceManager, TrustManager trustManager)
        {
            _serviceManager = serviceManager;
            _trustManager = trustManager;

            this.Init();
        }

        private void Init()
        {
            {
                this.StoreSearchControlViewModel = new StoreSearchControlViewModel(_serviceManager, _trustManager);
                this.StoreSubscribeControlViewModel = new StoreSubscribeControlViewModel(_serviceManager);
                this.StorePublishControlViewModel = new StorePublishControlViewModel(_serviceManager);
                this.StoreStateControlViewModel = new StoreStateControlViewModel(_serviceManager);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(StoreControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += () => this.Save();
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

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                this.Save();

                this.StoreSubscribeControlViewModel.Dispose();
                this.StorePublishControlViewModel.Dispose();
                this.StoreStateControlViewModel.Dispose();

                _disposable.Dispose();
            }
        }
    }
}
