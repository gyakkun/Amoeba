using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Data;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Net.Amoeba;
using Omnius.Security;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class AccountOptionsControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        private Random _random = new Random();

        public AccountOptionsInfo AccountOptions { get; } = new AccountOptionsInfo();

        public ReactiveProperty<string> SelectedItem { get; private set; }

        public ReactiveCommand SignatureNewCommand { get; private set; }
        public ReactiveCommand SignatureImportCommand { get; private set; }
        public ReactiveCommand SignatureExportCommand { get; private set; }

        public ListCollectionView TrustSignaturesView => (ListCollectionView)CollectionViewSource.GetDefaultView(this.AccountOptions.TrustSignatures);
        public ObservableCollection<object> SelectedTrustSignatureItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _trustSignaturesSortInfo;
        public ReactiveCommand<string> TrustSignaturesSortCommand { get; private set; }

        public ReactiveCommand TrustSignatureDeleteCommand { get; private set; }
        public ReactiveCommand TrustSignatureCopyCommand { get; private set; }
        public ReactiveCommand TrustSignaturePasteCommand { get; private set; }

        public ListCollectionView UntrustSignaturesView => (ListCollectionView)CollectionViewSource.GetDefaultView(this.AccountOptions.UntrustSignatures);
        public ObservableCollection<object> SelectedUntrustSignatureItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _untrustSignaturesSortInfo;
        public ReactiveCommand<string> UntrustSignaturesSortCommand { get; private set; }

        public ReactiveCommand UntrustSignatureDeleteCommand { get; private set; }
        public ReactiveCommand UntrustSignatureCopyCommand { get; private set; }
        public ReactiveCommand UntrustSignaturePasteCommand { get; private set; }

        public ListCollectionView TagsView => (ListCollectionView)CollectionViewSource.GetDefaultView(this.AccountOptions.Tags);
        public ObservableCollection<object> SelectedTagItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _tagsSortInfo;
        public ReactiveCommand<string> TagsSortCommand { get; private set; }

        public ReactiveCommand TagNewCommand { get; private set; }
        public ReactiveCommand TagDeleteCommand { get; private set; }
        public ReactiveCommand TagCopyCommand { get; private set; }
        public ReactiveCommand TagPasteCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public AccountOptionsControlViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();
        }

        private void Init()
        {
            {
                this.SelectedItem = new ReactiveProperty<string>().AddTo(_disposable);

                this.SignatureNewCommand = new ReactiveCommand().AddTo(_disposable);
                this.SignatureNewCommand.Subscribe(() => this.SignatureNew()).AddTo(_disposable);

                this.SignatureImportCommand = new ReactiveCommand().AddTo(_disposable);
                this.SignatureImportCommand.Subscribe(() => this.SignatureImport()).AddTo(_disposable);

                this.SignatureExportCommand = new ReactiveCommand().AddTo(_disposable);
                this.SignatureExportCommand.Subscribe(() => this.SignatureExport()).AddTo(_disposable);

                this.TrustSignaturesSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.TrustSignaturesSortCommand.Subscribe((propertyName) => this.TrustSignaturesSort(propertyName)).AddTo(_disposable);

                this.TrustSignatureDeleteCommand = new ReactiveCommand().AddTo(_disposable);
                this.TrustSignatureDeleteCommand.Subscribe(() => this.TrustDelete()).AddTo(_disposable);

                this.TrustSignatureCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.TrustSignatureCopyCommand.Subscribe(() => this.TrustCopy()).AddTo(_disposable);

                this.TrustSignaturePasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.TrustSignaturePasteCommand.Subscribe(() => this.TrustPaste()).AddTo(_disposable);

                this.UntrustSignaturesSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.UntrustSignaturesSortCommand.Subscribe((propertyName) => this.UntrustSignaturesSort(propertyName)).AddTo(_disposable);

                this.UntrustSignatureDeleteCommand = new ReactiveCommand().AddTo(_disposable);
                this.UntrustSignatureDeleteCommand.Subscribe(() => this.UntrustDelete()).AddTo(_disposable);

                this.UntrustSignatureCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.UntrustSignatureCopyCommand.Subscribe(() => this.UntrustCopy()).AddTo(_disposable);

                this.UntrustSignaturePasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.UntrustSignaturePasteCommand.Subscribe(() => this.UntrustPaste()).AddTo(_disposable);

                this.TagsSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.TagsSortCommand.Subscribe((propertyName) => this.TagsSort(propertyName)).AddTo(_disposable);

                this.TagNewCommand = new ReactiveCommand().AddTo(_disposable);
                this.TagNewCommand.Subscribe(() => this.TagNew()).AddTo(_disposable);

                this.TagDeleteCommand = new ReactiveCommand().AddTo(_disposable);
                this.TagDeleteCommand.Subscribe(() => this.TagDelete()).AddTo(_disposable);

                this.TagCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.TagCopyCommand.Subscribe(() => this.TagCopy()).AddTo(_disposable);

                this.TagPasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.TagPasteCommand.Subscribe(() => this.TagPaste()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(AccountOptionsControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _trustSignaturesSortInfo = _settings.Load("TrustSignaturesSortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Signature" });
                _untrustSignaturesSortInfo = _settings.Load("UntrustSignaturesSortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Signature" });
                _tagsSortInfo = _settings.Load("TagsSortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Name" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            this.GetOptions();

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                this.TrustSignaturesSort(null);
                this.UntrustSignaturesSort(null);
                this.TagsSort(null);
            }
        }

        private void GetOptions()
        {
            var info = SettingsManager.Instance.AccountInfo;
            this.AccountOptions.DigitalSignature = info.DigitalSignature;
            this.AccountOptions.Comment = info.Comment;
            this.AccountOptions.TrustSignatures.AddRange(info.TrustSignatures);
            this.AccountOptions.UntrustSignatures.AddRange(info.UntrustSignatures);
            this.AccountOptions.Tags.AddRange(info.Tags);
        }

        public void SetOptions()
        {
            var info = SettingsManager.Instance.AccountInfo;

            if (info.DigitalSignature != this.AccountOptions.DigitalSignature)
            {
                info.Exchange = new Exchange(ExchangeAlgorithm.Rsa4096);
            }

            bool uploadFlag = false;

            if (info.DigitalSignature != this.AccountOptions.DigitalSignature
                || info.Comment != this.AccountOptions.Comment
                || !CollectionUtils.Equals(info.TrustSignatures, this.AccountOptions.TrustSignatures)
                || !CollectionUtils.Equals(info.UntrustSignatures, this.AccountOptions.UntrustSignatures)
                || !CollectionUtils.Equals(info.Tags, this.AccountOptions.Tags))
            {
                uploadFlag = true;
            }

            info.DigitalSignature = this.AccountOptions.DigitalSignature;
            info.Comment = this.AccountOptions.Comment;
            info.TrustSignatures.Clear();
            info.TrustSignatures.AddRange(this.AccountOptions.TrustSignatures);
            info.UntrustSignatures.Clear();
            info.UntrustSignatures.AddRange(this.AccountOptions.UntrustSignatures);
            info.Tags.Clear();
            info.Tags.AddRange(this.AccountOptions.Tags);

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

        private void SignatureNew()
        {
            var viewModel = new NameEditWindowViewModel("Anonymous");
            viewModel.Callback += (name) =>
            {
                var digitalSignature = new DigitalSignature(name, DigitalSignatureAlgorithm.EcDsaP521_Sha256_v3);
                this.AccountOptions.DigitalSignature = digitalSignature;
            };

            Messenger.Instance.GetEvent<NameEditWindowShowEvent>()
                .Publish(viewModel);
        }

        private void SignatureImport()
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

                        this.AccountOptions.DigitalSignature = digitalSignature;
                    }
                }
            }
        }

        private void SignatureExport()
        {
            using (var dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.RestoreDirectory = true;
                dialog.FileName = this.AccountOptions.DigitalSignature.GetSignature().ToString();
                dialog.DefaultExt = ".ds";
                dialog.Filter = "DigitalSignature (*.ds)|*.ds";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string fileName = dialog.FileName;

                    using (var fileStream = new FileStream(fileName, FileMode.Create))
                    using (var digitalSignatureStream = DigitalSignatureConverter.ToDigitalSignatureStream(this.AccountOptions.DigitalSignature))
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

        private void TrustSignaturesSort(string propertyName)
        {
            if (propertyName == null)
            {
                if (!string.IsNullOrEmpty(_tagsSortInfo.PropertyName))
                {
                    this.TrustSignaturesSort(_tagsSortInfo.PropertyName, _tagsSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_tagsSortInfo.PropertyName == propertyName)
                {
                    if (_tagsSortInfo.Direction == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                if (!string.IsNullOrEmpty(propertyName))
                {
                    this.TrustSignaturesSort(propertyName, direction);
                }

                _tagsSortInfo.Direction = direction;
                _tagsSortInfo.PropertyName = propertyName;
            }
        }

        private void TrustSignaturesSort(string propertyName, ListSortDirection direction)
        {
            this.TrustSignaturesView.IsLiveSorting = true;
            this.TrustSignaturesView.LiveSortingProperties.Clear();
            this.TrustSignaturesView.SortDescriptions.Clear();

            if (propertyName == "Signature")
            {
                this.TrustSignaturesView.LiveSortingProperties.Add(propertyName);

                this.TrustSignaturesView.CustomSort = new CustomSortComparer(direction, (x, y) =>
                {
                    if (x is Signature tx && y is Signature ty)
                    {
                        int c = tx.Name.CompareTo(ty.Name);
                        if (c != 0) return c;
                        c = Unsafe.Compare(tx.Id, ty.Id);
                        if (c != 0) return c;
                    }

                    return 0;
                });
            }
            else
            {
                this.TrustSignaturesView.LiveSortingProperties.Add(propertyName);
                this.TrustSignaturesView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            }
        }

        private void TrustDelete()
        {
            foreach (var item in this.SelectedTrustSignatureItems.OfType<Signature>().ToArray())
            {
                this.AccountOptions.TrustSignatures.Remove(item);
            }
        }

        private void TrustCopy()
        {
            Clipboard.SetSignatures(this.SelectedTrustSignatureItems.OfType<Signature>().ToArray());
        }

        private void TrustPaste()
        {
            foreach (var item in Clipboard.GetSignatures())
            {
                if (this.AccountOptions.TrustSignatures.Contains(item)) continue;

                this.AccountOptions.TrustSignatures.Add(item);
            }
        }

        private void UntrustSignaturesSort(string propertyName)
        {
            if (propertyName == null)
            {
                if (!string.IsNullOrEmpty(_untrustSignaturesSortInfo.PropertyName))
                {
                    this.UntrustSignaturesSort(_untrustSignaturesSortInfo.PropertyName, _untrustSignaturesSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_untrustSignaturesSortInfo.PropertyName == propertyName)
                {
                    if (_untrustSignaturesSortInfo.Direction == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                if (!string.IsNullOrEmpty(propertyName))
                {
                    this.UntrustSignaturesSort(propertyName, direction);
                }

                _untrustSignaturesSortInfo.Direction = direction;
                _untrustSignaturesSortInfo.PropertyName = propertyName;
            }
        }

        private void UntrustSignaturesSort(string propertyName, ListSortDirection direction)
        {
            this.UntrustSignaturesView.IsLiveSorting = true;
            this.UntrustSignaturesView.LiveSortingProperties.Clear();
            this.UntrustSignaturesView.SortDescriptions.Clear();

            if (propertyName == "Signature")
            {
                this.UntrustSignaturesView.LiveSortingProperties.Add(propertyName);

                this.UntrustSignaturesView.CustomSort = new CustomSortComparer(direction, (x, y) =>
               {
                   if (x is Signature tx && y is Signature ty)
                   {
                       int c = tx.Name.CompareTo(ty.Name);
                       if (c != 0) return c;
                       c = Unsafe.Compare(tx.Id, ty.Id);
                       if (c != 0) return c;
                   }

                   return 0;
               });
            }
            else
            {
                this.UntrustSignaturesView.LiveSortingProperties.Add(propertyName);
                this.UntrustSignaturesView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            }
        }

        private void UntrustDelete()
        {
            foreach (var item in this.SelectedUntrustSignatureItems.OfType<Signature>().ToArray())
            {
                this.AccountOptions.UntrustSignatures.Remove(item);
            }
        }

        private void UntrustCopy()
        {
            Clipboard.SetSignatures(this.SelectedUntrustSignatureItems.OfType<Signature>().ToArray());
        }

        private void UntrustPaste()
        {
            foreach (var item in Clipboard.GetSignatures())
            {
                if (this.AccountOptions.UntrustSignatures.Contains(item)) continue;

                this.AccountOptions.UntrustSignatures.Add(item);
            }
        }

        private void TagsSort(string propertyName)
        {
            if (propertyName == null)
            {
                if (!string.IsNullOrEmpty(_tagsSortInfo.PropertyName))
                {
                    this.TagsSort(_tagsSortInfo.PropertyName, _tagsSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_tagsSortInfo.PropertyName == propertyName)
                {
                    if (_tagsSortInfo.Direction == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                if (!string.IsNullOrEmpty(propertyName))
                {
                    this.TagsSort(propertyName, direction);
                }

                _tagsSortInfo.Direction = direction;
                _tagsSortInfo.PropertyName = propertyName;
            }
        }

        private void TagsSort(string propertyName, ListSortDirection direction)
        {
            this.TagsView.IsLiveSorting = true;
            this.TagsView.LiveSortingProperties.Clear();
            this.TagsView.SortDescriptions.Clear();

            if (propertyName == "Id")
            {
                this.TagsView.LiveSortingProperties.Add(propertyName);

                this.TagsView.CustomSort = new CustomSortComparer(direction, (x, y) => Unsafe.Compare(((Tag)x).Id, ((Tag)y).Id));
            }
            else
            {
                this.TagsView.LiveSortingProperties.Add(propertyName);
                this.TagsView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            }
        }

        private void TagNew()
        {
            var viewModel = new NameEditWindowViewModel("");
            viewModel.Callback += (name) =>
            {
                this.AccountOptions.Tags.Add(new Tag(name, _random.GetBytes(32)));
            };

            Messenger.Instance.GetEvent<NameEditWindowShowEvent>()
                .Publish(viewModel);
        }

        private void TagDelete()
        {
            foreach (var item in this.SelectedTagItems.OfType<Tag>().ToArray())
            {
                this.AccountOptions.Tags.Remove(item);
            }
        }

        private void TagCopy()
        {
            Clipboard.SetTags(this.SelectedTagItems.OfType<Tag>().ToArray());
        }

        private void TagPaste()
        {
            foreach (var item in Clipboard.GetTags())
            {
                if (this.AccountOptions.Tags.Contains(item)) continue;

                this.AccountOptions.Tags.Add(item);
            }
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save("TrustSignaturesSortInfo", _trustSignaturesSortInfo);
                _settings.Save("UntrustSignaturesSortInfo", _untrustSignaturesSortInfo);
                _settings.Save("TagsSortInfo", _tagsSortInfo);
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
