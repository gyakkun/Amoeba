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

        private Settings _settings;

        public StoreSubscribeControlViewModel StoreSubscribeControlViewModel { get; private set; }
        public StorePublishControlViewModel StorePublishControlViewModel { get; private set; }
        public StoreStateControlViewModel StoreStateControlViewModel { get; private set; }
        public DynamicViewModel Config { get; } = new DynamicViewModel();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public StoreControlViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();
        }

        public void Init()
        {
            {
                this.StoreSubscribeControlViewModel = new StoreSubscribeControlViewModel(_serviceManager);
                this.StorePublishControlViewModel = new StorePublishControlViewModel(_serviceManager);
                this.StoreStateControlViewModel = new StoreStateControlViewModel(_serviceManager);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(ChatControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.Config.SetPairs(_settings.Load("Config", () => new Dictionary<string, object>()));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                this.StoreSubscribeControlViewModel.Dispose();
                this.StorePublishControlViewModel.Dispose();
                this.StoreStateControlViewModel.Dispose();

                _settings.Save("Config", this.Config.GetPairs());

                _disposable.Dispose();
            }
        }
    }
}
