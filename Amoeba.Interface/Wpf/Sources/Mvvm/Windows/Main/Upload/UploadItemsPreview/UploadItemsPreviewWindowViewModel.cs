using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Data;
using Amoeba.Messages;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class UploadItemsPreviewWindowViewModel : ManagerBase
    {
        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;
        public event Action<IEnumerable<string>> Callback;

        public ListCollectionView AddContentsView => (ListCollectionView)CollectionViewSource.GetDefaultView(_addContents);
        private ObservableCollection<UploadPreviewListViewItemInfo> _addContents = new ObservableCollection<UploadPreviewListViewItemInfo>();
        public ObservableCollection<object> AddSelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _addItemsSortInfo;
        public ReactiveCommand<string> AddItemsSortCommand { get; private set; }

        public ListCollectionView RemoveContentsView => (ListCollectionView)CollectionViewSource.GetDefaultView(_removeContents);
        private ObservableCollection<UploadPreviewListViewItemInfo> _removeContents = new ObservableCollection<UploadPreviewListViewItemInfo>();
        public ObservableCollection<object> RemoveSelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _removeItemsSortInfo;
        public ReactiveCommand<string> RemoveItemsSortCommand { get; private set; }

        public ReactiveProperty<int> AddCount { get; private set; }
        public ReactiveProperty<int> RemoveCount { get; private set; }

        public ReactiveCommand AddItemCopyCommand { get; private set; }
        public ReactiveCommand RemoveItemCopyCommand { get; private set; }

        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public UploadItemsPreviewWindowViewModel(IEnumerable<(string, long)> addInfos, IEnumerable<(string, long)> removeInfos)
        {
            this.Init(addInfos, removeInfos);
        }

        private void Init(IEnumerable<(string, long)> addInfos, IEnumerable<(string, long)> removeInfos)
        {
            {
                this.AddItemsSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.AddItemsSortCommand.Subscribe((propertyName) => this.AddItemsSort(propertyName)).AddTo(_disposable);

                this.RemoveItemsSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.RemoveItemsSortCommand.Subscribe((propertyName) => this.RemoveItemsSort(propertyName)).AddTo(_disposable);

                this.AddCount = new ReactiveProperty<int>().AddTo(_disposable);
                this.RemoveCount = new ReactiveProperty<int>().AddTo(_disposable);

                this.AddItemCopyCommand = this.AddSelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.AddItemCopyCommand.Subscribe(() => this.AddItemCopy()).AddTo(_disposable);

                this.RemoveItemCopyCommand = this.RemoveSelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.RemoveItemCopyCommand.Subscribe(() => this.RemoveItemCopy()).AddTo(_disposable);

                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);

                this.CancelCommand = new ReactiveCommand().AddTo(_disposable);
                this.CancelCommand.Subscribe(() => this.Cancel()).AddTo(_disposable);
            }

            {
                foreach (var (path, length) in addInfos)
                {
                    var viewModel = new UploadPreviewListViewItemInfo();
                    viewModel.Icon = IconUtils.GetImage(path);
                    viewModel.Name = Path.GetFileName(path);
                    viewModel.Length = length;
                    viewModel.Path = path;

                    _addContents.Add(viewModel);
                }

                foreach (var (path, length) in removeInfos)
                {
                    var viewModel = new UploadPreviewListViewItemInfo();
                    viewModel.Icon = IconUtils.GetImage(path);
                    viewModel.Name = Path.GetFileName(path);
                    viewModel.Length = length;
                    viewModel.Path = path;

                    _removeContents.Add(viewModel);
                }

                this.AddCount.Value = _addContents.Count;
                this.RemoveCount.Value = _removeContents.Count;
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(UploadItemsPreviewWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _addItemsSortInfo = _settings.Load("AddItemsSortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Name" });
                _removeItemsSortInfo = _settings.Load("RemoveItemsSortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Name" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                this.AddItemsSort(null);
                this.RemoveItemsSort(null);
            }
        }

        private void AddItemsSort(string propertyName)
        {
            if (propertyName == null)
            {
                if (!string.IsNullOrEmpty(_addItemsSortInfo.PropertyName))
                {
                    this.AddItemsSort(_addItemsSortInfo.PropertyName, _addItemsSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_addItemsSortInfo.PropertyName == propertyName)
                {
                    if (_addItemsSortInfo.Direction == ListSortDirection.Ascending)
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
                    this.AddItemsSort(propertyName, direction);
                }

                _addItemsSortInfo.Direction = direction;
                _addItemsSortInfo.PropertyName = propertyName;
            }
        }

        private void AddItemsSort(string propertyName, ListSortDirection direction)
        {
            this.AddContentsView.IsLiveSorting = true;
            this.AddContentsView.LiveSortingProperties.Clear();
            this.AddContentsView.SortDescriptions.Clear();

            this.AddContentsView.LiveSortingProperties.Add(propertyName);
            this.AddContentsView.SortDescriptions.Add(new SortDescription(propertyName, direction));
        }

        private void RemoveItemsSort(string propertyName)
        {
            if (propertyName == null)
            {
                if (!string.IsNullOrEmpty(_removeItemsSortInfo.PropertyName))
                {
                    this.RemoveItemsSort(_removeItemsSortInfo.PropertyName, _removeItemsSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_removeItemsSortInfo.PropertyName == propertyName)
                {
                    if (_removeItemsSortInfo.Direction == ListSortDirection.Ascending)
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
                    this.RemoveItemsSort(propertyName, direction);
                }

                _removeItemsSortInfo.Direction = direction;
                _removeItemsSortInfo.PropertyName = propertyName;
            }
        }

        private void RemoveItemsSort(string propertyName, ListSortDirection direction)
        {
            this.RemoveContentsView.IsLiveSorting = true;
            this.RemoveContentsView.LiveSortingProperties.Clear();
            this.RemoveContentsView.SortDescriptions.Clear();

            this.RemoveContentsView.LiveSortingProperties.Remove(propertyName);
            this.RemoveContentsView.SortDescriptions.Remove(new SortDescription(propertyName, direction));
        }

        private void AddItemCopy()
        {
            var selectedItems = new HashSet<string>(this.AddSelectedItems.OfType<UploadPreviewListViewItemInfo>()
                .Select(n => n.Path));

            Clipboard.SetText(string.Join(Environment.NewLine, selectedItems));
        }

        private void RemoveItemCopy()
        {
            var selectedItems = new HashSet<string>(this.RemoveSelectedItems.OfType<UploadPreviewListViewItemInfo>()
                .Select(n => n.Path));

            Clipboard.SetText(string.Join(Environment.NewLine, selectedItems));
        }

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void Ok()
        {
            this.Callback?.Invoke(_addContents.Select(n => n.Path).ToArray());

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
                _settings.Save("AddItemsSortInfo", _addItemsSortInfo);
                _settings.Save("RemoveItemsSortInfo", _removeItemsSortInfo);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
            });
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                Backup.Instance.SaveEvent -= this.Save;

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
