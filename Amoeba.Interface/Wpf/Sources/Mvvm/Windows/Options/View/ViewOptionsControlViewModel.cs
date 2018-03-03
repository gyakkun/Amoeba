using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Data;
using Amoeba.Rpc;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class ViewOptionsControlViewModel : ManagerBase
    {
        private DialogService _dialogService;

        private Settings _settings;

        public ViewOptionsInfo Options { get; }

        public ReactiveProperty<string> SelectedItem { get; private set; }

        public ListCollectionView SubscribeSignaturesView => (ListCollectionView)CollectionViewSource.GetDefaultView(this.Options.Subscribe.Signatures);
        public ObservableCollection<object> SelectedSubscribeSignatureItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _subscribeSignaturesSortInfo;
        public ReactiveCommand<string> SubscribeSignaturesSortCommand { get; private set; }

        public ReactiveCommand SubscribeDeleteCommand { get; private set; }
        public ReactiveCommand SubscribeCopyCommand { get; private set; }
        public ReactiveCommand SubscribePasteCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public ViewOptionsControlViewModel(ViewOptionsInfo options, DialogService dialogService)
        {
            _dialogService = dialogService;

            this.Options = options;

            this.Init();
        }

        private void Init()
        {
            {
                this.SelectedItem = new ReactiveProperty<string>().AddTo(_disposable);

                this.SubscribeSignaturesSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SubscribeSignaturesSortCommand.Subscribe((propertyName) => this.SubscribeSignaturesSort(propertyName)).AddTo(_disposable);

                this.SubscribeDeleteCommand = new ReactiveCommand().AddTo(_disposable);
                this.SubscribeDeleteCommand.Subscribe(() => this.SubscribeDelete()).AddTo(_disposable);

                this.SubscribeCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.SubscribeCopyCommand.Subscribe(() => this.SubscribeCopy()).AddTo(_disposable);

                this.SubscribePasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.SubscribePasteCommand.Subscribe(() => this.SubscribePaste()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "View", nameof(ViewOptionsControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _subscribeSignaturesSortInfo = _settings.Load("SubscribeSignaturesSortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Signature" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                EventHooks.Instance.SaveEvent += this.Save;
            }

            {
                this.SubscribeSignaturesSort(null);
            }
        }

        private void SubscribeSignaturesSort(string propertyName)
        {
            if (propertyName == null)
            {
                if (!string.IsNullOrEmpty(_subscribeSignaturesSortInfo.PropertyName))
                {
                    this.SubscribeSignaturesSort(_subscribeSignaturesSortInfo.PropertyName, _subscribeSignaturesSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_subscribeSignaturesSortInfo.PropertyName == propertyName)
                {
                    if (_subscribeSignaturesSortInfo.Direction == ListSortDirection.Ascending)
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
                    this.SubscribeSignaturesSort(propertyName, direction);
                }

                _subscribeSignaturesSortInfo.Direction = direction;
                _subscribeSignaturesSortInfo.PropertyName = propertyName;
            }
        }

        private void SubscribeSignaturesSort(string propertyName, ListSortDirection direction)
        {
            this.SubscribeSignaturesView.IsLiveSorting = true;
            this.SubscribeSignaturesView.LiveSortingProperties.Clear();
            this.SubscribeSignaturesView.SortDescriptions.Clear();

            if (propertyName == "Signature")
            {
                this.SubscribeSignaturesView.LiveSortingProperties.Add(propertyName);

                var view = this.SubscribeSignaturesView;
                view.CustomSort = new CustomSortComparer(direction, (x, y) =>
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
                this.SubscribeSignaturesView.LiveSortingProperties.Add(propertyName);
                this.SubscribeSignaturesView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            }
        }

        private void SubscribeDelete()
        {
            foreach (var item in this.SelectedSubscribeSignatureItems.OfType<Signature>().ToArray())
            {
                this.Options.Subscribe.Signatures.Remove(item);
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
                if (this.Options.Subscribe.Signatures.Contains(item)) continue;

                this.Options.Subscribe.Signatures.Add(item);
            }
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save("SubscribeSignaturesSortInfo", _subscribeSignaturesSortInfo);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
            });
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                EventHooks.Instance.SaveEvent -= this.Save;

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
