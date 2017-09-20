using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
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
    class StoreSubscribeControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;

        private Settings _settings;

        private DialogService _dialogService;

        private LockedHashDictionary<Metadata, SearchState> _cacheStates = new LockedHashDictionary<Metadata, SearchState>();

        public ReactiveProperty<SubscribeCategoryViewModel> TabViewModel { get; private set; }
        public ReactiveProperty<TreeViewModelBase> TabSelectedItem { get; private set; }
        public DragAcceptDescription DragAcceptDescription { get; private set; }

        public ReactiveCommand TabClickCommand { get; private set; }

        public ReactiveCommand TabNewCategoryCommand { get; private set; }
        public ReactiveCommand TabEditCommand { get; private set; }
        public ReactiveCommand TabDeleteCommand { get; private set; }
        public ReactiveCommand TabCutCommand { get; private set; }
        public ReactiveCommand TabCopyCommand { get; private set; }
        public ReactiveCommand TabPasteCommand { get; private set; }

        public ListCollectionView ContentsView => (ListCollectionView)CollectionViewSource.GetDefaultView(_contents);
        private ObservableCollection<SubscribeListViewItemInfo> _contents = new ObservableCollection<SubscribeListViewItemInfo>();
        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public ReactiveCommand<SubscribeListViewItemInfo> ListViewDoubleClickCommand { get; private set; }

        public ReactiveCommand CopyCommand { get; private set; }
        public ReactiveCommand DownloadCommand { get; private set; }
        public ReactiveCommand AdvancedCommand { get; private set; }
        public ReactiveCommand<string> AdvancedCopyCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public StoreSubscribeControlViewModel(ServiceManager serviceManager, DialogService dialogService)
        {
            _serviceManager = serviceManager;
            _dialogService = dialogService;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();

            this.DragAcceptDescription = new DragAcceptDescription() { Effects = DragDropEffects.Move, Format = "Amoeba_Subscribe" };
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
        }

        private void Init()
        {
            {
                this.TabViewModel = new ReactiveProperty<SubscribeCategoryViewModel>().AddTo(_disposable);

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.TabClickCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabClickCommand.Where(n => n == this.TabSelectedItem.Value).Subscribe((_) => this.Refresh()).AddTo(_disposable);

                this.TabNewCategoryCommand = this.TabSelectedItem.Select(n => n is SubscribeCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabNewCategoryCommand.Subscribe(() => this.TabNewCategory()).AddTo(_disposable);

                this.TabEditCommand = this.TabSelectedItem.Select(n => n is SubscribeCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabEditCommand.Subscribe(() => this.TabEdit()).AddTo(_disposable);

                this.TabDeleteCommand = this.TabSelectedItem.Select(n => n != this.TabViewModel.Value && (n is SubscribeCategoryViewModel || n is SubscribeStoreViewModel)).ToReactiveCommand().AddTo(_disposable);
                this.TabDeleteCommand.Subscribe(() => this.TabDelete()).AddTo(_disposable);

                this.TabCutCommand = this.TabSelectedItem.Select(n => n != this.TabViewModel.Value && (n is SubscribeCategoryViewModel || n is SubscribeStoreViewModel)).ToReactiveCommand().AddTo(_disposable);
                this.TabCutCommand.Subscribe(() => this.TabCut()).AddTo(_disposable);

                this.TabCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabCopyCommand.Subscribe(() => this.TabCopy()).AddTo(_disposable);

                this.TabPasteCommand = this.TabSelectedItem.Select(n => n is SubscribeCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabPasteCommand.Subscribe(() => this.TabPaste()).AddTo(_disposable);

                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.ListViewDoubleClickCommand = new ReactiveCommand<SubscribeListViewItemInfo>().AddTo(_disposable);
                this.ListViewDoubleClickCommand.Subscribe((target) => this.ListViewDoubleClick(target));

                this.CopyCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);

                this.DownloadCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.DownloadCommand.Subscribe(() => this.Download()).AddTo(_disposable);

                this.AdvancedCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);

                this.AdvancedCopyCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.AdvancedCopyCommand.Subscribe((type) => this.AdvancedCopy(type)).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(StoreSubscribeControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                {
                    var model = _settings.Load("SubscribeCategoryInfo", () =>
                    {
                        var categoryInfo = new SubscribeCategoryInfo() { Name = "Category", IsExpanded = true };
                        categoryInfo.StoreInfos.Add(new SubscribeStoreInfo() { AuthorSignature = Signature.Parse("Lyrise@i-2IpSdusn_TKfn6NSECLYRVO4r51cpHZ5wFgBo_0eU") });

                        return categoryInfo;
                    });

                    this.TabViewModel.Value = new SubscribeCategoryViewModel(null, model);
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
            if (viewModel is SubscribeCategoryViewModel categoryViewModel)
            {
                _contents.Clear();
            }
            else if (viewModel is SubscribeStoreViewModel storeViewModel)
            {
                storeViewModel.Model.IsUpdated = false;

                var subscribeItems = this.GetSubscribeItems(storeViewModel.Model.BoxInfos, Array.Empty<Seed>(), propertyName, direction);

                _contents.Clear();
                _contents.AddRange(subscribeItems);
            }
            else if (viewModel is SubscribeBoxViewModel boxViewModel)
            {
                var subscribeItems = this.GetSubscribeItems(boxViewModel.Model.BoxInfos, boxViewModel.Model.Seeds, propertyName, direction);

                _contents.Clear();
                _contents.AddRange(subscribeItems);
            }
        }

        private IEnumerable<SubscribeListViewItemInfo> GetSubscribeItems(IEnumerable<SubscribeBoxInfo> boxInfos, IEnumerable<Seed> seeds, string propertyName, ListSortDirection direction)
        {
            var list = new List<SubscribeListViewItemInfo>();

            foreach (var item in boxInfos)
            {
                var vm = new SubscribeListViewItemInfo();
                vm.Icon = AmoebaEnvironment.Icons.Box;
                vm.Name = item.Name;
                vm.Length = GetBoxLength(item);
                vm.CreationTime = GetBoxCreationTime(item);

                vm.Model = item;

                list.Add(vm);
            }

            foreach (var item in seeds)
            {
                var vm = new SubscribeListViewItemInfo();
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

        private long GetBoxLength(SubscribeBoxInfo boxInfo)
        {
            var seeds = new List<Seed>();
            {
                var boxInfos = new List<SubscribeBoxInfo>();
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

        private DateTime GetBoxCreationTime(SubscribeBoxInfo boxInfo)
        {
            var seeds = new List<Seed>();
            {
                var boxInfos = new List<SubscribeBoxInfo>();
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

        private IEnumerable<SubscribeListViewItemInfo> Sort(IEnumerable<SubscribeListViewItemInfo> collection, string propertyName, ListSortDirection direction, int maxCount)
        {
            var list = new List<SubscribeListViewItemInfo>(collection);

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
                var subscribeStoreInfos = new List<SubscribeStoreInfo>();

                try
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        var subscribeCategoryInfos = new List<SubscribeCategoryInfo>();
                        subscribeCategoryInfos.Add(this.TabViewModel.Value.Model);

                        for (int i = 0; i < subscribeCategoryInfos.Count; i++)
                        {
                            subscribeCategoryInfos.AddRange(subscribeCategoryInfos[i].CategoryInfos);
                            subscribeStoreInfos.AddRange(subscribeCategoryInfos[i].StoreInfos);
                        }
                    }, DispatcherPriority.Background, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                foreach (var storeInfo in subscribeStoreInfos)
                {
                    if (token.IsCancellationRequested) return;

                    var message = _serviceManager.GetStore(storeInfo.AuthorSignature, CancellationToken.None).Result;
                    if (message == null || storeInfo.CreationTime == message.CreationTime) continue;

                    var tempBoxInfos = new List<SubscribeBoxInfo>();

                    foreach (var targetBox in message.Value.Boxes)
                    {
                        tempBoxInfos.Add(CreateSubscribeBoxInfo(targetBox, storeInfo.BoxInfos.FirstOrDefault(n => n.Name == targetBox.Name)));
                    }

                    try
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            if (token.IsCancellationRequested) return;

                            storeInfo.CreationTime = message.CreationTime;
                            storeInfo.IsUpdated = true;
                            storeInfo.BoxInfos.Clear();
                            storeInfo.BoxInfos.AddRange(tempBoxInfos);
                        }, DispatcherPriority.Background, token);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                }

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
                }

                if (token.WaitHandle.WaitOne(1000 * 30)) return;
            }
        }

        private SubscribeBoxInfo CreateSubscribeBoxInfo(Box targetBox, SubscribeBoxInfo oldBoxInfo)
        {
            var info = new SubscribeBoxInfo();
            info.IsExpanded = oldBoxInfo?.IsExpanded ?? false;
            info.Name = targetBox.Name;
            info.Seeds.AddRange(targetBox.Seeds);

            foreach (var tempBox in targetBox.Boxes)
            {
                info.BoxInfos.Add(CreateSubscribeBoxInfo(tempBox, oldBoxInfo?.BoxInfos.FirstOrDefault(n => n.Name == tempBox.Name)));
            }

            return info;
        }

        private Box CreateBox(SubscribeBoxInfo targetBoxInfo)
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

        private void TabNewCategory()
        {
            if (this.TabSelectedItem.Value is SubscribeCategoryViewModel categoryViewModel)
            {
                var viewModel = new NameEditWindowViewModel("");
                viewModel.Callback += (name) =>
                {
                    categoryViewModel.Model.CategoryInfos.Add(new SubscribeCategoryInfo() { Name = name });
                };

                _dialogService.Show(viewModel);
            }
        }

        private void TabEdit()
        {
            if (this.TabSelectedItem.Value is SubscribeCategoryViewModel categoryViewModel)
            {
                var viewModel = new NameEditWindowViewModel(categoryViewModel.Name.Value);
                viewModel.Callback += (name) =>
                {
                    categoryViewModel.Model.Name = name;
                };

                _dialogService.Show(viewModel);
            }
        }

        private void TabDelete()
        {
            var viewModel = new ConfirmWindowViewModel(ConfirmWindowType.Delete);
            viewModel.Callback += () =>
            {
                if (this.TabSelectedItem.Value is SubscribeCategoryViewModel categoryViewModel)
                {
                    if (categoryViewModel.Parent == null) return;
                    categoryViewModel.Parent.TryRemove(categoryViewModel);
                }
                else if (this.TabSelectedItem.Value is SubscribeStoreViewModel storeViewModel)
                {
                    storeViewModel.Parent.TryRemove(storeViewModel);
                }
            };

            _dialogService.Show(viewModel);
        }

        private void TabCut()
        {
            if (this.TabSelectedItem.Value is SubscribeCategoryViewModel categoryViewModel)
            {
                if (categoryViewModel.Parent == null) return;
                Clipboard.SetSubscribeCategoryInfos(new SubscribeCategoryInfo[] { categoryViewModel.Model });
                categoryViewModel.Parent.TryRemove(categoryViewModel);
            }
            else if (this.TabSelectedItem.Value is SubscribeStoreViewModel storeViewModel)
            {
                Clipboard.SetSubscribeStoreInfos(new SubscribeStoreInfo[] { storeViewModel.Model });
                storeViewModel.Parent.TryRemove(storeViewModel);
            }
        }

        private void TabCopy()
        {
            if (this.TabSelectedItem.Value is SubscribeCategoryViewModel categoryViewModel)
            {
                Clipboard.SetSubscribeCategoryInfos(new SubscribeCategoryInfo[] { categoryViewModel.Model });
            }
            else if (this.TabSelectedItem.Value is SubscribeStoreViewModel storeViewModel)
            {
                Clipboard.SetSubscribeStoreInfos(new SubscribeStoreInfo[] { storeViewModel.Model });
            }
            else if (this.TabSelectedItem.Value is SubscribeBoxViewModel boxViewModel)
            {
                Clipboard.SetBoxs(new Box[] { CreateBox(boxViewModel.Model) });
            }
        }

        private void TabPaste()
        {
            if (this.TabSelectedItem.Value is SubscribeCategoryViewModel categoryViewModel)
            {
                categoryViewModel.Model.CategoryInfos.AddRange(Clipboard.GetSubscribeCategoryInfos());
                categoryViewModel.Model.StoreInfos.AddRange(Clipboard.GetSubscribeStoreInfos());

                foreach (var signature in Clipboard.GetSignatures())
                {
                    if (categoryViewModel.Model.StoreInfos.Any(n => n.AuthorSignature == signature)) continue;
                    categoryViewModel.Model.StoreInfos.Add(new SubscribeStoreInfo() { AuthorSignature = signature });
                }
            }
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

        private void ListViewDoubleClick(SubscribeListViewItemInfo target)
        {
            if (target.Model is SubscribeBoxInfo boxInfo)
            {
                if (this.TabSelectedItem.Value is SubscribeStoreViewModel storeViewModel)
                {
                    var item = storeViewModel.Boxes.FirstOrDefault(n => n.Model == boxInfo);
                    if (item == null) return;

                    item.IsSelected.Value = true;
                }
                else if (this.TabSelectedItem.Value is SubscribeBoxViewModel boxViewModel)
                {
                    var item = boxViewModel.Boxes.FirstOrDefault(n => n.Model == boxInfo);
                    if (item == null) return;

                    item.IsSelected.Value = true;
                }
            }
        }

        private void Copy()
        {
            var seeds = this.SelectedItems.OfType<SubscribeListViewItemInfo>().Select(n => n.Model).OfType<Seed>().ToArray();
            var boxInfos = this.SelectedItems.OfType<SubscribeListViewItemInfo>().Select(n => n.Model).OfType<SubscribeBoxInfo>().ToArray();

            Clipboard.SetSeedsAndBoxes(seeds, boxInfos.Select(n => CreateBox(n)));
        }

        private void Download()
        {
            var relativePath = new StringBuilder();

            foreach (var node in this.TabSelectedItem.Value.GetAncestors())
            {
                relativePath.Append(node.Name.Value.Trim(' ') + Path.DirectorySeparatorChar);
            }

            foreach (var seed in this.SelectedItems.OfType<SubscribeListViewItemInfo>()
                .Select(n => n.Model).OfType<Seed>().ToArray())
            {
                SettingsManager.Instance.DownloadItemInfos.Add(new DownloadItemInfo(seed, Path.Combine(relativePath.ToString(), seed.Name)));
            }

            foreach (var boxInfo in this.SelectedItems.OfType<SubscribeListViewItemInfo>()
                .Select(n => n.Model).OfType<SubscribeBoxInfo>().ToArray())
            {
                this.Download(relativePath.ToString(), boxInfo);
            }
        }

        private void Download(string basePath, SubscribeBoxInfo rootBoxinfo)
        {
            foreach (var seed in rootBoxinfo.Seeds)
            {
                SettingsManager.Instance.DownloadItemInfos.Add(new DownloadItemInfo(seed, Path.Combine(basePath, rootBoxinfo.Name, seed.Name)));
            }

            foreach (var boxInfo in rootBoxinfo.BoxInfos)
            {
                this.Download(Path.Combine(basePath, rootBoxinfo.Name.Trim(' ')), boxInfo);
            }
        }

        private void AdvancedCopy(string type)
        {
            var selectItems = this.SelectedItems.OfType<SubscribeListViewItemInfo>().ToArray();

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
                _settings.Save("SubscribeCategoryInfo", this.TabViewModel.Value.Model);
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
