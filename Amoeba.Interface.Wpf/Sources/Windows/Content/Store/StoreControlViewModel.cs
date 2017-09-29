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
    class StoreControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;

        private Settings _settings;

        private DialogService _dialogService;

        private LockedHashDictionary<Metadata, SearchState> _cacheStates = new LockedHashDictionary<Metadata, SearchState>();

        public ReactiveProperty<StoreCategoryViewModel> TabViewModel { get; private set; }
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
        private ObservableCollection<StoreListViewItemInfo> _contents = new ObservableCollection<StoreListViewItemInfo>();
        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public ReactiveCommand<StoreListViewItemInfo> ListViewDoubleClickCommand { get; private set; }

        public ReactiveCommand UpMoveCommand { get; private set; }

        public ReactiveCommand NewCategoryCommand { get; private set; }
        public ReactiveCommand EditCommand { get; private set; }
        public ReactiveCommand DeleteCommand { get; private set; }
        public ReactiveCommand CutCommand { get; private set; }
        public ReactiveCommand CopyCommand { get; private set; }
        public ReactiveCommand PasteCommand { get; private set; }
        public ReactiveCommand DownloadCommand { get; private set; }
        public ReactiveCommand AdvancedCommand { get; private set; }
        public ReactiveCommand<string> AdvancedCopyCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public StoreControlViewModel(ServiceManager serviceManager, DialogService dialogService)
        {
            _serviceManager = serviceManager;
            _dialogService = dialogService;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();

            this.DragAcceptDescription = new DragAcceptDescription() { Effects = DragDropEffects.Move, Format = "Amoeba_Store" };
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
                IObservable<object> clipboardObservable;
                {
                    var returnObservable = Observable.Return((object)null);
                    var watchObservable = Observable.FromEventPattern<EventHandler, EventArgs>(h => Clipboard.ClipboardChanged += h, h => Clipboard.ClipboardChanged -= h).Select(n => (object)null);
                    clipboardObservable = Observable.Merge(returnObservable, watchObservable);
                }

                this.TabViewModel = new ReactiveProperty<StoreCategoryViewModel>().AddTo(_disposable);

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.TabClickCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabClickCommand.Where(n => n == this.TabSelectedItem.Value).Subscribe((_) => this.Refresh()).AddTo(_disposable);

                this.TabNewCategoryCommand = this.TabSelectedItem.Select(n => n is StoreCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabNewCategoryCommand.Subscribe(() => this.TabNewCategory()).AddTo(_disposable);

                this.TabEditCommand = this.TabSelectedItem.Select(n => n is StoreCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabEditCommand.Subscribe(() => this.TabEdit()).AddTo(_disposable);

                this.TabDeleteCommand = this.TabSelectedItem.Select(n => n != this.TabViewModel.Value && (n is StoreCategoryViewModel || n is StoreSignatureViewModel)).ToReactiveCommand().AddTo(_disposable);
                this.TabDeleteCommand.Subscribe(() => this.TabDelete()).AddTo(_disposable);

                this.TabCutCommand = this.TabSelectedItem.Select(n => n != this.TabViewModel.Value && (n is StoreCategoryViewModel || n is StoreSignatureViewModel)).ToReactiveCommand().AddTo(_disposable);
                this.TabCutCommand.Subscribe(() => this.TabCut()).AddTo(_disposable);

                this.TabCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabCopyCommand.Subscribe(() => this.TabCopy()).AddTo(_disposable);

                this.TabPasteCommand = this.TabSelectedItem.Select(n => n is StoreCategoryViewModel)
                    .CombineLatest(clipboardObservable.Select(n => Clipboard.ContainsStoreCategoryInfo() || Clipboard.ContainsStoreSignatureInfo()), (r1, r2) => (r1 && r2)).ToReactiveCommand().AddTo(_disposable);
                this.TabPasteCommand.Subscribe(() => this.TabPaste()).AddTo(_disposable);

                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.ListViewDoubleClickCommand = new ReactiveCommand<StoreListViewItemInfo>().AddTo(_disposable);
                this.ListViewDoubleClickCommand.Subscribe((target) => this.ListViewDoubleClick(target));

                this.UpMoveCommand = this.TabSelectedItem.Select(n => n?.Parent != null).ToReactiveCommand().AddTo(_disposable);
                this.UpMoveCommand.Subscribe(() => this.UpMove()).AddTo(_disposable);

                this.NewCategoryCommand = this.TabSelectedItem.Select(n => n is StoreCategoryViewModel)
                    .CombineLatest(this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n == 0 || n == 1 && SelectedMatch(typeof(StoreCategoryInfo))), (r1, r2) => (r1 && r2)).ToReactiveCommand().AddTo(_disposable);
                this.NewCategoryCommand.Subscribe(() => this.NewCategory()).AddTo(_disposable);

                this.EditCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n == 1 && SelectedMatch(typeof(StoreCategoryInfo))).ToReactiveCommand().AddTo(_disposable);
                this.EditCommand.Subscribe(() => this.Edit()).AddTo(_disposable);

                this.DeleteCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n != 0 && (this.TabSelectedItem.Value is StoreCategoryViewModel)).ToReactiveCommand().AddTo(_disposable);
                this.DeleteCommand.Subscribe(() => this.Delete()).AddTo(_disposable);

                this.CutCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n != 0 && (this.TabSelectedItem.Value is StoreCategoryViewModel)).ToReactiveCommand().AddTo(_disposable);
                this.CutCommand.Subscribe(() => this.Cut()).AddTo(_disposable);

                this.CopyCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n != 0 && SelectedMatch(typeof(StoreCategoryInfo), typeof(Seed))).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);

                this.PasteCommand = this.TabSelectedItem.Select(n => n is StoreCategoryViewModel)
                    .CombineLatest(this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n == 0 || n == 1 && SelectedMatch(typeof(StoreCategoryInfo))), (r1, r2) => (r1 && r2))
                    .CombineLatest(clipboardObservable.Select(n => Clipboard.ContainsStoreCategoryInfo() || Clipboard.ContainsStoreSignatureInfo()), (r1, r2) => (r1 && r2)).ToReactiveCommand().AddTo(_disposable);
                this.PasteCommand.Subscribe(() => this.Paste()).AddTo(_disposable);

                this.DownloadCommand = this.SelectedItems.ObserveProperty(n => n.Count)
                    .Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.DownloadCommand.Subscribe(() => this.Download()).AddTo(_disposable);

                this.AdvancedCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0)
                    .ToReactiveCommand().AddTo(_disposable);

                this.AdvancedCopyCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.AdvancedCopyCommand.Subscribe((type) => this.AdvancedCopy(type)).AddTo(_disposable);

                bool SelectedMatch(params Type[] types)
                {
                    object selectedModel = this.SelectedItems.OfType<StoreListViewItemInfo>().First().Model;
                    return types.Any(n => n == selectedModel.GetType());
                }
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(StoreControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                {
                    var model = _settings.Load("StoreCategoryInfo", () =>
                    {
                        var categoryInfo = new StoreCategoryInfo() { Name = "Category", IsExpanded = true };
                        categoryInfo.SignatureInfos.Add(new StoreSignatureInfo() { AuthorSignature = Signature.Parse("Lyrise@i-2IpSdusn_TKfn6NSECLYRVO4r51cpHZ5wFgBo_0eU") });

                        return categoryInfo;
                    });

                    this.TabViewModel.Value = new StoreCategoryViewModel(null, model);
                }

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

            if (viewModel is StoreCategoryViewModel categoryViewModel)
            {
                var items = this.GetListViewItems(categoryViewModel.Model.CategoryInfos, categoryViewModel.Model.SignatureInfos,
                    Array.Empty<StoreBoxInfo>(), Array.Empty<Seed>(), propertyName, direction);

                _contents.AddRange(items);
            }
            else if (viewModel is StoreSignatureViewModel signatureViewModel)
            {
                signatureViewModel.Model.IsUpdated = false;

                var items = this.GetListViewItems(Array.Empty<StoreCategoryInfo>(), Array.Empty<StoreSignatureInfo>(),
                    signatureViewModel.Model.BoxInfos, Array.Empty<Seed>(), propertyName, direction);

                _contents.AddRange(items);
            }
            else if (viewModel is StoreBoxViewModel boxViewModel)
            {
                var items = this.GetListViewItems(Array.Empty<StoreCategoryInfo>(), Array.Empty<StoreSignatureInfo>(),
                    boxViewModel.Model.BoxInfos, boxViewModel.Model.Seeds, propertyName, direction);

                _contents.AddRange(items);
            }
        }

        private IEnumerable<StoreListViewItemInfo> GetListViewItems(IEnumerable<StoreCategoryInfo> categoryInfos, IEnumerable<StoreSignatureInfo> signatureInfos,
            IEnumerable<StoreBoxInfo> boxInfos, IEnumerable<Seed> seeds, string propertyName, ListSortDirection direction)
        {
            var list = new List<StoreListViewItemInfo>();

            foreach (var item in categoryInfos)
            {
                var vm = new StoreListViewItemInfo();
                vm.Icon = AmoebaEnvironment.Icons.Box;
                vm.Name = item.Name;
                vm.Length = item.Length;
                vm.CreationTime = item.CreationTime;

                vm.Model = item;

                list.Add(vm);
            }

            foreach (var item in signatureInfos)
            {
                var vm = new StoreListViewItemInfo();
                vm.Icon = AmoebaEnvironment.Icons.Box;
                vm.Name = item.AuthorSignature.ToString();
                vm.Length = item.Length;
                vm.CreationTime = item.CreationTime;

                vm.Model = item;

                list.Add(vm);
            }

            foreach (var item in boxInfos)
            {
                var vm = new StoreListViewItemInfo();
                vm.Icon = AmoebaEnvironment.Icons.Box;
                vm.Name = item.Name;
                vm.Length = item.Length;
                vm.CreationTime = item.CreationTime;

                vm.Model = item;

                list.Add(vm);
            }

            foreach (var item in seeds)
            {
                var vm = new StoreListViewItemInfo();
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

        private IEnumerable<StoreListViewItemInfo> Sort(IEnumerable<StoreListViewItemInfo> collection, string propertyName, ListSortDirection direction, int maxCount)
        {
            var list = new List<StoreListViewItemInfo>(collection);

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
            for (; ; )
            {
                var subscribeStoreInfos = new List<StoreSignatureInfo>();

                try
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        var subscribeCategoryInfos = new List<StoreCategoryInfo>();
                        subscribeCategoryInfos.Add(this.TabViewModel.Value.Model);

                        for (int i = 0; i < subscribeCategoryInfos.Count; i++)
                        {
                            subscribeCategoryInfos.AddRange(subscribeCategoryInfos[i].CategoryInfos);
                            subscribeStoreInfos.AddRange(subscribeCategoryInfos[i].SignatureInfos);
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
                    if (message == null || storeInfo.UpdateTime == message.CreationTime) continue;

                    var tempBoxInfos = new List<StoreBoxInfo>();

                    foreach (var targetBox in message.Value.Boxes)
                    {
                        tempBoxInfos.Add(CreateStoreBoxInfo(targetBox, storeInfo.BoxInfos.FirstOrDefault(n => n.Name == targetBox.Name)));
                    }

                    try
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            if (token.IsCancellationRequested) return;

                            storeInfo.UpdateTime = message.CreationTime;
                            storeInfo.IsUpdated = true;
                            storeInfo.BoxInfos.Clear();
                            storeInfo.BoxInfos.AddRange(tempBoxInfos);

                            // Refresh
                            {
                                if (this.TabSelectedItem.Value is StoreSignatureViewModel selectedSignatureViewModel)
                                {
                                    if (selectedSignatureViewModel.Model == storeInfo)
                                    {
                                        this.Refresh();
                                    }
                                }
                                else if (this.TabSelectedItem.Value is StoreBoxViewModel selectedBoxViewModel)
                                {
                                    if (selectedBoxViewModel.GetAncestors().OfType<StoreSignatureViewModel>().First().Model == storeInfo)
                                    {
                                        this.Refresh();
                                    }
                                }
                            }
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

        private StoreBoxInfo CreateStoreBoxInfo(Box targetBox, StoreBoxInfo oldBoxInfo)
        {
            var info = new StoreBoxInfo();
            info.IsExpanded = oldBoxInfo?.IsExpanded ?? false;
            info.Name = targetBox.Name;
            info.Seeds.AddRange(targetBox.Seeds);

            foreach (var tempBox in targetBox.Boxes)
            {
                info.BoxInfos.Add(CreateStoreBoxInfo(tempBox, oldBoxInfo?.BoxInfos.FirstOrDefault(n => n.Name == tempBox.Name)));
            }

            return info;
        }

        private void TabNewCategory()
        {
            if (this.TabSelectedItem.Value is StoreCategoryViewModel categoryViewModel)
            {
                var viewModel = new NameEditWindowViewModel("");
                viewModel.Callback += (name) =>
                {
                    categoryViewModel.Model.CategoryInfos.Add(new StoreCategoryInfo() { Name = name });
                };

                _dialogService.Show(viewModel);
            }

            this.Refresh();
        }

        private void TabEdit()
        {
            if (this.TabSelectedItem.Value is StoreCategoryViewModel categoryViewModel)
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
                if (this.TabSelectedItem.Value is StoreCategoryViewModel categoryViewModel)
                {
                    if (categoryViewModel.Parent == null) return;
                    categoryViewModel.Parent.TryRemove(categoryViewModel);
                }
                else if (this.TabSelectedItem.Value is StoreSignatureViewModel signatureViewModel)
                {
                    signatureViewModel.Parent.TryRemove(signatureViewModel);
                }
            };

            _dialogService.Show(viewModel);
        }

        private void TabCut()
        {
            if (this.TabSelectedItem.Value is StoreCategoryViewModel categoryViewModel)
            {
                if (categoryViewModel.Parent == null) return;
                Clipboard.SetStoreCategoryInfos(new StoreCategoryInfo[] { categoryViewModel.Model });
                categoryViewModel.Parent.TryRemove(categoryViewModel);
            }
            else if (this.TabSelectedItem.Value is StoreSignatureViewModel signatureViewModel)
            {
                Clipboard.SetStoreSignatureInfos(new StoreSignatureInfo[] { signatureViewModel.Model });
                signatureViewModel.Parent.TryRemove(signatureViewModel);
            }
        }

        private void TabCopy()
        {
            if (this.TabSelectedItem.Value is StoreCategoryViewModel categoryViewModel)
            {
                Clipboard.SetStoreCategoryInfos(new StoreCategoryInfo[] { categoryViewModel.Model });
            }
            else if (this.TabSelectedItem.Value is StoreSignatureViewModel signatureViewModel)
            {
                Clipboard.SetStoreSignatureInfos(new StoreSignatureInfo[] { signatureViewModel.Model });
            }
        }

        private void TabPaste()
        {
            if (this.TabSelectedItem.Value is StoreCategoryViewModel categoryViewModel)
            {
                categoryViewModel.Model.CategoryInfos.AddRange(Clipboard.GetStoreCategoryInfos());
                categoryViewModel.Model.SignatureInfos.AddRange(Clipboard.GetStoreSignatureInfos());

                foreach (var signature in Clipboard.GetSignatures())
                {
                    if (categoryViewModel.Model.SignatureInfos.Any(n => n.AuthorSignature == signature)) continue;
                    categoryViewModel.Model.SignatureInfos.Add(new StoreSignatureInfo() { AuthorSignature = signature });
                }
            }

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

        private void ListViewDoubleClick(StoreListViewItemInfo target)
        {
            this.TabSelectedItem.Value.IsExpanded.Value = true;

            if (target.Model is StoreCategoryInfo categoryInfo)
            {
                if (this.TabSelectedItem.Value is StoreCategoryViewModel categoryViewModel)
                {
                    var item = categoryViewModel.CategoryViewModels.FirstOrDefault(n => n.Model == categoryInfo);
                    if (item == null) return;

                    item.IsSelected.Value = true;
                }
            }
            else if (target.Model is StoreSignatureInfo signatureInfo)
            {
                if (this.TabSelectedItem.Value is StoreCategoryViewModel categoryViewModel)
                {
                    var item = categoryViewModel.SignatureViewModels.FirstOrDefault(n => n.Model == signatureInfo);
                    if (item == null) return;

                    item.IsSelected.Value = true;
                }
            }
            else if (target.Model is StoreBoxInfo boxInfo)
            {
                if (this.TabSelectedItem.Value is StoreSignatureViewModel signatureViewModel)
                {
                    var item = signatureViewModel.BoxViewModels.FirstOrDefault(n => n.Model == boxInfo);
                    if (item == null) return;

                    item.IsSelected.Value = true;
                }
                else if (this.TabSelectedItem.Value is StoreBoxViewModel boxViewModel)
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
            var categoryInfo = this.SelectedItems.OfType<StoreListViewItemInfo>().Select(n => n.Model).FirstOrDefault() as StoreCategoryInfo;
            if (categoryInfo == null)
            {
                this.TabNewCategory();
                return;
            }

            var viewModel = new NameEditWindowViewModel("");
            viewModel.Callback += (name) =>
            {
                categoryInfo.CategoryInfos.Add(new StoreCategoryInfo() { Name = name });
            };

            _dialogService.Show(viewModel);

            this.Refresh();
        }

        private void Edit()
        {
            object selectedModel = this.SelectedItems.OfType<StoreListViewItemInfo>().Select(n => n.Model).FirstOrDefault();
            if (selectedModel == null) return;

            if (selectedModel is StoreCategoryInfo categoryInfo)
            {
                var viewModel = new NameEditWindowViewModel(categoryInfo.Name);
                viewModel.Callback += (name) =>
                {
                    categoryInfo.Name = name;
                };

                _dialogService.Show(viewModel);
            }

            this.Refresh();
        }

        private void Delete()
        {
            var viewModel = new ConfirmWindowViewModel(ConfirmWindowType.Delete);
            viewModel.Callback += () =>
            {
                var categoryInfos = this.SelectedItems.OfType<StoreListViewItemInfo>().Select(n => n.Model).OfType<StoreCategoryInfo>().ToArray();
                var signatureInfos = this.SelectedItems.OfType<StoreListViewItemInfo>().Select(n => n.Model).OfType<StoreSignatureInfo>().ToArray();

                if (this.TabSelectedItem.Value is StoreCategoryViewModel categoryViewModel)
                {
                    foreach (var categoryInfo in categoryInfos)
                    {
                        categoryViewModel.Model.CategoryInfos.Remove(categoryInfo);
                    }

                    foreach (var signatureInfo in signatureInfos)
                    {
                        categoryViewModel.Model.SignatureInfos.Remove(signatureInfo);
                    }
                }

                this.Refresh();
            };

            _dialogService.Show(viewModel);
        }

        private void Cut()
        {
            var categoryInfos = this.SelectedItems.OfType<StoreListViewItemInfo>().Select(n => n.Model).OfType<StoreCategoryInfo>().ToArray();
            var signatureInfos = this.SelectedItems.OfType<StoreListViewItemInfo>().Select(n => n.Model).OfType<StoreSignatureInfo>().ToArray();

            if (this.TabSelectedItem.Value is StoreCategoryViewModel categoryViewModel)
            {
                foreach (var categoryInfo in categoryInfos)
                {
                    categoryViewModel.Model.CategoryInfos.Remove(categoryInfo);
                }

                foreach (var signatureInfo in signatureInfos)
                {
                    categoryViewModel.Model.SignatureInfos.Remove(signatureInfo);
                }
            }

            Clipboard.SetStoreCategoryInfosAndStoreSignatureInfos(categoryInfos, signatureInfos);

            this.Refresh();
        }

        private void Copy()
        {
            if (this.TabSelectedItem.Value is StoreSignatureViewModel || this.TabSelectedItem.Value is UploadCategoryViewModel)
            {
                var categoryInfos = this.SelectedItems.OfType<StoreListViewItemInfo>().Select(n => n.Model).OfType<StoreCategoryInfo>().ToArray();
                var signatureInfos = this.SelectedItems.OfType<StoreListViewItemInfo>().Select(n => n.Model).OfType<StoreSignatureInfo>().ToArray();

                Clipboard.SetStoreCategoryInfosAndStoreSignatureInfos(categoryInfos, signatureInfos);
            }
            else if (this.TabSelectedItem.Value is UploadDirectoryViewModel)
            {
                var seeds = this.SelectedItems.OfType<StoreListViewItemInfo>().Select(n => n.Model).OfType<Seed>().ToArray();

                Clipboard.SetSeeds(seeds);
            }
            else if (this.TabSelectedItem.Value is UploadBoxViewModel)
            {
                var seeds = this.SelectedItems.OfType<StoreListViewItemInfo>().Select(n => n.Model).OfType<Seed>().ToArray();

                Clipboard.SetSeeds(seeds);
            }
        }

        private void Paste()
        {
            var categoryInfo = this.SelectedItems.OfType<StoreListViewItemInfo>().Select(n => n.Model).FirstOrDefault() as StoreCategoryInfo;
            if (categoryInfo == null)
            {
                this.TabPaste();
                return;
            }

            {
                categoryInfo.CategoryInfos.AddRange(Clipboard.GetStoreCategoryInfos());
                categoryInfo.SignatureInfos.AddRange(Clipboard.GetStoreSignatureInfos());
            }

            this.Refresh();
        }

        private void Download()
        {
            var relativePath = new StringBuilder();

            foreach (var node in this.TabSelectedItem.Value.GetAncestors())
            {
                relativePath.Append(node.Name.Value.Trim(' ') + Path.DirectorySeparatorChar);
            }

            foreach (var seed in this.SelectedItems.OfType<StoreListViewItemInfo>()
                .Select(n => n.Model).OfType<Seed>().ToArray())
            {
                SettingsManager.Instance.DownloadItemInfos.Add(new DownloadItemInfo(seed, Path.Combine(relativePath.ToString(), seed.Name)));
            }

            foreach (var boxInfo in this.SelectedItems.OfType<StoreListViewItemInfo>()
                .Select(n => n.Model).OfType<StoreBoxInfo>().ToArray())
            {
                this.Download(relativePath.ToString(), boxInfo);
            }
        }

        private void Download(string basePath, StoreBoxInfo rootBoxinfo)
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
            var selectItems = this.SelectedItems.OfType<StoreListViewItemInfo>().ToArray();

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
                _settings.Save("StoreCategoryInfo", this.TabViewModel.Value.Model);
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
