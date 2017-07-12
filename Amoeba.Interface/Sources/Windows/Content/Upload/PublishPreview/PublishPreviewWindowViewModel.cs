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
    class PublishPreviewWindowViewModel : ManagerBase
    {
        private PublishPreviewCategoryInfo _previewBoxInfo;

        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;
        public event Action Callback;

        public ReactiveProperty<PublishPreviewCategoryViewModel> TabViewModel { get; private set; }
        public ReactiveProperty<TreeViewModelBase> TabSelectedItem { get; private set; }

        public ReactiveCommand TabClickCommand { get; private set; }

        public ListCollectionView ContentsView => (ListCollectionView)CollectionViewSource.GetDefaultView(_contents);
        private ObservableCollection<PublishPreviewItemViewModel> _contents = new ObservableCollection<PublishPreviewItemViewModel>();
        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public PublishPreviewWindowViewModel(PublishPreviewCategoryInfo info)
        {
            _previewBoxInfo = info;

            this.Init();
        }

        private void Init()
        {
            {
                this.TabViewModel = new ReactiveProperty<PublishPreviewCategoryViewModel>().AddTo(_disposable);
                this.TabViewModel.Value = new PublishPreviewCategoryViewModel(null, _previewBoxInfo);

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.TabClickCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabClickCommand.Subscribe(() => this.TabSelectChanged(this.TabSelectedItem.Value)).AddTo(_disposable);

                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);

                this.CancelCommand = new ReactiveCommand().AddTo(_disposable);
                this.CancelCommand.Subscribe(() => this.Cancel()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(PublishPreviewWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _sortInfo = _settings.Load("SortInfo", () => new ListSortInfo());
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                this.Sort(null);
            }
        }

        private void TabSelectChanged(TreeViewModelBase viewModel)
        {
            if (viewModel is PublishPreviewCategoryViewModel boxViewModel)
            {
                var list = new List<PublishPreviewItemViewModel>();

                foreach (var item in boxViewModel.Model.CategoryInfos)
                {
                    var vm = new PublishPreviewItemViewModel();
                    vm.Icon = AmoebaEnvironment.Icons.Box;
                    vm.Name = item.Name;
                    vm.Length = GetBoxLength(item);

                    list.Add(vm);
                }

                foreach (var item in boxViewModel.Model.SeedInfos)
                {
                    var vm = new PublishPreviewItemViewModel();
                    vm.Icon = IconUtils.GetImage(item.Name);
                    vm.Name = item.Name;
                    vm.Length = item.Length;

                    list.Add(vm);
                }

                _contents.Clear();
                _contents.AddRange(list);
            }
        }

        private long GetBoxLength(PublishPreviewCategoryInfo boxInfo)
        {
            var seedInfos = new List<PublishPreviewSeedInfo>();
            {
                var boxInfos = new List<PublishPreviewCategoryInfo>();
                boxInfos.Add(boxInfo);

                for (int i = 0; i < boxInfos.Count; i++)
                {
                    boxInfos.AddRange(boxInfos[i].CategoryInfos);
                    seedInfos.AddRange(boxInfos[i].SeedInfos);
                }
            }

            if (seedInfos.Count == 0) return 0;
            else return seedInfos.Sum(n => n.Length);
        }

        private void Sort(string propertyName)
        {
            if (propertyName == null)
            {
                this.ContentsView.SortDescriptions.Clear();

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

                this.ContentsView.SortDescriptions.Clear();

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
            this.ContentsView.LiveSortingProperties.Add(propertyName);
            this.ContentsView.SortDescriptions.Add(new SortDescription(propertyName, direction));
        }

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void Ok()
        {
            this.Callback?.Invoke();

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
