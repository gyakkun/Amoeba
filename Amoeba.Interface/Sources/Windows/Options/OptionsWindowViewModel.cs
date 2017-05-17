using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Wpf;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class OptionsWindowViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;

        public ServiceOptionsInfo Options { get; } = new ServiceOptionsInfo();

        public ReactiveCommand SignatureNewCommand { get; private set; }
        public ReactiveCommand SignatureImportCommand { get; private set; }
        public ReactiveCommand SignatureExportCommand { get; private set; }

        public ReactiveCommand OkCommand { get; private set; }

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }

        public DynamicViewModel Config { get; } = new DynamicViewModel();

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
                this.SignatureNewCommand = new ReactiveCommand().AddTo(_disposable);
                this.SignatureNewCommand.Subscribe(() => this.SignatureNew()).AddTo(_disposable);

                this.SignatureImportCommand = new ReactiveCommand().AddTo(_disposable);
                this.SignatureImportCommand.Subscribe(() => this.SignatureImport()).AddTo(_disposable);

                this.SignatureExportCommand = new ReactiveCommand().AddTo(_disposable);
                this.SignatureExportCommand.Subscribe(() => this.SignatureExport()).AddTo(_disposable);

                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);

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

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
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

            // Account
            {
                this.Options.Account.DigitalSignature = SettingsManager.Instance.DigitalSignature;
            }
        }

        public void SetOptions()
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

            // Account
            {
                SettingsManager.Instance.DigitalSignature = this.Options.Account.DigitalSignature;
            }
        }

        public void SignatureNew()
        {
            var viewModel = new NameEditWindowViewModel("Anonymous");
            viewModel.Callback += (name) =>
            {
                var digitalSignature = new DigitalSignature(name, DigitalSignatureAlgorithm.EcDsaP521_Sha256);
                this.Options.Account.DigitalSignature = digitalSignature;
            };

            Messenger.Instance.GetEvent<NameEditWindowViewModelShowEvent>()
                .Publish(viewModel);
        }

        public void SignatureImport()
        {
            using (var dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Multiselect = false;
                dialog.RestoreDirectory = true;
                dialog.DefaultExt = ".ds";
                dialog.Filter = "DigitalSignature (*.ds)|*.ds";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string filePath = dialog.FileNames.FirstOrDefault();
                    if (filePath == null) return;

                    using (var stream = new FileStream(filePath, FileMode.Open))
                    {
                        var digitalSignature = DigitalSignatureConverter.FromDigitalSignatureStream(stream);
                        if (digitalSignature == null) return;

                        this.Options.Account.DigitalSignature = digitalSignature;
                    }
                }
            }
        }

        public void SignatureExport()
        {
            using (var dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.RestoreDirectory = true;
                dialog.FileName = this.Options.Account.DigitalSignature.GetSignature().ToString();
                dialog.DefaultExt = ".ds";
                dialog.Filter = "DigitalSignature (*.ds)|*.ds";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string fileName = dialog.FileName;

                    using (var fileStream = new FileStream(fileName, FileMode.Create))
                    using (var digitalSignatureStream = DigitalSignatureConverter.ToDigitalSignatureStream(this.Options.Account.DigitalSignature))
                    using (var safeBuffer = BufferManager.Instance.CreateSafeBuffer(1024 * 4))
                    {
                        int length;

                        while ((length = digitalSignatureStream.Read(safeBuffer.Value, 0, safeBuffer.Value.Length)) > 0)
                        {
                            fileStream.Write(safeBuffer.Value, 0, length);
                        }
                    }
                }
            }
        }

        public void Ok()
        {
            this.SetOptions();

            this.OnCloseEvent();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);
                _settings.Save("Config", this.Config.GetPairs());
                _disposable.Dispose();
            }
        }
    }
}
