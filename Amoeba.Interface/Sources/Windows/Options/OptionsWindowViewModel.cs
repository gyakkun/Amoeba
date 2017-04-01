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
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class OptionsWindowViewModel : SettingsViewModelBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public TcpConnectionOpitons TcpConnectionOptions { get; } = new TcpConnectionOpitons();

        public OptionsWindowViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public override void Load()
        {
            {
                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", "OptionsWindow");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
                this.SetPairs(_settings.Load("DynamicSettings", () => new Dictionary<string, object>()));
            }

            {
                var config = _serviceManager.TcpConnectionConfig;
                var options = this.TcpConnectionOptions;
                options.Ipv4IsEnabled = (config.Type == TcpConnectionType.Ipv4);
                options.Ipv4Port = config.Ipv4Port;
            }
        }

        public override void Save()
        {
            _serviceManager.Save();

            _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);
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
