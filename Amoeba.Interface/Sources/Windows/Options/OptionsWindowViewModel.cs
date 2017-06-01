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
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Threading;

namespace Amoeba.Interface
{
    class OptionsWindowViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        private Random _random = new Random();

        public event EventHandler<EventArgs> CloseEvent;

        public ServiceOptionsInfo Options { get; } = new ServiceOptionsInfo();

        public ReactiveCommand AccountSignatureNewCommand { get; private set; }
        public ReactiveCommand AccountSignatureImportCommand { get; private set; }
        public ReactiveCommand AccountSignatureExportCommand { get; private set; }

        public ObservableCollection<object> SelectedAccountTrustSignatureItems { get; } = new ObservableCollection<object>();
        public ReactiveCommand AccountTrustDeleteCommand { get; private set; }
        public ReactiveCommand AccountTrustCopyCommand { get; private set; }
        public ReactiveCommand AccountTrustPasteCommand { get; private set; }

        public ObservableCollection<object> SelectedAccountUntrustSignatureItems { get; } = new ObservableCollection<object>();
        public ReactiveCommand AccountUntrustDeleteCommand { get; private set; }
        public ReactiveCommand AccountUntrustCopyCommand { get; private set; }
        public ReactiveCommand AccountUntrustPasteCommand { get; private set; }

        public ObservableCollection<object> SelectedAccountTagItems { get; } = new ObservableCollection<object>();
        public ReactiveCommand AccountTagNewCommand { get; private set; }
        public ReactiveCommand AccountTagDeleteCommand { get; private set; }
        public ReactiveCommand AccountTagCopyCommand { get; private set; }
        public ReactiveCommand AccountTagPasteCommand { get; private set; }

        public ReactiveCommand DownloadDirectoryPathEditDialogCommand { get; private set; }

        public ReactiveCommand OkCommand { get; private set; }

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public OptionsWindowViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();
        }

        private void Init()
        {
            {
                this.AccountSignatureNewCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountSignatureNewCommand.Subscribe(() => this.AccountSignatureNew()).AddTo(_disposable);

                this.AccountSignatureImportCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountSignatureImportCommand.Subscribe(() => this.AccountSignatureImport()).AddTo(_disposable);

                this.AccountSignatureExportCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountSignatureExportCommand.Subscribe(() => this.AccountSignatureExport()).AddTo(_disposable);

                this.AccountTrustDeleteCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTrustDeleteCommand.Subscribe(() => this.AccountTrustDelete()).AddTo(_disposable);

                this.AccountTrustCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTrustCopyCommand.Subscribe(() => this.AccountTrustCopy()).AddTo(_disposable);

                this.AccountTrustPasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTrustPasteCommand.Subscribe(() => this.AccountTrustPaste()).AddTo(_disposable);

                this.AccountUntrustDeleteCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountUntrustDeleteCommand.Subscribe(() => this.AccountUntrustDelete()).AddTo(_disposable);

                this.AccountUntrustCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountUntrustCopyCommand.Subscribe(() => this.AccountUntrustCopy()).AddTo(_disposable);

                this.AccountUntrustPasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountUntrustPasteCommand.Subscribe(() => this.AccountUntrustPaste()).AddTo(_disposable);

                this.AccountTagNewCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTagNewCommand.Subscribe(() => this.AccountTagNew()).AddTo(_disposable);

                this.AccountTagDeleteCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTagDeleteCommand.Subscribe(() => this.AccountTagDelete()).AddTo(_disposable);

                this.AccountTagCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTagCopyCommand.Subscribe(() => this.AccountTagCopy()).AddTo(_disposable);

                this.AccountTagPasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTagPasteCommand.Subscribe(() => this.AccountTagPaste()).AddTo(_disposable);

                this.DownloadDirectoryPathEditDialogCommand = new ReactiveCommand().AddTo(_disposable);
                this.DownloadDirectoryPathEditDialogCommand.Subscribe(() => this.DownloadDirectoryPathEditDialog()).AddTo(_disposable);

                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);

                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(OptionsWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            this.GetOptions();
        }

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void GetOptions()
        {
            // Account
            {
                var info = SettingsManager.Instance.AccountInfo;
                this.Options.Account.DigitalSignature = info.DigitalSignature;
                this.Options.Account.Comment = info.Comment;
                this.Options.Account.TrustSignatures.AddRange(info.TrustSignatures);
                this.Options.Account.UntrustSignatures.AddRange(info.UntrustSignatures);
                this.Options.Account.Tags.AddRange(info.Tags);
            }

            // Tcp
            {
                var config = _serviceManager.TcpConnectionConfig;
                this.Options.Tcp.ProxyUri = config.ProxyUri;
                this.Options.Tcp.Ipv4IsEnabled = config.Type.HasFlag(TcpConnectionType.Ipv4);
                this.Options.Tcp.Ipv4Port = config.Ipv4Port;
                this.Options.Tcp.Ipv6IsEnabled = config.Type.HasFlag(TcpConnectionType.Ipv6);
                this.Options.Tcp.Ipv6Port = config.Ipv6Port;
            }

            // I2p
            {
                var config = _serviceManager.I2pConnectionConfig;
                this.Options.I2p.IsEnabled = config.IsEnabled;
                this.Options.I2p.SamBridgeUri = config.SamBridgeUri;
            }

            // Bandwidth
            {
                this.Options.Bandwidth.Limit = _serviceManager.BandwidthLimit;
            }

            // Data
            {
                this.Options.Data.CacheSize = _serviceManager.Size;
                this.Options.Data.DownloadDirectoryPath = _serviceManager.BasePath;
            }
        }

        private void SetOptions()
        {
            // Account
            {
                var info = SettingsManager.Instance.AccountInfo;

                if (info.DigitalSignature != this.Options.Account.DigitalSignature)
                {
                    info.Exchange = new Exchange(ExchangeAlgorithm.Rsa4096);
                }

                bool uploadFlag = false;

                if (info.DigitalSignature != this.Options.Account.DigitalSignature
                    || info.Comment != this.Options.Account.Comment
                    || !CollectionUtils.Equals(info.TrustSignatures, this.Options.Account.TrustSignatures)
                    || !CollectionUtils.Equals(info.UntrustSignatures, this.Options.Account.UntrustSignatures)
                    || !CollectionUtils.Equals(info.Tags, this.Options.Account.Tags))
                {
                    uploadFlag = true;
                }

                info.DigitalSignature = this.Options.Account.DigitalSignature;
                info.Comment = this.Options.Account.Comment;
                info.TrustSignatures.Clear();
                info.TrustSignatures.AddRange(this.Options.Account.TrustSignatures);
                info.UntrustSignatures.Clear();
                info.UntrustSignatures.AddRange(this.Options.Account.UntrustSignatures);
                info.Tags.Clear();
                info.Tags.AddRange(this.Options.Account.Tags);

                if (uploadFlag)
                {
                    ProgressDialog.Instance.Increment();

                    _serviceManager.Upload(
                        new Profile(info.Comment,
                            info.Exchange.GetExchangePublicKey(),
                            info.TrustSignatures,
                            info.UntrustSignatures,
                            info.Tags),
                        info.DigitalSignature,
                        CancellationToken.None)
                        .ContinueWith((_) => ProgressDialog.Instance.Decrement());
                }
            }

            // Tcp
            {
                var tcp = this.Options.Tcp;
                var type = TcpConnectionType.None;
                if (tcp.Ipv4IsEnabled) type |= TcpConnectionType.Ipv4;
                if (tcp.Ipv6IsEnabled) type |= TcpConnectionType.Ipv6;

                _serviceManager.SetTcpConnectionConfig(
                    new TcpConnectionConfig(type, tcp.ProxyUri, tcp.Ipv4Port, tcp.Ipv6Port));
            }

            // I2p
            {
                var i2p = this.Options.I2p;
                _serviceManager.SetI2pConnectionConfig(
                    new I2pConnectionConfig(i2p.IsEnabled, i2p.SamBridgeUri));
            }

            // Bandwidth
            {
                _serviceManager.BandwidthLimit = this.Options.Bandwidth.Limit;
            }

            // Data
            {
                if (this.Options.Data.CacheSize < _serviceManager.Size)
                {
                    var viewModel = new ConfirmWindowViewModel(LanguagesManager.Instance.OptionsWindow_CacheResize_Message);
                    viewModel.Callback += () =>
                    {
                        ProgressDialog.Instance.Increment();

                        _serviceManager.Resize(this.Options.Data.CacheSize)
                        .ContinueWith((_) => ProgressDialog.Instance.Decrement());
                    };

                    Messenger.Instance.GetEvent<ConfirmWindowShowEvent>()
                        .Publish(viewModel);
                }
                else if (this.Options.Data.CacheSize > _serviceManager.Size)
                {
                    ProgressDialog.Instance.Increment();

                    _serviceManager.Resize(this.Options.Data.CacheSize)
                    .ContinueWith((_) => ProgressDialog.Instance.Decrement());
                }

                _serviceManager.BasePath = this.Options.Data.DownloadDirectoryPath;
            }
        }

        private void AccountSignatureNew()
        {
            var viewModel = new NameEditWindowViewModel("Anonymous");
            viewModel.Callback += (name) =>
            {
                var digitalSignature = new DigitalSignature(name, DigitalSignatureAlgorithm.EcDsaP521_Sha256);
                this.Options.Account.DigitalSignature = digitalSignature;
            };

            Messenger.Instance.GetEvent<NameEditWindowShowEvent>()
                .Publish(viewModel);
        }

        private void AccountSignatureImport()
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

        private void AccountSignatureExport()
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

        private void AccountTrustDelete()
        {
            foreach (var item in this.SelectedAccountTrustSignatureItems.OfType<Signature>().ToArray())
            {
                this.Options.Account.TrustSignatures.Remove(item);
            }
        }

        private void AccountTrustCopy()
        {
            Clipboard.SetSignatures(this.SelectedAccountTrustSignatureItems.OfType<Signature>().ToArray());
        }

        private void AccountTrustPaste()
        {
            foreach (var item in Clipboard.GetSignatures())
            {
                this.Options.Account.TrustSignatures.Add(item);
            }
        }

        private void AccountUntrustDelete()
        {
            foreach (var item in this.SelectedAccountUntrustSignatureItems.OfType<Signature>().ToArray())
            {
                this.Options.Account.UntrustSignatures.Remove(item);
            }
        }

        private void AccountUntrustCopy()
        {
            Clipboard.SetSignatures(this.SelectedAccountUntrustSignatureItems.OfType<Signature>().ToArray());
        }

        private void AccountUntrustPaste()
        {
            foreach (var item in Clipboard.GetSignatures())
            {
                this.Options.Account.UntrustSignatures.Add(item);
            }
        }

        private void AccountTagNew()
        {
            var viewModel = new NameEditWindowViewModel("");
            viewModel.Callback += (name) =>
            {
                this.Options.Account.Tags.Add(new Tag(name, _random.GetBytes(32)));
            };

            Messenger.Instance.GetEvent<NameEditWindowShowEvent>()
                .Publish(viewModel);
        }

        private void AccountTagDelete()
        {
            foreach (var item in this.SelectedAccountTagItems.OfType<Tag>().ToArray())
            {
                this.Options.Account.Tags.Remove(item);
            }
        }

        private void AccountTagCopy()
        {
            Clipboard.SetTags(this.SelectedAccountTagItems.OfType<Tag>().ToArray());
        }

        private void AccountTagPaste()
        {
            foreach (var item in Clipboard.GetTags())
            {
                this.Options.Account.Tags.Add(item);
            }
        }

        private void DownloadDirectoryPathEditDialog()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
                dialog.SelectedPath = this.Options.Data.DownloadDirectoryPath;
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    this.Options.Data.DownloadDirectoryPath = dialog.SelectedPath;
                }
            }
        }

        private void Ok()
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
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
                _disposable.Dispose();
            }
        }
    }
}
