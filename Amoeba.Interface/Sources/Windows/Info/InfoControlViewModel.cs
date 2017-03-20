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
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class InfoControlViewModel : SettingsViewModelBase
    {
        private BufferManager _bufferManager;
        private ServiceManager _serviceManager;

        private Settings _settings;

        private CompositeDisposable _disposable = new CompositeDisposable();

        private volatile bool _disposed;

        public InfoControlViewModel(BufferManager bufferManager, ServiceManager serviceManager)
        {
            _bufferManager = bufferManager;
            _serviceManager = serviceManager;
        }

        public override void Load()
        {
            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(InfoControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                base.SetPairs(_settings.Load("DynamicSettings", () => new Dictionary<string, object>()));
            }
        }

        public override void Save()
        {
            _settings.Save("DynamicSettings", base.GetPairs());
        }

        public override void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _serviceManager.Dispose();
            _disposable.Dispose();
        }
    }
}
