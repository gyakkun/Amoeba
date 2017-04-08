using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class OptionsWindowViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }

        public DynamicViewModel Config { get; } = new DynamicViewModel();
        public ServiceOptionsViewModel Options { get; } = new ServiceOptionsViewModel();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public OptionsWindowViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();
        }

        public void Init()
        {
            {
                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", "OptionsWindow");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
                this.Config.SetPairs(_settings.Load("Config", () => new Dictionary<string, object>()));
            }

            this.GetOptions();
        }

        public void GetOptions()
        {
            // Tcp
            {
                var config = _serviceManager.TcpConnectionConfig;
                this.Options.Tcp.ProxyUri = config.ProxyUri;
                this.Options.Tcp.Ipv4IsEnabled = config.Type.HasFlag(TcpConnectionType.Ipv4);
                this.Options.Tcp.Ipv4Port = config.Ipv4Port;
                this.Options.Tcp.Ipv6IsEnabled = config.Type.HasFlag(TcpConnectionType.Ipv6);
                this.Options.Tcp.Ipv6Port = config.Ipv6Port;
            }
        }

        public void SetOpitons()
        {
            // Tcp
            {
                var tcp = this.Options.Tcp;
                var type = TcpConnectionType.None;
                if (tcp.Ipv4IsEnabled) type |= TcpConnectionType.Ipv4;
                if (tcp.Ipv6IsEnabled) type |= TcpConnectionType.Ipv6;

                _serviceManager.SetTcpConnectionConfig(
                    new TcpConnectionConfig(type, tcp.ProxyUri, tcp.Ipv4Port, tcp.Ipv6Port));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                this.SetOpitons();

                _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);
                _settings.Save("Config", this.Config.GetPairs());
                _disposable.Dispose();
            }
        }
    }
}
