using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Data;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class UploadPreviewWindowViewModel : ManagerBase
    {
        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;
        public event Action<IEnumerable<string>> Callback;

        public ListCollectionView ContentsView => (ListCollectionView)CollectionViewSource.GetDefaultView(_contents);
        private ObservableCollection<UploadPreviewListViewItemInfo> _contents = new ObservableCollection<UploadPreviewListViewItemInfo>();
        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public ReactiveCommand DeleteCommand { get; private set; }
        public ReactiveCommand CopyCommand { get; private set; }

        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public UploadPreviewWindowViewModel(IEnumerable<string> paths)
        {
            this.Init(paths);
        }

        private void Init(IEnumerable<string> paths)
        {
            {
                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.DeleteCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.DeleteCommand.Subscribe(() => this.Delete()).AddTo(_disposable);

                this.CopyCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);

                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);

                this.CancelCommand = new ReactiveCommand().AddTo(_disposable);
                this.CancelCommand.Subscribe(() => this.Cancel()).AddTo(_disposable);
            }

            {
                foreach (string path in paths)
                {
                    var viewModel = new UploadPreviewListViewItemInfo();
                    viewModel.Icon = IconUtils.GetImage(path);
                    viewModel.Name = Path.GetFileName(path);
                    viewModel.Length = new FileInfo(path).Length;
                    viewModel.Path = path;

                    _contents.Add(viewModel);
                }
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(UploadPreviewWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _sortInfo = _settings.Load("SortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Name" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                this.Sort(null);
            }
        }

        private void Sort(string propertyName)
        {
            if (propertyName == null)
            {
                if (!string.IsNullOrEmpty(_sortInfo.PropertyName))
                {
                    this.Sort(_sortInfo.PropertyName, _sortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_sortInfo.PropertyName == propertyName)
                {
                    if (_sortInfo.Direction == ListSortDirection.Ascending)
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
                    this.Sort(propertyName, direction);
                }

                _sortInfo.Direction = direction;
                _sortInfo.PropertyName = propertyName;
            }
        }

        private void Sort(string propertyName, ListSortDirection direction)
        {
            this.ContentsView.IsLiveSorting = true;
            this.ContentsView.LiveSortingProperties.Clear();
            this.ContentsView.SortDescriptions.Clear();

            this.ContentsView.LiveSortingProperties.Add(propertyName);
            this.ContentsView.SortDescriptions.Add(new SortDescription(propertyName, direction));
        }

        private void Delete()
        {
            var selectedItems = this.SelectedItems.OfType<UploadPreviewListViewItemInfo>().ToList();

            foreach (var item in selectedItems)
            {
                _contents.Remove(item);
            }
        }

        private void Copy()
        {
            var selectedItems = new HashSet<string>(this.SelectedItems.OfType<UploadPreviewListViewItemInfo>()
                .Select(n => n.Path));

            Clipboard.SetText(string.Join(Environment.NewLine, selectedItems));
        }

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void Ok()
        {
            this.Callback?.Invoke(_contents.Select(n => n.Path).ToArray());

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
                _settings.Save("SortInfo", _sortInfo);
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
