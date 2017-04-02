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

        public ServiceTcpConfig Tcp { get; } = new ServiceTcpConfig();

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
                this.Tcp.ProxyUri = config.ProxyUri;
                this.Tcp.Ipv4IsEnabled = config.Type.HasFlag(TcpConnectionType.Ipv4);
                this.Tcp.Ipv4Port = config.Ipv4Port;
                this.Tcp.Ipv6IsEnabled = config.Type.HasFlag(TcpConnectionType.Ipv6);
                this.Tcp.Ipv6Port = config.Ipv6Port;
            }
        }

        public override void Save()
        {
            // Tcp
            {
                TcpConnectionType type = TcpConnectionType.None;
                if (this.Tcp.Ipv4IsEnabled) type |= TcpConnectionType.Ipv4;
                if (this.Tcp.Ipv6IsEnabled) type |= TcpConnectionType.Ipv6;

                _serviceManager.SetTcpConnectionConfig(
                    new TcpConnectionConfig(type, this.Tcp.ProxyUri, this.Tcp.Ipv4Port, this.Tcp.Ipv6Port));
            }

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
