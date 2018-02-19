using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Amoeba.Messages;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class UploadControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;
        private TaskManager _uploadWatchTaskManager;

        private Settings _settings;

        private DialogService _dialogService;

        private LockedHashDictionary<Metadata, SearchState> _cacheStates = new LockedHashDictionary<Metadata, SearchState>();

        public ReactiveProperty<bool> IsSyncing { get; private set; }
        public UploadSyncRateInfo SyncRateInfo { get; } = new UploadSyncRateInfo();

        public ReactiveProperty<UploadStoreViewModel> TabViewModel { get; private set; }
        public ReactiveProperty<TreeViewModelBase> TabSelectedItem { get; private set; }
        public DragAcceptDescription DragAcceptDescription { get; private set; }

        public ReactiveCommand TabClickCommand { get; private set; }

        public ReactiveCommand TabNewCategoryCommand { get; private set; }
        public ReactiveCommand TabAddDirectoryCommand { get; private set; }
        public ReactiveCommand TabEditCommand { get; private set; }
        public ReactiveCommand TabDeleteCommand { get; private set; }
        public ReactiveCommand TabCutCommand { get; private set; }
        public ReactiveCommand TabCopyCommand { get; private set; }
        public ReactiveCommand TabPasteCommand { get; private set; }

        public ReactiveCommand SyncCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }

        public ListCollectionView ContentsView => (ListCollectionView)CollectionViewSource.GetDefaultView(_contents);
        private ObservableCollection<UploadListViewItemInfo> _contents = new ObservableCollection<UploadListViewItemInfo>();
        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public ReactiveCommand<UploadListViewItemInfo> ListViewDoubleClickCommand { get; private set; }

        public ReactiveCommand UpMoveCommand { get; private set; }

        public ReactiveCommand NewCategoryCommand { get; private set; }
        public ReactiveCommand AddDirectoryCommand { get; private set; }
        public ReactiveCommand EditCommand { get; private set; }
        public ReactiveCommand DeleteCommand { get; private set; }
        public ReactiveCommand CutCommand { get; private set; }
        public ReactiveCommand CopyCommand { get; private set; }
        public ReactiveCommand PasteCommand { get; private set; }
        public ReactiveCommand ReuploadCommand { get; private set; }
        public ReactiveCommand AdvancedCommand { get; private set; }
        public ReactiveCommand<string> AdvancedCopyCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private UploadItemsInfo _uploadItemsInfo;

        private readonly object _lockObject = new object();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public UploadControlViewModel(ServiceManager serviceManager, DialogService dialogService)
        {
            _serviceManager = serviceManager;
            _dialogService = dialogService;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();

            _uploadWatchTaskManager = new TaskManager(this.UploadWatchThread);
            _uploadWatchTaskManager.Start();

            this.DragAcceptDescription = new DragAcceptDescription() { Effects = DragDropEffects.Move, Format = "Amoeba_Upload" };
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
                IObservable<object> clipboardObservable;
                {
                    var returnObservable = Observable.Return((object)null);
                    var watchObservable = Observable.FromEventPattern<EventHandler, EventArgs>(h => Clipboard.ClipboardChanged += h, h => Clipboard.ClipboardChanged -= h).Select(n => (object)null);
                    clipboardObservable = Observable.Merge(returnObservable, watchObservable);
                }

                this.IsSyncing = new ReactiveProperty<bool>().AddTo(_disposable);

                this.TabViewModel = new ReactiveProperty<UploadStoreViewModel>().AddTo(_disposable);

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.TabClickCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabClickCommand.Where(n => n == this.TabSelectedItem.Value).Subscribe((_) => this.Refresh()).AddTo(_disposable);

                this.TabNewCategoryCommand = this.TabSelectedItem.Select(n => n is UploadStoreViewModel || n is UploadCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabNewCategoryCommand.Subscribe(() => this.TabNewCategory()).AddTo(_disposable);

                this.TabAddDirectoryCommand = this.TabSelectedItem.Select(n => n is UploadStoreViewModel || n is UploadCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabAddDirectoryCommand.Subscribe(() => this.TabAddDirectory()).AddTo(_disposable);

                this.TabEditCommand = this.TabSelectedItem.Select(n => n is UploadCategoryViewModel || n is UploadDirectoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabEditCommand.Subscribe(() => this.TabEdit()).AddTo(_disposable);

                this.TabDeleteCommand = this.TabSelectedItem.Select(n => n is UploadCategoryViewModel || n is UploadDirectoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabDeleteCommand.Subscribe(() => this.TabDelete()).AddTo(_disposable);

                this.TabCutCommand = this.TabSelectedItem.Select(n => n is UploadCategoryViewModel || n is UploadDirectoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabCutCommand.Subscribe(() => this.TabCut()).AddTo(_disposable);

                this.TabCopyCommand = this.TabSelectedItem.Select(n => n is UploadCategoryViewModel || n is UploadDirectoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabCopyCommand.Subscribe(() => this.TabCopy()).AddTo(_disposable);

                this.TabPasteCommand = this.TabSelectedItem.Select(n => n is UploadStoreViewModel || n is UploadCategoryViewModel)
                    .CombineLatest(clipboardObservable.Select(n => Clipboard.ContainsUploadCategoryInfo() || Clipboard.ContainsUploadDirectoryInfo()), (r1, r2) => (r1 && r2)).ToReactiveCommand().AddTo(_disposable);
                this.TabPasteCommand.Subscribe(() => this.TabPaste()).AddTo(_disposable);

                this.SyncCommand = this.IsSyncing.Select(n => !n).ToReactiveCommand().AddTo(_disposable);
                this.SyncCommand.Subscribe(() => this.Sync()).AddTo(_disposable);

                this.CancelCommand = this.IsSyncing.Select(n => n).ToReactiveCommand();
                this.CancelCommand.Subscribe(() => this.Cancel()).AddTo(_disposable);

                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.ListViewDoubleClickCommand = new ReactiveCommand<UploadListViewItemInfo>().AddTo(_disposable);
                this.ListViewDoubleClickCommand.Subscribe((target) => this.ListViewDoubleClick(target));

                this.UpMoveCommand = this.TabSelectedItem.Select(n => n?.Parent != null).ToReactiveCommand().AddTo(_disposable);
                this.UpMoveCommand.Subscribe(() => this.UpMove()).AddTo(_disposable);

                this.NewCategoryCommand = this.TabSelectedItem.Select(n => n is UploadStoreViewModel || n is UploadCategoryViewModel)
                    .CombineLatest(this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n == 0 || n == 1 && SelectedMatch(typeof(UploadStoreInfo), typeof(UploadCategoryInfo))), (r1, r2) => (r1 && r2)).ToReactiveCommand().AddTo(_disposable);
                this.NewCategoryCommand.Subscribe(() => this.NewCategory()).AddTo(_disposable);

                this.AddDirectoryCommand = this.TabSelectedItem.Select(n => n is UploadStoreViewModel || n is UploadCategoryViewModel)
                    .CombineLatest(this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n == 0 || n == 1 && SelectedMatch(typeof(UploadStoreInfo), typeof(UploadCategoryInfo))), (r1, r2) => (r1 && r2)).ToReactiveCommand().AddTo(_disposable);
                this.AddDirectoryCommand.Subscribe(() => this.AddDirectory()).AddTo(_disposable);

                this.EditCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n == 1 && SelectedMatch(typeof(UploadCategoryInfo), typeof(UploadDirectoryInfo))).ToReactiveCommand().AddTo(_disposable);
                this.EditCommand.Subscribe(() => this.Edit()).AddTo(_disposable);

                this.DeleteCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n != 0 && (this.TabSelectedItem.Value is UploadStoreViewModel || this.TabSelectedItem.Value is UploadCategoryViewModel)).ToReactiveCommand().AddTo(_disposable);
                this.DeleteCommand.Subscribe(() => this.Delete()).AddTo(_disposable);

                this.CutCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n != 0 && (this.TabSelectedItem.Value is UploadStoreViewModel || this.TabSelectedItem.Value is UploadCategoryViewModel)).ToReactiveCommand().AddTo(_disposable);
                this.CutCommand.Subscribe(() => this.Cut()).AddTo(_disposable);

                this.CopyCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n != 0 && SelectedMatch(typeof(UploadCategoryInfo), typeof(UploadDirectoryInfo), typeof(Seed))).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);

                this.PasteCommand = this.TabSelectedItem.Select(n => n is UploadStoreViewModel || n is UploadCategoryViewModel)
                    .CombineLatest(this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n == 0 || n == 1 && SelectedMatch(typeof(UploadStoreInfo), typeof(UploadCategoryInfo))), (r1, r2) => (r1 && r2))
                    .CombineLatest(clipboardObservable.Select(n => Clipboard.ContainsUploadCategoryInfo() || Clipboard.ContainsUploadDirectoryInfo()), (r1, r2) => (r1 && r2)).ToReactiveCommand().AddTo(_disposable);
                this.PasteCommand.Subscribe(() => this.Paste()).AddTo(_disposable);

                this.ReuploadCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n != 0 && SelectedMatch(typeof(UploadCategoryInfo), typeof(UploadDirectoryInfo), typeof(Seed))).ToReactiveCommand().AddTo(_disposable);
                this.ReuploadCommand.Subscribe(() => this.Reupload()).AddTo(_disposable);

                this.AdvancedCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);

                this.AdvancedCopyCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.AdvancedCopyCommand.Subscribe((type) => this.AdvancedCopy(type)).AddTo(_disposable);

                clipboardObservable.Publish();

                bool SelectedMatch(params Type[] types)
                {
                    object selectedModel = this.SelectedItems.OfType<UploadListViewItemInfo>().First().Model;
                    return types.Any(n => n == selectedModel.GetType());
                }
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(UploadControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                {
                    var model = _settings.Load("UploadStoreInfo", () =>
                    {
                        return new UploadStoreInfo() { IsExpanded = true }; ;
                    });

                    this.TabViewModel.Value = new UploadStoreViewModel(null, model);
                }

                _uploadItemsInfo = _settings.Load<UploadItemsInfo>("UploadItemsInfo2", () => null);

                _sortInfo = _settings.Load("SortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Name" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                this.Refresh();
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
            _contents.Clear();

            if (viewModel is UploadStoreViewModel storeViewModel)
            {
                var items = this.GetListViewItems(storeViewModel.Model.CategoryInfos, storeViewModel.Model.DirectoryInfos,
                    Array.Empty<UploadBoxInfo>(), Array.Empty<Seed>(), propertyName, direction);

                _contents.AddRange(items);
            }
            else if (viewModel is UploadCategoryViewModel categoryViewModel)
            {
                var items = this.GetListViewItems(categoryViewModel.Model.CategoryInfos, categoryViewModel.Model.DirectoryInfos,
                    Array.Empty<UploadBoxInfo>(), Array.Empty<Seed>(), propertyName, direction);

                _contents.AddRange(items);
            }
            else if (viewModel is UploadDirectoryViewModel directoryViewModel)
            {
                var items = this.GetListViewItems(Array.Empty<UploadCategoryInfo>(), Array.Empty<UploadDirectoryInfo>(),
                    directoryViewModel.Model.BoxInfos, directoryViewModel.Model.Seeds, propertyName, direction);

                _contents.AddRange(items);
            }
            else if (viewModel is UploadBoxViewModel boxViewModel)
            {
                var items = this.GetListViewItems(Array.Empty<UploadCategoryInfo>(), Array.Empty<UploadDirectoryInfo>(),
                    boxViewModel.Model.BoxInfos, boxViewModel.Model.Seeds, propertyName, direction);

                _contents.AddRange(items);
            }
        }

        private IEnumerable<UploadListViewItemInfo> GetListViewItems(IEnumerable<UploadCategoryInfo> categoryInfos,
            IEnumerable<UploadDirectoryInfo> directoryInfos, IEnumerable<UploadBoxInfo> boxInfos, IEnumerable<Seed> seeds,
            string propertyName, ListSortDirection direction)
        {
            var list = new List<UploadListViewItemInfo>();

            foreach (var item in categoryInfos)
            {
                var vm = new UploadListViewItemInfo();
                vm.Group = 0;
                vm.Icon = AmoebaEnvironment.Icons.Box;
                vm.Name = item.Name;
                vm.Length = item.Length;
                vm.CreationTime = item.CreationTime;

                vm.Model = item;

                list.Add(vm);
            }

            foreach (var item in directoryInfos)
            {
                var vm = new UploadListViewItemInfo();
                vm.Group = 1;
                vm.Icon = AmoebaEnvironment.Icons.Box;
                vm.Name = item.Name;
                vm.Length = item.Length;
                vm.CreationTime = item.CreationTime;
                vm.Path = item.Path;

                vm.Model = item;

                list.Add(vm);
            }

            foreach (var item in boxInfos)
            {
                var vm = new UploadListViewItemInfo();
                vm.Group = 2;
                vm.Icon = AmoebaEnvironment.Icons.Box;
                vm.Name = item.Name;
                vm.Length = item.Length;
                vm.CreationTime = item.CreationTime;

                vm.Model = item;

                list.Add(vm);
            }

            foreach (var item in seeds)
            {
                var vm = new UploadListViewItemInfo();
                vm.Group = 3;
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

        private IEnumerable<UploadListViewItemInfo> Sort(IEnumerable<UploadListViewItemInfo> collection, string propertyName, ListSortDirection direction, int maxCount)
        {
            var list = new List<UploadListViewItemInfo>(collection);

            if (propertyName == "Name")
            {
                list.Sort((x, y) =>
                {
                    int c = x.Group.CompareTo(y.Group);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (propertyName == "Length")
            {
                list.Sort((x, y) =>
                {
                    int c = x.Group.CompareTo(y.Group);
                    if (c != 0) return c;
                    c = x.Length.CompareTo(y.Length);
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
                    int c = x.Group.CompareTo(y.Group);
                    if (c != 0) return c;
                    c = x.CreationTime.CompareTo(y.CreationTime);
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
                    int c = x.Group.CompareTo(y.Group);
                    if (c != 0) return c;
                    c = x.State.CompareTo(y.State);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (propertyName == "Path")
            {
                list.Sort((x, y) =>
                {
                    int c = x.Group.CompareTo(y.Group);
                    if (c != 0) return c;
                    c = x.Path.CompareTo(y.Path);
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
            for (; ; )
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

        private void UploadWatchThread(CancellationToken token)
        {
            for (; ; )
            {
                Start:;

                if (token.WaitHandle.WaitOne(1000 * 3)) return;

                var targetUploadItemsInfo = _uploadItemsInfo;

                if (targetUploadItemsInfo == null)
                {
                    try
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            this.IsSyncing.Value = false;
                        }, DispatcherPriority.Background, token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    continue;
                }

                try
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        this.IsSyncing.Value = true;
                    }, DispatcherPriority.Background, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                // Remove
                {
                    var hashMap = new HashSet<string>();
                    hashMap.UnionWith(_serviceManager.GetCacheContentReports().Select(n => n.Path));
                    hashMap.ExceptWith(targetUploadItemsInfo.Map.SelectMany(n => n.Value));

                    foreach (string path in hashMap)
                    {
                        if (token.IsCancellationRequested) return;
                        if (targetUploadItemsInfo != _uploadItemsInfo) goto Start;

                        _serviceManager.RemoveContent(path);
                    }
                }

                // Add
                {
                    var sortedList = new List<string>();
                    {
                        var hashMap = new HashSet<string>();
                        hashMap.UnionWith(targetUploadItemsInfo.Map.SelectMany(n => n.Value));
                        hashMap.ExceptWith(_serviceManager.GetCacheContentReports().Select(n => n.Path));

                        sortedList.AddRange(hashMap);
                        sortedList.Sort((x, y) => x.CompareTo(y));
                    }

                    foreach (var (path, i) in sortedList.Select((n, i) => (n, i)))
                    {
                        if (token.IsCancellationRequested) return;
                        if (targetUploadItemsInfo != _uploadItemsInfo) goto Start;

                        try
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                double value = Math.Round(((double)i / sortedList.Count) * 100, 2);
                                this.SyncRateInfo.Text = $"{value}% {i}/{sortedList.Count}";
                                this.SyncRateInfo.Value = value;
                            }, DispatcherPriority.Background, token);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }

                        try
                        {
                            _serviceManager.AddContent(path, targetUploadItemsInfo.CreationTime, token).Wait();
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }

                        if (token.IsCancellationRequested) return;
                    }
                }

                // Set
                {
                    var reportMap = new Dictionary<string, CacheContentReport>();

                    foreach (var report in _serviceManager.GetCacheContentReports())
                    {
                        reportMap.Add(report.Path, report);
                    }

                    var directoryInfos = new List<UploadDirectoryInfo>();
                    {
                        directoryInfos.AddRange(this.TabViewModel.Value.Model.DirectoryInfos);

                        var categoryInfos = new List<UploadCategoryInfo>();
                        categoryInfos.AddRange(this.TabViewModel.Value.Model.CategoryInfos);

                        for (int i = 0; i < categoryInfos.Count; i++)
                        {
                            categoryInfos.AddRange(categoryInfos[i].CategoryInfos);
                            directoryInfos.AddRange(categoryInfos[i].DirectoryInfos);
                        }
                    }

                    foreach (var directoryInfo in directoryInfos)
                    {
                        var (tempBoxInfos, tempSeeds) = CreateBoxInfosAndSeeds(directoryInfo.Path, reportMap);
                        if (tempBoxInfos.Count() == 0 && tempSeeds.Count() == 0) continue;

                        try
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                directoryInfo.BoxInfos.Clear();
                                directoryInfo.BoxInfos.AddRange(tempBoxInfos);

                                directoryInfo.Seeds.Clear();
                                directoryInfo.Seeds.AddRange(tempSeeds);

                                // Refresh
                                {
                                    if (this.TabSelectedItem.Value is UploadDirectoryViewModel selectedDirectoryViewModel)
                                    {
                                        if (selectedDirectoryViewModel.Model == directoryInfo)
                                        {
                                            this.Refresh();
                                        }
                                    }
                                    else if (this.TabSelectedItem.Value is UploadBoxViewModel selectedBoxViewModel)
                                    {
                                        if (selectedBoxViewModel.GetAncestors().OfType<UploadDirectoryViewModel>().First().Model == directoryInfo)
                                        {
                                            this.Refresh();
                                        }
                                    }
                                }

                                this.TabViewModel.Value.Model.IsUpdated = true;

                            }, DispatcherPriority.Background, token);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                    }
                }

                if (token.IsCancellationRequested) return;

                try
                {
                    DigitalSignature digitalSignature = null;
                    Store store = null;

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        digitalSignature = SettingsManager.Instance.AccountInfo.DigitalSignature;
                        store = StoreBuilder.Create(this.TabViewModel.Value.Model);
                    }, DispatcherPriority.Background, token);

                    _serviceManager.SetStore(store, digitalSignature, token).Wait();

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        this.TabViewModel.Value.Model.IsUpdated = false;
                    }, DispatcherPriority.Background, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                lock (_lockObject)
                {
                    if (targetUploadItemsInfo == _uploadItemsInfo)
                    {
                        _uploadItemsInfo = null;
                    }
                }
            }

            (IEnumerable<UploadBoxInfo>, IEnumerable<Seed>) CreateBoxInfosAndSeeds(string basePath, Dictionary<string, CacheContentReport> reportMap)
            {
                var boxInfos = new List<UploadBoxInfo>();
                var seeds = new List<Seed>();

                foreach (string directoryPath in Directory.GetDirectories(basePath, "*", SearchOption.TopDirectoryOnly))
                {
                    var (tempBoxInfos, tempSeeds) = CreateBoxInfosAndSeeds(directoryPath, reportMap);
                    if (tempBoxInfos.Count() == 0 && tempSeeds.Count() == 0) continue;

                    var childBoxInfo = new UploadBoxInfo();
                    childBoxInfo.Name = Path.GetFileName(directoryPath).Trim();
                    childBoxInfo.BoxInfos.AddRange(tempBoxInfos);
                    childBoxInfo.Seeds.AddRange(tempSeeds);

                    boxInfos.Add(childBoxInfo);
                }

                foreach (string filePath in Directory.GetFiles(basePath, "*", SearchOption.TopDirectoryOnly))
                {
                    if (!reportMap.TryGetValue(filePath, out var report)) continue;

                    seeds.Add(new Seed(Path.GetFileName(filePath), report.Length, report.CreationTime, report.Metadata));
                }

                return (boxInfos, seeds);
            }
        }

        private static class StoreBuilder
        {
            public static Store Create(UploadStoreInfo uploadStoreInfo)
            {
                var tempBoxes = new List<Box>();

                foreach (var categoryInfo in uploadStoreInfo.CategoryInfos)
                {
                    tempBoxes.Add(CreateBox(categoryInfo));
                }

                foreach (var directoryInfo in uploadStoreInfo.DirectoryInfos)
                {
                    tempBoxes.Add(CreateBox(directoryInfo));
                }

                return new Store(tempBoxes);
            }

            private static Box CreateBox(UploadCategoryInfo rootCategoryInfo)
            {
                var tempBoxes = new List<Box>();

                foreach (var categoryInfo in rootCategoryInfo.CategoryInfos)
                {
                    tempBoxes.Add(CreateBox(categoryInfo));
                }

                foreach (var directoryInfo in rootCategoryInfo.DirectoryInfos)
                {
                    tempBoxes.Add(CreateBox(directoryInfo));
                }

                return new Box(rootCategoryInfo.Name, Array.Empty<Seed>(), tempBoxes);
            }

            private static Box CreateBox(UploadDirectoryInfo rootDirectoryInfo)
            {
                var tempBoxes = new List<Box>();

                foreach (var boxInfo in rootDirectoryInfo.BoxInfos)
                {
                    tempBoxes.Add(CreateBox(boxInfo));
                }

                return new Box(rootDirectoryInfo.Name, rootDirectoryInfo.Seeds, tempBoxes);
            }

            private static Box CreateBox(UploadBoxInfo rootBoxInfo)
            {
                var tempBoxes = new List<Box>();

                foreach (var boxInfo in rootBoxInfo.BoxInfos)
                {
                    tempBoxes.Add(CreateBox(boxInfo));
                }

                return new Box(rootBoxInfo.Name, rootBoxInfo.Seeds, tempBoxes);
            }
        }

        private UploadDirectoryInfo CreatePublishDirectoryInfo()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return new UploadDirectoryInfo()
                    {
                        Name = System.IO.Path.GetFileName(dialog.SelectedPath).Trim(),
                        Path = dialog.SelectedPath
                    };
                }
            }

            return null;
        }

        private async void Sync()
        {
            var digitalSignature = SettingsManager.Instance.AccountInfo.DigitalSignature;
            if (digitalSignature == null) return;

            var directoryPaths = new HashSet<string>();
            {
                directoryPaths.UnionWith(this.TabViewModel.Value.Model.DirectoryInfos.Select(n => n.Path));

                var categoryInfos = new List<UploadCategoryInfo>();
                categoryInfos.AddRange(this.TabViewModel.Value.Model.CategoryInfos);

                for (int i = 0; i < categoryInfos.Count; i++)
                {
                    categoryInfos.AddRange(categoryInfos[i].CategoryInfos);
                    directoryPaths.UnionWith(categoryInfos[i].DirectoryInfos.Select(n => n.Path));
                }
            }

            var map = new Dictionary<string, string[]>();

            await Task.Run(() =>
            {
                foreach (string directoryPath in directoryPaths)
                {
                    map.Add(directoryPath, Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories));
                }
            });

            {
                var addItems = new Dictionary<string, long>();
                {
                    foreach (string path in map.SelectMany(n => n.Value))
                    {
                        addItems.Add(path, new FileInfo(path).Length);
                    }

                    foreach (string path in _serviceManager.GetCacheContentReports().Select(n => n.Path))
                    {
                        addItems.Remove(path);
                    }
                }

                var removeItems = new Dictionary<string, long>();
                {
                    foreach (var (path, length) in _serviceManager.GetCacheContentReports().Select(n => (n.Path, n.Length)))
                    {
                        removeItems.Add(path, length);
                    }

                    foreach (string path in map.SelectMany(n => n.Value))
                    {
                        removeItems.Remove(path);
                    }
                }

                var viewModel = new UploadItemsPreviewWindowViewModel(addItems.Select(n => (n.Key, n.Value)), removeItems.Select(n => (n.Key, n.Value)));
                viewModel.Callback += (name) =>
                {
                    lock (_lockObject)
                    {
                        _uploadItemsInfo = new UploadItemsInfo(DateTime.UtcNow, digitalSignature, map);
                    }
                };

                _dialogService.ShowDialog(viewModel);
            }
        }

        private void Cancel()
        {
            lock (_lockObject)
            {
                _uploadItemsInfo = null;
            }
        }

        private void TabNewCategory()
        {
            var viewModel = new NameEditWindowViewModel("", 256);
            viewModel.Callback += (name) =>
            {
                if (this.TabSelectedItem.Value is UploadStoreViewModel storeViewModel)
                {
                    storeViewModel.Model.CategoryInfos.Add(new UploadCategoryInfo() { Name = name });
                }
                else if (this.TabSelectedItem.Value is UploadCategoryViewModel categoryViewModel)
                {
                    categoryViewModel.Model.CategoryInfos.Add(new UploadCategoryInfo() { Name = name });
                }
            };

            _dialogService.ShowDialog(viewModel);

            this.TabViewModel.Value.Model.IsUpdated = true;
            this.Refresh();
        }

        private void TabAddDirectory()
        {
            var directoryInfo = this.CreatePublishDirectoryInfo();
            if (directoryInfo == null) return;

            var viewModel = new UploadDirectoryInfoEditWindowViewModel(directoryInfo);
            viewModel.Callback += (_) =>
            {
                if (this.TabSelectedItem.Value is UploadStoreViewModel storeViewModel)
                {
                    storeViewModel.Model.DirectoryInfos.Add(directoryInfo);
                }
                else if (this.TabSelectedItem.Value is UploadCategoryViewModel categoryViewModel)
                {
                    categoryViewModel.Model.DirectoryInfos.Add(directoryInfo);
                }
            };

            _dialogService.ShowDialog(viewModel);

            this.TabViewModel.Value.Model.IsUpdated = true;
            this.Refresh();
        }

        private void TabEdit()
        {
            if (this.TabSelectedItem.Value is UploadCategoryViewModel categoryViewModel)
            {
                var viewModel = new NameEditWindowViewModel(categoryViewModel.Name.Value, 256);
                viewModel.Callback += (name) =>
                {
                    categoryViewModel.Model.Name = name;
                };

                _dialogService.ShowDialog(viewModel);
            }
            else if (this.TabSelectedItem.Value is UploadDirectoryViewModel directoryViewModel)
            {
                var viewModel = new NameEditWindowViewModel(directoryViewModel.Name.Value, 256);
                viewModel.Callback += (name) =>
                {
                    directoryViewModel.Model.Name = name;
                };

                _dialogService.ShowDialog(viewModel);
            }

            this.TabViewModel.Value.Model.IsUpdated = true;
        }

        private void TabDelete()
        {
            if (_dialogService.ShowDialog(LanguagesManager.Instance.ConfirmWindow_DeleteMessage,
                MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) == MessageBoxResult.OK)
            {
                if (this.TabSelectedItem.Value is UploadCategoryViewModel categoryViewModel)
                {
                    categoryViewModel.Parent.TryRemove(categoryViewModel);
                }
                else if (this.TabSelectedItem.Value is UploadDirectoryViewModel directoryViewModel)
                {
                    directoryViewModel.Parent.TryRemove(directoryViewModel);
                }
            }

            this.TabViewModel.Value.Model.IsUpdated = true;
        }

        private void TabCut()
        {
            if (this.TabSelectedItem.Value is UploadCategoryViewModel categoryViewModel)
            {
                Clipboard.SetUploadCategoryInfos(new UploadCategoryInfo[] { categoryViewModel.Model });
                categoryViewModel.Parent.TryRemove(categoryViewModel);
            }
            else if (this.TabSelectedItem.Value is UploadDirectoryViewModel directoryViewModel)
            {
                Clipboard.SetUploadDirectoryInfos(new UploadDirectoryInfo[] { directoryViewModel.Model });
                directoryViewModel.Parent.TryRemove(directoryViewModel);
            }

            this.TabViewModel.Value.Model.IsUpdated = true;
        }

        private void TabCopy()
        {
            if (this.TabSelectedItem.Value is UploadCategoryViewModel categoryViewModel)
            {
                Clipboard.SetUploadCategoryInfos(new UploadCategoryInfo[] { categoryViewModel.Model });
            }
            else if (this.TabSelectedItem.Value is UploadDirectoryViewModel directoryViewModel)
            {
                Clipboard.SetUploadDirectoryInfos(new UploadDirectoryInfo[] { directoryViewModel.Model });
            }
        }

        private void TabPaste()
        {
            if (this.TabSelectedItem.Value is UploadStoreViewModel storeViewModel)
            {
                storeViewModel.Model.CategoryInfos.AddRange(Clipboard.GetUploadCategoryInfos());
                storeViewModel.Model.DirectoryInfos.AddRange(Clipboard.GetUploadDirectoryInfos());
            }
            else if (this.TabSelectedItem.Value is UploadCategoryViewModel categoryViewModel)
            {
                categoryViewModel.Model.CategoryInfos.AddRange(Clipboard.GetUploadCategoryInfos());
                categoryViewModel.Model.DirectoryInfos.AddRange(Clipboard.GetUploadDirectoryInfos());
            }

            this.TabViewModel.Value.Model.IsUpdated = true;
            this.Refresh();
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

        private void ListViewDoubleClick(UploadListViewItemInfo target)
        {
            this.TabSelectedItem.Value.IsExpanded.Value = true;

            if (target.Model is UploadCategoryInfo categoryInfo)
            {
                if (this.TabSelectedItem.Value is UploadStoreViewModel storeViewModel)
                {
                    var item = storeViewModel.CategoryViewModels.FirstOrDefault(n => n.Model == categoryInfo);
                    if (item == null) return;

                    item.IsSelected.Value = true;
                }
                else if (this.TabSelectedItem.Value is UploadCategoryViewModel categoryViewModel)
                {
                    var item = categoryViewModel.CategoryViewModels.FirstOrDefault(n => n.Model == categoryInfo);
                    if (item == null) return;

                    item.IsSelected.Value = true;
                }
            }
            else if (target.Model is UploadDirectoryInfo directoryInfo)
            {
                if (this.TabSelectedItem.Value is UploadStoreViewModel storeViewModel)
                {
                    var item = storeViewModel.DirectoryViewModels.FirstOrDefault(n => n.Model == directoryInfo);
                    if (item == null) return;

                    item.IsSelected.Value = true;
                }
                else if (this.TabSelectedItem.Value is UploadCategoryViewModel categoryViewModel)
                {
                    var item = categoryViewModel.DirectoryViewModels.FirstOrDefault(n => n.Model == directoryInfo);
                    if (item == null) return;

                    item.IsSelected.Value = true;
                }
            }
            else if (target.Model is UploadBoxInfo boxInfo)
            {
                if (this.TabSelectedItem.Value is UploadDirectoryViewModel directoryViewModel)
                {
                    var item = directoryViewModel.BoxViewModels.FirstOrDefault(n => n.Model == boxInfo);
                    if (item == null) return;

                    item.IsSelected.Value = true;
                }
                else if (this.TabSelectedItem.Value is UploadBoxViewModel boxViewModel)
                {
                    var item = boxViewModel.BoxViewModels.FirstOrDefault(n => n.Model == boxInfo);
                    if (item == null) return;

                    item.IsSelected.Value = true;
                }
            }
        }

        private void UpMove()
        {
            if (this.TabSelectedItem.Value?.Parent == null) return;
            this.TabSelectedItem.Value.Parent.IsSelected.Value = true;
        }

        private void NewCategory()
        {
            var categoryInfo = this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).FirstOrDefault() as UploadCategoryInfo;
            if (categoryInfo == null)
            {
                this.TabNewCategory();
                return;
            }

            var viewModel = new NameEditWindowViewModel("", 256);
            viewModel.Callback += (name) =>
            {
                categoryInfo.CategoryInfos.Add(new UploadCategoryInfo() { Name = name });
            };

            _dialogService.ShowDialog(viewModel);

            this.Refresh();
            this.TabViewModel.Value.Model.IsUpdated = true;
        }

        private void AddDirectory()
        {
            var categoryInfo = this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).FirstOrDefault() as UploadCategoryInfo;
            if (categoryInfo == null)
            {
                this.TabAddDirectory();
                return;
            }

            var directoryInfo = this.CreatePublishDirectoryInfo();
            if (directoryInfo == null) return;

            var viewModel = new UploadDirectoryInfoEditWindowViewModel(directoryInfo);
            viewModel.Callback += (_) =>
            {
                categoryInfo.DirectoryInfos.Add(directoryInfo);
            };

            _dialogService.ShowDialog(viewModel);

            this.Refresh();
            this.TabViewModel.Value.Model.IsUpdated = true;
        }

        private void Edit()
        {
            object selectedModel = this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).FirstOrDefault();
            if (selectedModel == null) return;

            if (selectedModel is UploadCategoryInfo categoryInfo)
            {
                var viewModel = new NameEditWindowViewModel(categoryInfo.Name, 256);
                viewModel.Callback += (name) =>
                {
                    categoryInfo.Name = name;
                };

                _dialogService.ShowDialog(viewModel);
            }
            else if (selectedModel is UploadDirectoryInfo directoryInfo)
            {
                var viewModel = new NameEditWindowViewModel(directoryInfo.Name, 256);
                viewModel.Callback += (name) =>
                {
                    directoryInfo.Name = name;
                };

                _dialogService.ShowDialog(viewModel);
            }

            this.TabViewModel.Value.Model.IsUpdated = true;
            this.Refresh();
        }

        private void Delete()
        {
            if (_dialogService.ShowDialog(LanguagesManager.Instance.ConfirmWindow_DeleteMessage,
                MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) == MessageBoxResult.OK)
            {
                var categoryInfos = this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).OfType<UploadCategoryInfo>().ToArray();
                var directoryInfos = this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).OfType<UploadDirectoryInfo>().ToArray();

                if (this.TabSelectedItem.Value is UploadStoreViewModel storeViewModel)
                {
                    foreach (var categoryInfo in categoryInfos)
                    {
                        storeViewModel.Model.CategoryInfos.Remove(categoryInfo);
                    }

                    foreach (var directoryInfo in directoryInfos)
                    {
                        storeViewModel.Model.DirectoryInfos.Remove(directoryInfo);
                    }
                }
                else if (this.TabSelectedItem.Value is UploadCategoryViewModel categoryViewModel)
                {
                    foreach (var categoryInfo in categoryInfos)
                    {
                        categoryViewModel.Model.CategoryInfos.Remove(categoryInfo);
                    }

                    foreach (var directoryInfo in directoryInfos)
                    {
                        categoryViewModel.Model.DirectoryInfos.Remove(directoryInfo);
                    }
                }

                this.TabViewModel.Value.Model.IsUpdated = true;
                this.Refresh();
            };
        }

        private void Cut()
        {
            var categoryInfos = this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).OfType<UploadCategoryInfo>().ToArray();
            var directoryInfos = this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).OfType<UploadDirectoryInfo>().ToArray();

            if (this.TabSelectedItem.Value is UploadStoreViewModel storeViewModel)
            {
                foreach (var categoryInfo in categoryInfos)
                {
                    storeViewModel.Model.CategoryInfos.Remove(categoryInfo);
                }

                foreach (var directoryInfo in directoryInfos)
                {
                    storeViewModel.Model.DirectoryInfos.Remove(directoryInfo);
                }
            }
            else if (this.TabSelectedItem.Value is UploadCategoryViewModel categoryViewModel)
            {
                foreach (var categoryInfo in categoryInfos)
                {
                    categoryViewModel.Model.CategoryInfos.Remove(categoryInfo);
                }

                foreach (var directoryInfo in directoryInfos)
                {
                    categoryViewModel.Model.DirectoryInfos.Remove(directoryInfo);
                }
            }

            Clipboard.SetUploadCategoryInfosAndUploadDirectoryInfos(categoryInfos, directoryInfos);

            this.TabViewModel.Value.Model.IsUpdated = true;
            this.Refresh();
        }

        private void Copy()
        {
            if (this.TabSelectedItem.Value is UploadStoreViewModel || this.TabSelectedItem.Value is UploadCategoryViewModel)
            {
                var categoryInfos = this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).OfType<UploadCategoryInfo>().ToArray();
                var directoryInfos = this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).OfType<UploadDirectoryInfo>().ToArray();

                Clipboard.SetUploadCategoryInfosAndUploadDirectoryInfos(categoryInfos, directoryInfos);
            }
            else if (this.TabSelectedItem.Value is UploadDirectoryViewModel)
            {
                var seeds = this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).OfType<Seed>().ToArray();

                Clipboard.SetSeeds(seeds);
            }
            else if (this.TabSelectedItem.Value is UploadBoxViewModel)
            {
                var seeds = this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).OfType<Seed>().ToArray();

                Clipboard.SetSeeds(seeds);
            }
        }

        private void Paste()
        {
            var categoryInfo = this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).FirstOrDefault() as UploadCategoryInfo;
            if (categoryInfo == null)
            {
                this.TabPaste();
                return;
            }

            {
                categoryInfo.CategoryInfos.AddRange(Clipboard.GetUploadCategoryInfos());
                categoryInfo.DirectoryInfos.AddRange(Clipboard.GetUploadDirectoryInfos());
            }

            this.TabViewModel.Value.Model.IsUpdated = true;
            this.Refresh();
        }

        private void Reupload()
        {
            var pathList = new HashSet<string>();

            {
                var seeds = new List<Seed>();
                seeds.AddRange(this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).OfType<Seed>());

                {
                    var boxInfos = new List<UploadBoxInfo>();
                    boxInfos.AddRange(this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).OfType<UploadBoxInfo>());

                    {
                        var directoryInfos = new List<UploadDirectoryInfo>();
                        directoryInfos.AddRange(this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).OfType<UploadDirectoryInfo>());

                        {
                            var categoryInfos = new List<UploadCategoryInfo>();
                            categoryInfos.AddRange(this.SelectedItems.OfType<UploadListViewItemInfo>().Select(n => n.Model).OfType<UploadCategoryInfo>());

                            for (int i = 0; i < categoryInfos.Count; i++)
                            {
                                categoryInfos.AddRange(categoryInfos[i].CategoryInfos);
                                directoryInfos.AddRange(categoryInfos[i].DirectoryInfos);
                            }
                        }

                        for (int i = 0; i < directoryInfos.Count; i++)
                        {
                            boxInfos.AddRange(directoryInfos[i].BoxInfos);
                        }
                    }

                    for (int i = 0; i < boxInfos.Count; i++)
                    {
                        boxInfos.AddRange(boxInfos[i].BoxInfos);
                        seeds.AddRange(boxInfos[i].Seeds);
                    }
                }

                var tempMap = new Dictionary<Metadata, string>();
                {
                    foreach (var report in _serviceManager.GetCacheContentReports())
                    {
                        tempMap[report.Metadata] = report.Path;
                    }
                }

                foreach (var seed in seeds)
                {
                    if (tempMap.TryGetValue(seed.Metadata, out var path))
                    {
                        pathList.Add(path);
                    }
                }
            }

            Task.Run(() =>
            {
                try
                {
                    foreach (string path in pathList)
                    {
                        _serviceManager.DiffuseContent(path);
                    }
                }
                catch (Exception)
                {

                }
            });
        }

        private void AdvancedCopy(string type)
        {
            var selectItems = this.SelectedItems.OfType<UploadListViewItemInfo>().ToArray();

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
                _settings.Save("UploadStoreInfo", this.TabViewModel.Value.Model);
                _settings.Save("UploadItemsInfo2", _uploadItemsInfo);
                _settings.Save("SortInfo", _sortInfo);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
            });
        }

        [DataContract(Name = nameof(UploadItemsInfo))]
        private class UploadItemsInfo
        {
            public UploadItemsInfo(DateTime creationTime, DigitalSignature digitalSignature, IReadOnlyDictionary<string, string[]> map)
            {
                this.CreationTime = creationTime;
                this.DigitalSignature = digitalSignature;

                if (map != null)
                {
                    foreach (var (key, value) in map)
                    {
                        this.ProtectedMap.Add(key, value);
                    }
                }
            }

            [DataMember(Name = nameof(CreationTime))]
            public DateTime CreationTime { get; private set; }

            [DataMember(Name = nameof(DigitalSignature))]
            public DigitalSignature DigitalSignature { get; private set; }

            private volatile ReadOnlyDictionary<string, string[]> _readOnlyMap;

            public IReadOnlyDictionary<string, string[]> Map
            {
                get
                {
                    if (_readOnlyMap == null)
                        _readOnlyMap = new ReadOnlyDictionary<string, string[]>(this.ProtectedMap);

                    return _readOnlyMap;
                }
            }

            private Dictionary<string, string[]> _map;

            [DataMember(Name = nameof(Map))]
            private Dictionary<string, string[]> ProtectedMap
            {
                get
                {
                    if (_map == null)
                        _map = new Dictionary<string, string[]>();

                    return _map;
                }
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                Backup.Instance.SaveEvent -= this.Save;

                _watchTaskManager.Stop();
                _watchTaskManager.Dispose();

                _uploadWatchTaskManager.Stop();
                _uploadWatchTaskManager.Dispose();

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
