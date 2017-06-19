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
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Data;
using System.Collections;

namespace Amoeba.Interface
{
    class OptionsWindowViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        private Random _random = new Random();

        public event EventHandler<EventArgs> CloseEvent;

        public OptionsInfo Options { get; } = new OptionsInfo();

        public ReactiveCommand AccountSignatureNewCommand { get; private set; }
        public ReactiveCommand AccountSignatureImportCommand { get; private set; }
        public ReactiveCommand AccountSignatureExportCommand { get; private set; }

        public ICollectionView AccountTrustSignaturesView => CollectionViewSource.GetDefaultView(this.Options.Account.TrustSignatures);
        public ObservableCollection<object> SelectedAccountTrustSignatureItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _accountTrustSignaturesSortInfo;
        public ReactiveCommand<string> AccountTrustSignaturesSortCommand { get; private set; }

        public ReactiveCommand AccountTrustDeleteCommand { get; private set; }
        public ReactiveCommand AccountTrustCopyCommand { get; private set; }
        public ReactiveCommand AccountTrustPasteCommand { get; private set; }

        public ICollectionView AccountUntrustSignaturesView => CollectionViewSource.GetDefaultView(this.Options.Account.UntrustSignatures);
        public ObservableCollection<object> SelectedAccountUntrustSignatureItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _accountUntrustSignaturesSortInfo;
        public ReactiveCommand<string> AccountUntrustSignaturesSortCommand { get; private set; }

        public ReactiveCommand AccountUntrustDeleteCommand { get; private set; }
        public ReactiveCommand AccountUntrustCopyCommand { get; private set; }
        public ReactiveCommand AccountUntrustPasteCommand { get; private set; }

        public ICollectionView AccountTagsView => CollectionViewSource.GetDefaultView(this.Options.Account.Tags);
        public ObservableCollection<object> SelectedAccountTagItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _accountTagsSortInfo;
        public ReactiveCommand<string> AccountTagsSortCommand { get; private set; }

        public ReactiveCommand AccountTagNewCommand { get; private set; }
        public ReactiveCommand AccountTagDeleteCommand { get; private set; }
        public ReactiveCommand AccountTagCopyCommand { get; private set; }
        public ReactiveCommand AccountTagPasteCommand { get; private set; }

        public ICollectionView SubscribeSignaturesView => CollectionViewSource.GetDefaultView(this.Options.Subscribe.SubscribeSignatures);
        public ObservableCollection<object> SelectedSubscribeSignatureItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _subscribeSignaturesSortInfo;
        public ReactiveCommand<string> SubscribeSignaturesSortCommand { get; private set; }

        public ReactiveCommand SubscribeDeleteCommand { get; private set; }
        public ReactiveCommand SubscribeCopyCommand { get; private set; }
        public ReactiveCommand SubscribePasteCommand { get; private set; }

        public ReactiveCommand DownloadDirectoryPathEditDialogCommand { get; private set; }

        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }

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

                this.AccountTrustSignaturesSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.AccountTrustSignaturesSortCommand.Subscribe((propertyName) => this.AccountTrustSignaturesSort(propertyName)).AddTo(_disposable);

                this.AccountTrustDeleteCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTrustDeleteCommand.Subscribe(() => this.AccountTrustDelete()).AddTo(_disposable);

                this.AccountTrustCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTrustCopyCommand.Subscribe(() => this.AccountTrustCopy()).AddTo(_disposable);

                this.AccountTrustPasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTrustPasteCommand.Subscribe(() => this.AccountTrustPaste()).AddTo(_disposable);

                this.AccountUntrustSignaturesSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.AccountUntrustSignaturesSortCommand.Subscribe((propertyName) => this.AccountUntrustSignaturesSort(propertyName)).AddTo(_disposable);

                this.AccountUntrustDeleteCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountUntrustDeleteCommand.Subscribe(() => this.AccountUntrustDelete()).AddTo(_disposable);

                this.AccountUntrustCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountUntrustCopyCommand.Subscribe(() => this.AccountUntrustCopy()).AddTo(_disposable);

                this.AccountUntrustPasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountUntrustPasteCommand.Subscribe(() => this.AccountUntrustPaste()).AddTo(_disposable);

                this.AccountTagsSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.AccountTagsSortCommand.Subscribe((propertyName) => this.AccountTagsSort(propertyName)).AddTo(_disposable);

                this.AccountTagNewCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTagNewCommand.Subscribe(() => this.AccountTagNew()).AddTo(_disposable);

                this.AccountTagDeleteCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTagDeleteCommand.Subscribe(() => this.AccountTagDelete()).AddTo(_disposable);

                this.AccountTagCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTagCopyCommand.Subscribe(() => this.AccountTagCopy()).AddTo(_disposable);

                this.AccountTagPasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.AccountTagPasteCommand.Subscribe(() => this.AccountTagPaste()).AddTo(_disposable);

                this.SubscribeSignaturesSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SubscribeSignaturesSortCommand.Subscribe((propertyName) => this.SubscribeSignaturesSort(propertyName)).AddTo(_disposable);

                this.SubscribeDeleteCommand = new ReactiveCommand().AddTo(_disposable);
                this.SubscribeDeleteCommand.Subscribe(() => this.SubscribeDelete()).AddTo(_disposable);

                this.SubscribeCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.SubscribeCopyCommand.Subscribe(() => this.SubscribeCopy()).AddTo(_disposable);

                this.SubscribePasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.SubscribePasteCommand.Subscribe(() => this.SubscribePaste()).AddTo(_disposable);

                this.DownloadDirectoryPathEditDialogCommand = new ReactiveCommand().AddTo(_disposable);
                this.DownloadDirectoryPathEditDialogCommand.Subscribe(() => this.DownloadDirectoryPathEditDialog()).AddTo(_disposable);

                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);

                this.CancelCommand = new ReactiveCommand().AddTo(_disposable);
                this.CancelCommand.Subscribe(() => this.Cancel()).AddTo(_disposable);

                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(OptionsWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _accountTrustSignaturesSortInfo = _settings.Load("AccountTrustSignaturesSortInfo ", () => new ListSortInfo());
                _accountUntrustSignaturesSortInfo = _settings.Load("AccountUntrustSignaturesSortInfo ", () => new ListSortInfo());
                _accountTagsSortInfo = _settings.Load("AccountTagsSortInfo", () => new ListSortInfo());
                _subscribeSignaturesSortInfo = _settings.Load("SubscribeSignaturesSortInfo ", () => new ListSortInfo());
                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            this.GetOptions();

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                this.AccountTrustSignaturesSort(null);
                this.AccountUntrustSignaturesSort(null);
                this.AccountTagsSort(null);
                this.SubscribeSignaturesSort(null);
            }
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

            // Subscribe
            {
                this.Options.Subscribe.SubscribeSignatures.AddRange(SettingsManager.Instance.SubscribeSignatures);
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
                this.Options.Bandwidth.BandwidthLimit = _serviceManager.BandwidthLimit;
                this.Options.Bandwidth.ConnectionCountLimit = _serviceManager.ConnectionCountLimit;
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

            // Subscribe
            {
                lock (SettingsManager.Instance.SubscribeSignatures.LockObject)
                {
                    SettingsManager.Instance.SubscribeSignatures.Clear();
                    SettingsManager.Instance.SubscribeSignatures.UnionWith(this.Options.Subscribe.SubscribeSignatures);
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
                _serviceManager.BandwidthLimit = this.Options.Bandwidth.BandwidthLimit;
                _serviceManager.ConnectionCountLimit = this.Options.Bandwidth.ConnectionCountLimit;
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

        private void AccountTrustSignaturesSort(string propertyName)
        {
            if (propertyName == null)
            {
                this.AccountTrustSignaturesView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(_accountTagsSortInfo.PropertyName))
                {
                    this.AccountTrustSignaturesSort(_accountTagsSortInfo.PropertyName, _accountTagsSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_accountTagsSortInfo.PropertyName == propertyName)
                {
                    if (_accountTagsSortInfo.Direction == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                this.AccountTrustSignaturesView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(propertyName))
                {
                    this.AccountTrustSignaturesSort(propertyName, direction);
                }

                _accountTagsSortInfo.Direction = direction;
                _accountTagsSortInfo.PropertyName = propertyName;
            }
        }

        private void AccountTrustSignaturesSort(string propertyName, ListSortDirection direction)
        {
            switch (propertyName)
            {
                case "Signature":
                    this.AccountTrustSignaturesView.SortDescriptions.Add(new SortDescription("Name", direction));
                    break;
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

        private void AccountUntrustSignaturesSort(string propertyName)
        {
            if (propertyName == null)
            {
                this.AccountUntrustSignaturesView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(_accountUntrustSignaturesSortInfo.PropertyName))
                {
                    this.AccountUntrustSignaturesSort(_accountUntrustSignaturesSortInfo.PropertyName, _accountUntrustSignaturesSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_accountUntrustSignaturesSortInfo.PropertyName == propertyName)
                {
                    if (_accountUntrustSignaturesSortInfo.Direction == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                this.AccountUntrustSignaturesView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(propertyName))
                {
                    this.AccountUntrustSignaturesSort(propertyName, direction);
                }

                _accountUntrustSignaturesSortInfo.Direction = direction;
                _accountUntrustSignaturesSortInfo.PropertyName = propertyName;
            }
        }

        private void AccountUntrustSignaturesSort(string propertyName, ListSortDirection direction)
        {
            switch (propertyName)
            {
                case "Signature":
                    this.AccountUntrustSignaturesView.SortDescriptions.Add(new SortDescription("Name", direction));
                    break;
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

        private void AccountTagsSort(string propertyName)
        {
            if (propertyName == null)
            {
                this.AccountTagsView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(_accountTagsSortInfo.PropertyName))
                {
                    this.AccountTagsSort(_accountTagsSortInfo.PropertyName, _accountTagsSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_accountTagsSortInfo.PropertyName == propertyName)
                {
                    if (_accountTagsSortInfo.Direction == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                this.AccountTagsView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(propertyName))
                {
                    this.AccountTagsSort(propertyName, direction);
                }

                _accountTagsSortInfo.Direction = direction;
                _accountTagsSortInfo.PropertyName = propertyName;
            }
        }

        private void AccountTagsSort(string propertyName, ListSortDirection direction)
        {
            switch (propertyName)
            {
                case "Name":
                    this.AccountTagsView.SortDescriptions.Add(new SortDescription("Name", direction));
                    break;
                case "Id":
                    {
                        var view = ((ListCollectionView)this.AccountTagsView);
                        view.CustomSort = new CustomSortComparer(direction, (x, y) => Unsafe.Compare(((Tag)x).Id, ((Tag)y).Id));
                        view.Refresh();
                    }
                    break;
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

        private void SubscribeSignaturesSort(string propertyName)
        {
            if (propertyName == null)
            {
                this.SubscribeSignaturesView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(_accountTagsSortInfo.PropertyName))
                {
                    this.SubscribeSignaturesSort(_accountTagsSortInfo.PropertyName, _accountTagsSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_accountTagsSortInfo.PropertyName == propertyName)
                {
                    if (_accountTagsSortInfo.Direction == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                this.SubscribeSignaturesView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(propertyName))
                {
                    this.SubscribeSignaturesSort(propertyName, direction);
                }

                _accountTagsSortInfo.Direction = direction;
                _accountTagsSortInfo.PropertyName = propertyName;
            }
        }

        private void SubscribeSignaturesSort(string propertyName, ListSortDirection direction)
        {
            switch (propertyName)
            {
                case "Signature":
                    this.SubscribeSignaturesView.SortDescriptions.Add(new SortDescription("Name", direction));
                    break;
            }
        }

        private void SubscribeDelete()
        {
            foreach (var item in this.SelectedSubscribeSignatureItems.OfType<Signature>().ToArray())
            {
                this.Options.Subscribe.SubscribeSignatures.Remove(item);
            }
        }

        private void SubscribeCopy()
        {
            Clipboard.SetSignatures(this.SelectedSubscribeSignatureItems.OfType<Signature>().ToArray());
        }

        private void SubscribePaste()
        {
            foreach (var item in Clipboard.GetSignatures())
            {
                this.Options.Subscribe.SubscribeSignatures.Add(item);
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

        private void Cancel()
        {
            this.OnCloseEvent();
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save("AccountTrustSignaturesSortInfo", _accountTrustSignaturesSortInfo);
                _settings.Save("AccountUntrustSignaturesSortInfo", _accountUntrustSignaturesSortInfo);
                _settings.Save("AccountTagsSortInfo", _accountTagsSortInfo);
                _settings.Save("SubscribeSignaturesSortInfo", _accountTrustSignaturesSortInfo);
                _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                Backup.Instance.SaveEvent -= this.Save;

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
