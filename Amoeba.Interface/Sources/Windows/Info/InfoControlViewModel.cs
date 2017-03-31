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
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class InfoControlViewModel : SettingsViewModelBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        private CompositeDisposable _disposable = new CompositeDisposable();

        private volatile bool _disposed;

        public InfoControlViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public override void Load()
        {
            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(InfoControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.SetPairs(_settings.Load("DynamicSettings", () => new Dictionary<string, object>()));
            }
        }

        public override void Save()
        {
            _settings.Save("DynamicSettings", this.GetPairs());
        }

        public override void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _disposable.Dispose();
        }
    }
}
