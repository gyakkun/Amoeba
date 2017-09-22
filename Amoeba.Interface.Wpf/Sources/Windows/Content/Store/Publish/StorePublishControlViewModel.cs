using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Amoeba.Service;
using Omnius.Security;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Amoeba.Messages;

namespace Amoeba.Interface
{
    class StorePublishControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;

        private Settings _settings;

        private DialogService _dialogService;

        private LockedHashDictionary<Metadata, SearchState> _cacheStates = new LockedHashDictionary<Metadata, SearchState>();

        public ReactiveProperty<PublishStoreViewModel> TabViewModel { get; private set; }
        public ReactiveProperty<TreeViewModelBase> TabSelectedItem { get; private set; }
        public DragAcceptDescription DragAcceptDescription { get; private set; }

        public ReactiveCommand TabClickCommand { get; private set; }

        public ReactiveCommand TabNewBoxCommand { get; private set; }
        public ReactiveCommand TabEditCommand { get; private set; }
        public ReactiveCommand TabDeleteCommand { get; private set; }
        public ReactiveCommand TabCutCommand { get; private set; }
        public ReactiveCommand TabCopyCommand { get; private set; }
        public ReactiveCommand TabPasteCommand { get; private set; }

        public ReactiveCommand UploadCommand { get; private set; }

        public ListCollectionView ContentsView => (ListCollectionView)CollectionViewSource.GetDefaultView(_contents);
        private ObservableCollection<PublishListViewItemInfo> _contents = new ObservableCollection<PublishListViewItemInfo>();
        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public ReactiveCommand<PublishListViewItemInfo> ListViewDoubleClickCommand { get; private set; }

        public ReactiveCommand NewBoxCommand { get; private set; }
        public ReactiveCommand EditCommand { get; private set; }
        public ReactiveCommand DeleteCommand { get; private set; }
        public ReactiveCommand CutCommand { get; private set; }
        public ReactiveCommand CopyCommand { get; private set; }
        public ReactiveCommand PasteCommand { get; private set; }
        public ReactiveCommand AdvancedCommand { get; private set; }
        public ReactiveCommand<string> AdvancedCopyCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public StorePublishControlViewModel(ServiceManager serviceManager, DialogService dialogService)
        {
            _serviceManager = serviceManager;
            _dialogService = dialogService;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();

            this.DragAcceptDescription = new DragAcceptDescription() { Effects = DragDropEffects.Move, Format = "Amoeba_Publish" };
            this.DragAcceptDescription.DragDrop += this.DragAcceptDescription_DragDrop;
        }

        private void DragAcceptDescription_DragDrop(DragAcceptEventArgs args)
        {
            var src = args.Source as TreeViewModelBase;
            var dest = args.Destination as TreeViewModelBase;
            if (src == null || dest == null) return;

            if (dest.GetAncestors().Contains(src)) return;

            if (dest.TryAdd(src))
            {
                src.Parent.TryRemove(src);
            }

            this.TabViewModel.Value.Model.IsUpdated = true;
        }

        private void Init()
        {
            {
                this.TabViewModel = new ReactiveProperty<PublishStoreViewModel>().AddTo(_disposable);

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.TabClickCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabClickCommand.Where(n => n == this.TabSelectedItem.Value).Subscribe((_) => this.Refresh()).AddTo(_disposable);

                this.TabNewBoxCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabNewBoxCommand.Subscribe(() => this.TabNewBox()).AddTo(_disposable);

                this.TabEditCommand = this.TabSelectedItem.Select(n => n is PublishBoxViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabEditCommand.Subscribe(() => this.TabEdit()).AddTo(_disposable);

                this.TabDeleteCommand = this.TabSelectedItem.Select(n => n is PublishBoxViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabDeleteCommand.Subscribe(() => this.TabDelete()).AddTo(_disposable);

                this.TabCutCommand = this.TabSelectedItem.Select(n => n is PublishBoxViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabCutCommand.Subscribe(() => this.TabCut()).AddTo(_disposable);

                this.TabCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabCopyCommand.Subscribe(() => this.TabCopy()).AddTo(_disposable);

                this.TabPasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabPasteCommand.Subscribe(() => this.TabPaste()).AddTo(_disposable);

                this.UploadCommand = new ReactiveCommand().AddTo(_disposable);
                this.UploadCommand.Subscribe(() => this.Upload()).AddTo(_disposable);

                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.ListViewDoubleClickCommand = new ReactiveCommand<PublishListViewItemInfo>().AddTo(_disposable);
                this.ListViewDoubleClickCommand.Subscribe((target) => this.ListViewDoubleClick(target));

                this.NewBoxCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n == 0 || n == 1 && this.SelectedItems.OfType<PublishListViewItemInfo>().First().Model is PublishBoxInfo).ToReactiveCommand().AddTo(_disposable);
                this.NewBoxCommand.Subscribe(() => this.NewBox()).AddTo(_disposable);

                this.EditCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n == 1 && this.SelectedItems.OfType<PublishListViewItemInfo>().First().Model is PublishBoxInfo).ToReactiveCommand().AddTo(_disposable);
                this.EditCommand.Subscribe(() => this.Edit()).AddTo(_disposable);

                this.DeleteCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.DeleteCommand.Subscribe(() => this.Delete()).AddTo(_disposable);

                this.CutCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.CutCommand.Subscribe(() => this.Cut()).AddTo(_disposable);

                this.CopyCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);

                this.PasteCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n == 0 || n == 1 && this.SelectedItems.OfType<PublishListViewItemInfo>().First().Model is PublishBoxInfo).ToReactiveCommand().AddTo(_disposable);
                this.PasteCommand.Subscribe(() => this.Paste()).AddTo(_disposable);

                this.AdvancedCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);

                this.AdvancedCopyCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.AdvancedCopyCommand.Subscribe((type) => this.AdvancedCopy(type)).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(StorePublishControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                {
                    var model = _settings.Load("PublishStoreInfo_v2", () =>
                    {
                        return new PublishStoreInfo() { IsExpanded = true }; ;
                    });

                    this.TabViewModel.Value = new PublishStoreViewModel(null, model);
                }

                _sortInfo = _settings.Load("SortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Name" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }
        }

        private void Refresh()
        {
            this.TabSelectChanged(this.TabSelectedItem.Value);
        }

        private void TabSelectChanged(TreeViewModelBase viewModel)
        {
            if (viewModel == null || _sortInfo == null) return;

            this.TabSelectChanged(viewModel, _sortInfo.PropertyName, _sortInfo.Direction);
        }

        private void TabSelectChanged(TreeViewModelBase viewModel, string propertyName, ListSortDirection direction)
        {
            if (viewModel is PublishStoreViewModel storeViewModel)
            {
                var publishItems = this.GetPublishItems(storeViewModel.Model.BoxInfos, Array.Empty<Seed>(), propertyName, direction);

                _contents.Clear();
                _contents.AddRange(publishItems);
            }
            else if (viewModel is PublishBoxViewModel boxViewModel)
            {
                var publishItems = this.GetPublishItems(boxViewModel.Model.BoxInfos, boxViewModel.Model.Seeds, propertyName, direction);

                _contents.Clear();
                _contents.AddRange(publishItems);
            }
        }

        private IEnumerable<PublishListViewItemInfo> GetPublishItems(IEnumerable<PublishBoxInfo> boxInfos, IEnumerable<Seed> seeds, string propertyName, ListSortDirection direction)
        {
            var list = new List<PublishListViewItemInfo>();

            foreach (var item in boxInfos)
            {
                var vm = new PublishListViewItemInfo();
                vm.Icon = AmoebaEnvironment.Icons.Box;
                vm.Name = item.Name;
                vm.Length = GetBoxLength(item);
                vm.CreationTime = GetBoxCreationTime(item);

                vm.Model = item;

                list.Add(vm);
            }

            foreach (var item in seeds)
            {
                var vm = new PublishListViewItemInfo();
                vm.Icon = IconUtils.GetImage(item.Name);
                vm.Name = item.Name;
                vm.Length = item.Length;
                vm.CreationTime = item.CreationTime;

                _cacheStates.TryGetValue(item.Metadata, out var state);
                vm.State = state;

                vm.Model = item;

                list.Add(vm);
            }

            return this.Sort(list, propertyName, direction, 100000);
        }

        private long GetBoxLength(PublishBoxInfo boxInfo)
        {
            var seeds = new List<Seed>();
            {
                var boxInfos = new List<PublishBoxInfo>();
                boxInfos.Add(boxInfo);

                for (int i = 0; i < boxInfos.Count; i++)
                {
                    boxInfos.AddRange(boxInfos[i].BoxInfos);
                    seeds.AddRange(boxInfos[i].Seeds);
                }
            }

            if (seeds.Count == 0) return 0;
            else return seeds.Sum(n => n.Length);
        }

        private DateTime GetBoxCreationTime(PublishBoxInfo boxInfo)
        {
            var seeds = new List<Seed>();
            {
                var boxInfos = new List<PublishBoxInfo>();
                boxInfos.Add(boxInfo);

                for (int i = 0; i < boxInfos.Count; i++)
                {
                    boxInfos.AddRange(boxInfos[i].BoxInfos);
                    seeds.AddRange(boxInfos[i].Seeds);
                }
            }

            if (seeds.Count == 0) return DateTime.MinValue;
            else return seeds.Max(n => n.CreationTime);
        }

        private IEnumerable<PublishListViewItemInfo> Sort(IEnumerable<PublishListViewItemInfo> collection, string propertyName, ListSortDirection direction, int maxCount)
        {
            var list = new List<PublishListViewItemInfo>(collection);

            if (propertyName == "Name")
            {
                list.Sort((x, y) =>
                {
                    int c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (propertyName == "Length")
            {
                list.Sort((x, y) =>
                {
                    int c = x.Length.CompareTo(y.Length);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (propertyName == "CreationTime")
            {
                list.Sort((x, y) =>
                {
                    int c = x.CreationTime.CompareTo(y.CreationTime);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (propertyName == "State")
            {
                list.Sort((x, y) =>
                {
                    int c = x.State.CompareTo(y.State);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;

                    return 0;
                });
            }

            if (direction == ListSortDirection.Descending)
            {
                list.Reverse();
            }

            if (list.Count <= maxCount) return list;
            else return list.GetRange(0, maxCount);
        }

        private void WatchThread(CancellationToken token)
        {
            for (;;)
            {
                var cacheMetadatas = new HashSet<Metadata>();
                cacheMetadatas.UnionWith(_serviceManager.GetCacheContentReports().Select(n => n.Metadata));

                var downloadingMetadatas = new HashSet<Metadata>();
                downloadingMetadatas.UnionWith(_serviceManager.GetDownloadContentReports().Select(n => n.Metadata));

                var downloadedMetadatas = new HashSet<Metadata>();
                downloadedMetadatas.UnionWith(SettingsManager.Instance.DownloadedSeeds.Select(n => n.Metadata));

                lock (_cacheStates.LockObject)
                {
                    _cacheStates.Clear();

                    foreach (var metadata in cacheMetadatas)
                    {
                        _cacheStates.Add(metadata, SearchState.Cache);
                    }

                    foreach (var metadata in downloadingMetadatas)
                    {
                        _cacheStates.AddOrUpdate(metadata, SearchState.Downloading, (_, oldValue) => oldValue | SearchState.Downloading);
                    }

                    foreach (var metadata in downloadedMetadatas)
                    {
                        _cacheStates.AddOrUpdate(metadata, SearchState.Downloaded, (_, oldValue) => oldValue | SearchState.Downloaded);
                    }
                }

                if (token.WaitHandle.WaitOne(1000 * 30)) return;
            }
        }

        private PublishBoxInfo CreatePublishBoxInfo(Box targetBox)
        {
            var info = new PublishBoxInfo();
            info.Name = targetBox.Name;
            info.Seeds.AddRange(targetBox.Seeds);

            foreach (var tempBox in targetBox.Boxes)
            {
                info.BoxInfos.Add(CreatePublishBoxInfo(tempBox));
            }

            return info;
        }

        private Box CreateBox(PublishBoxInfo targetBoxInfo)
        {
            string name = targetBoxInfo.Name;
            var seeds = targetBoxInfo.Seeds.ToList();
            var boxes = new List<Box>();

            foreach (var tempBoxInfo in targetBoxInfo.BoxInfos)
            {
                boxes.Add(CreateBox(tempBoxInfo));
            }

            return new Box(name, seeds, boxes);
        }

        private void TabNewBox()
        {
            var viewModel = new NameEditWindowViewModel("");
            viewModel.Callback += (name) =>
            {
                if (this.TabSelectedItem.Value is PublishStoreViewModel storeViewModel)
                {
                    storeViewModel.Model.BoxInfos.Add(new PublishBoxInfo() { Name = name });
                }
                else if (this.TabSelectedItem.Value is PublishBoxViewModel boxViewModel)
                {
                    boxViewModel.Model.BoxInfos.Add(new PublishBoxInfo() { Name = name });
                }
            };

            _dialogService.Show(viewModel);

            this.TabViewModel.Value.Model.IsUpdated = true;
            this.Refresh();
        }

        private void TabEdit()
        {
            if (this.TabSelectedItem.Value is PublishBoxViewModel boxViewModel)
            {
                var viewModel = new NameEditWindowViewModel(boxViewModel.Name.Value);
                viewModel.Callback += (name) =>
                {
                    boxViewModel.Model.Name = name;
                };

                _dialogService.Show(viewModel);
            }

            this.TabViewModel.Value.Model.IsUpdated = true;
        }

        private void TabDelete()
        {
            var viewModel = new ConfirmWindowViewModel(ConfirmWindowType.Delete);
            viewModel.Callback += () =>
            {
                if (this.TabSelectedItem.Value is PublishBoxViewModel boxViewModel)
                {
                    boxViewModel.Parent.TryRemove(boxViewModel);
                }
            };

            _dialogService.Show(viewModel);

            this.TabViewModel.Value.Model.IsUpdated = true;
        }

        private void TabCut()
        {
            if (this.TabSelectedItem.Value is PublishBoxViewModel boxViewModel)
            {
                Clipboard.SetBoxs(new Box[] { CreateBox(boxViewModel.Model) });
                boxViewModel.Parent.TryRemove(boxViewModel);
            }

            this.TabViewModel.Value.Model.IsUpdated = true;
        }

        private void TabCopy()
        {
            if (this.TabSelectedItem.Value is PublishStoreViewModel storeViewModel)
            {
                Clipboard.SetSignatures(new Signature[] { SettingsManager.Instance.AccountInfo.DigitalSignature.GetSignature() });
            }
            else if (this.TabSelectedItem.Value is PublishBoxViewModel boxViewModel)
            {
                Clipboard.SetBoxs(new Box[] { CreateBox(boxViewModel.Model) });
            }
        }

        private void TabPaste()
        {
            if (this.TabSelectedItem.Value is PublishStoreViewModel storeViewModel)
            {
                storeViewModel.Model.BoxInfos.AddRange(Clipboard.GetBoxs().Select(n => CreatePublishBoxInfo(n)).ToArray());
            }
            else if (this.TabSelectedItem.Value is PublishBoxViewModel boxViewModel)
            {
                boxViewModel.Model.Seeds.AddRange(Clipboard.GetSeeds());
                boxViewModel.Model.BoxInfos.AddRange(Clipboard.GetBoxs().Select(n => CreatePublishBoxInfo(n)).ToArray());
            }

            this.TabViewModel.Value.Model.IsUpdated = true;
            this.Refresh();
        }

        private async void Upload()
        {
            var digitalSignature = SettingsManager.Instance.AccountInfo.DigitalSignature;
            if (digitalSignature == null) return;

            var boxes = this.TabViewModel.Value.Model.BoxInfos.Select(n => CreateBox(n)).ToArray();

            await _serviceManager.SetStore(new Store(boxes), digitalSignature, CancellationToken.None);

            this.TabViewModel.Value.Model.IsUpdated = false;
        }

        private void Sort(string propertyName)
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
                this.TabSelectChanged(this.TabSelectedItem.Value, propertyName, direction);
            }

            _sortInfo.Direction = direction;
            _sortInfo.PropertyName = propertyName;
        }

        private void ListViewDoubleClick(PublishListViewItemInfo target)
        {
            if (target.Model is PublishBoxInfo boxInfo)
            {
                if (this.TabSelectedItem.Value is PublishStoreViewModel storeViewModel)
                {
                    var item = storeViewModel.Boxes.FirstOrDefault(n => n.Model == boxInfo);
                    if (item == null) return;

                    item.IsSelected.Value = true;
                }
                else if (this.TabSelectedItem.Value is PublishBoxViewModel boxViewModel)
                {
                    var item = boxViewModel.Boxes.FirstOrDefault(n => n.Model == boxInfo);
                    if (item == null) return;

                    item.IsSelected.Value = true;
                }
            }
        }

        private void NewBox()
        {
            var boxInfo = this.SelectedItems.OfType<PublishListViewItemInfo>().Select(n => n.Model).FirstOrDefault() as PublishBoxInfo;
            if (boxInfo == null)
            {
                this.TabNewBox();
                return;
            }

            var viewModel = new NameEditWindowViewModel("");
            viewModel.Callback += (name) =>
            {
                boxInfo.BoxInfos.Add(new PublishBoxInfo() { Name = name });
            };

            _dialogService.Show(viewModel);

            this.TabViewModel.Value.Model.IsUpdated = true;
        }

        private void Edit()
        {
            var boxInfo = this.SelectedItems.OfType<PublishListViewItemInfo>().Select(n => n.Model).FirstOrDefault() as PublishBoxInfo;
            if (boxInfo == null) return;

            var viewModel = new NameEditWindowViewModel(boxInfo.Name);
            viewModel.Callback += (name) =>
            {
                boxInfo.Name = name;
            };

            _dialogService.Show(viewModel);

            this.TabViewModel.Value.Model.IsUpdated = true;
            this.Refresh();
        }

        private void Delete()
        {
            var viewModel = new ConfirmWindowViewModel(ConfirmWindowType.Delete);
            viewModel.Callback += () =>
            {
                var seeds = this.SelectedItems.OfType<PublishListViewItemInfo>().Select(n => n.Model).OfType<Seed>().ToArray();
                var boxInfos = this.SelectedItems.OfType<PublishListViewItemInfo>().Select(n => n.Model).OfType<PublishBoxInfo>().ToArray();

                if (this.TabSelectedItem.Value is PublishStoreViewModel storeViewModel)
                {
                    foreach (var boxInfo in boxInfos)
                    {
                        storeViewModel.Model.BoxInfos.Remove(boxInfo);
                    }
                }
                else if (this.TabSelectedItem.Value is PublishBoxViewModel boxViewModel)
                {
                    foreach (var boxInfo in boxInfos)
                    {
                        boxViewModel.Model.BoxInfos.Remove(boxInfo);
                    }

                    foreach (var seed in seeds)
                    {
                        boxViewModel.Model.Seeds.Remove(seed);
                    }
                }

                this.TabViewModel.Value.Model.IsUpdated = true;
                this.Refresh();
            };

            _dialogService.Show(viewModel);
        }

        private void Cut()
        {
            var seeds = this.SelectedItems.OfType<PublishListViewItemInfo>().Select(n => n.Model).OfType<Seed>().ToArray();
            var boxInfos = this.SelectedItems.OfType<PublishListViewItemInfo>().Select(n => n.Model).OfType<PublishBoxInfo>().ToArray();

            if (this.TabSelectedItem.Value is PublishStoreViewModel storeViewModel)
            {
                foreach (var boxInfo in boxInfos)
                {
                    storeViewModel.Model.BoxInfos.Remove(boxInfo);
                }
            }
            else if (this.TabSelectedItem.Value is PublishBoxViewModel boxViewModel)
            {
                foreach (var seed in seeds)
                {
                    boxViewModel.Model.Seeds.Remove(seed);
                }

                foreach (var boxInfo in boxInfos)
                {
                    boxViewModel.Model.BoxInfos.Remove(boxInfo);
                }
            }

            Clipboard.SetSeedsAndBoxes(seeds, boxInfos.Select(n => CreateBox(n)));

            this.TabViewModel.Value.Model.IsUpdated = true;
            this.Refresh();
        }

        private void Copy()
        {
            var seeds = this.SelectedItems.OfType<PublishListViewItemInfo>().Select(n => n.Model).OfType<Seed>().ToArray();
            var boxInfos = this.SelectedItems.OfType<PublishListViewItemInfo>().Select(n => n.Model).OfType<PublishBoxInfo>().ToArray();

            Clipboard.SetSeedsAndBoxes(seeds, boxInfos.Select(n => CreateBox(n)));
        }

        private void Paste()
        {
            var boxInfo = this.SelectedItems.OfType<PublishListViewItemInfo>().Select(n => n.Model).FirstOrDefault() as PublishBoxInfo;
            if (boxInfo == null)
            {
                this.TabPaste();
                return;
            }

            {
                var seeds = Clipboard.GetSeeds();
                var boxes = Clipboard.GetBoxs();

                foreach (var seed in seeds)
                {
                    boxInfo.Seeds.Add(seed);
                }

                foreach (var box in boxes)
                {
                    boxInfo.BoxInfos.Add(CreatePublishBoxInfo(box));
                }
            }

            this.TabViewModel.Value.Model.IsUpdated = true;
            this.Refresh();
        }

        private void AdvancedCopy(string type)
        {
            var selectItems = this.SelectedItems.OfType<PublishListViewItemInfo>().ToArray();

            if (type == "Name")
            {
                Clipboard.SetText(string.Join(Environment.NewLine, new HashSet<string>(selectItems.Select(n => n.Name))));
            }
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save("PublishStoreInfo_v2", this.TabViewModel.Value.Model);
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

                _watchTaskManager.Stop();
                _watchTaskManager.Dispose();

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
