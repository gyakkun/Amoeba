using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Net.Amoeba;
using Omnius.Security;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class SearchControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private MessageManager _messageManager;

        private TaskManager _watchTaskManager;

        private Settings _settings;

        private LockedList<SearchListViewItemInfo> _cache_SearchItems = new LockedList<SearchListViewItemInfo>();

        public ReactiveProperty<string> SearchInput { get; private set; }
        public ReactiveCommand SearchCommand { get; private set; }

        public ReactiveProperty<SearchViewModel> TabViewModel { get; private set; }
        public ReactiveProperty<TreeViewModelBase> TabSelectedItem { get; private set; }
        public DragAcceptDescription DragAcceptDescription { get; private set; }

        public ReactiveCommand TabClickCommand { get; private set; }

        public ReactiveCommand TabNewSearchCommand { get; private set; }
        public ReactiveCommand TabEditCommand { get; private set; }
        public ReactiveCommand TabDeleteCommand { get; private set; }
        public ReactiveCommand TabCutCommand { get; private set; }
        public ReactiveCommand TabCopyCommand { get; private set; }
        public ReactiveCommand TabPasteCommand { get; private set; }

        public ListCollectionView ContentsView => (ListCollectionView)CollectionViewSource.GetDefaultView(_contents);
        private ObservableCollection<SearchListViewItemInfo> _contents = new ObservableCollection<SearchListViewItemInfo>();
        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public ReactiveCommand CopyCommand { get; private set; }
        public ReactiveCommand DownloadCommand { get; private set; }
        public ReactiveCommand AdvancedCommand { get; private set; }
        public ReactiveCommand<string> AdvancedCopyCommand { get; private set; }
        public ReactiveCommand RemoveCacheItemCommand { get; private set; }
        public ReactiveCommand RemoveDownloadingItemCommand { get; private set; }
        public ReactiveCommand RemoveDownloadedItemCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public SearchControlViewModel(ServiceManager serviceManager, MessageManager messageManager)
        {
            _serviceManager = serviceManager;
            _messageManager = messageManager;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();

            this.DragAcceptDescription = new DragAcceptDescription() { Effects = DragDropEffects.Move, Format = "Amoeba_Search" };
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
                this.SearchInput = new ReactiveProperty<string>().AddTo(_disposable);

                this.SearchCommand = this.SearchInput.Select(n => !string.IsNullOrWhiteSpace(n)).ToReactiveCommand().AddTo(_disposable);
                this.SearchCommand.Subscribe(() => this.Search());

                this.TabViewModel = new ReactiveProperty<SearchViewModel>().AddTo(_disposable);

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.TabClickCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabClickCommand.Subscribe(() => this.Refresh()).AddTo(_disposable);

                this.TabNewSearchCommand = this.TabSelectedItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.TabNewSearchCommand.Subscribe(() => this.TabNewSearch()).AddTo(_disposable);

                this.TabEditCommand = this.TabSelectedItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.TabEditCommand.Subscribe(() => this.TabEdit()).AddTo(_disposable);

                this.TabDeleteCommand = this.TabSelectedItem.Select(n => n != this.TabViewModel.Value).ToReactiveCommand().AddTo(_disposable);
                this.TabDeleteCommand.Subscribe(() => this.TabDelete()).AddTo(_disposable);

                this.TabCutCommand = this.TabSelectedItem.Select(n => n != this.TabViewModel.Value).ToReactiveCommand().AddTo(_disposable);
                this.TabCutCommand.Subscribe(() => this.TabCut()).AddTo(_disposable);

                this.TabCopyCommand = this.TabSelectedItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.TabCopyCommand.Subscribe(() => this.TabCopy()).AddTo(_disposable);

                this.TabPasteCommand = this.TabSelectedItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.TabPasteCommand.Subscribe(() => this.TabPaste()).AddTo(_disposable);

                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.CopyCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);

                this.DownloadCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.DownloadCommand.Subscribe(() => this.Download()).AddTo(_disposable);

                this.AdvancedCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);

                this.AdvancedCopyCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.AdvancedCopyCommand.Subscribe((type) => this.AdvancedCopy(type)).AddTo(_disposable);

                this.RemoveCacheItemCommand = new ReactiveCommand().AddTo(_disposable);
                this.RemoveCacheItemCommand.Subscribe(() => this.RemoveCacheItem()).AddTo(_disposable);

                this.RemoveDownloadingItemCommand = new ReactiveCommand().AddTo(_disposable);
                this.RemoveDownloadingItemCommand.Subscribe(() => this.RemoveDownloadingItem()).AddTo(_disposable);

                this.RemoveDownloadedItemCommand = new ReactiveCommand().AddTo(_disposable);
                this.RemoveDownloadedItemCommand.Subscribe(() => this.RemoveDownloadedItem()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(SearchControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                {
                    var model = _settings.Load("SearchInfo", () =>
                    {
                        var searchInfo = new SearchInfo() { Name = "Search", IsExpanded = true };
                        {
                            var pictureSearchItem = new SearchInfo() { Name = "Type - \"Picture\"" };
                            pictureSearchItem.Conditions.SearchRegexes.Add(new SearchCondition<SearchRegex>(true, new SearchRegex(@"\.(jpeg|jpg|jfif|gif|png|bmp)$", true)));
                            searchInfo.Children.Add(pictureSearchItem);

                            var movieSearchItem = new SearchInfo() { Name = "Type - \"Movie\"" };
                            movieSearchItem.Conditions.SearchRegexes.Add(new SearchCondition<SearchRegex>(true, new SearchRegex(@"\.(mpeg|mpg|avi|divx|asf|wmv|rm|ogm|mov|flv|vob)$", true)));
                            searchInfo.Children.Add(movieSearchItem);

                            var musicSearchItem = new SearchInfo() { Name = "Type - \"Music\"" };
                            musicSearchItem.Conditions.SearchRegexes.Add(new SearchCondition<SearchRegex>(true, new SearchRegex(@"\.(mp3|wma|m4a|ogg|wav|mid|mod|flac|sid)$", true)));
                            searchInfo.Children.Add(musicSearchItem);

                            var archiveSearchItem = new SearchInfo() { Name = "Type - \"Archive\"" };
                            archiveSearchItem.Conditions.SearchRegexes.Add(new SearchCondition<SearchRegex>(true, new SearchRegex(@"\.(zip|rar|7z|lzh|iso|gz|bz|xz|tar|tgz|tbz|txz)$", true)));
                            searchInfo.Children.Add(archiveSearchItem);

                            var documentSearchItem = new SearchInfo() { Name = "Type - \"Document\"" };
                            documentSearchItem.Conditions.SearchRegexes.Add(new SearchCondition<SearchRegex>(true, new SearchRegex(@"\.(doc|txt|pdf|odt|rtf)$", true)));
                            searchInfo.Children.Add(documentSearchItem);

                            var executableSearchItem = new SearchInfo() { Name = "Type - \"Executable\"" };
                            executableSearchItem.Conditions.SearchRegexes.Add(new SearchCondition<SearchRegex>(true, new SearchRegex(@"\.(exe|jar|sh|bat)$", true)));
                            searchInfo.Children.Add(executableSearchItem);
                        }

                        return searchInfo;
                    });

                    this.TabViewModel.Value = new SearchViewModel(null, model);
                }

                _sortInfo = _settings.Load("SortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Name" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }
        }

        private void Search()
        {
            var info = new SearchInfo() { Name = $"Name - \"{this.SearchInput.Value}\"" };
            info.Conditions.SearchNames.Add(new SearchCondition<string>(true, this.SearchInput.Value));

            this.TabViewModel.Value.Model.Children.Add(info);

            this.SearchInput.Value = "";
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

        private async void TabSelectChanged(TreeViewModelBase viewModel, string propertyName, ListSortDirection direction)
        {
            if (viewModel is SearchViewModel searchViewModel)
            {
                IEnumerable<SearchListViewItemInfo> searchItems = null;
                {
                    var tempViewModels = new List<SearchViewModel>();
                    tempViewModels.AddRange(searchViewModel.GetAncestors().OfType<SearchViewModel>());

                    await Task.Run(() =>
                    {
                        searchItems = _cache_SearchItems.ToList();

                        foreach (var tempViewModel in tempViewModels)
                        {
                            searchItems = Filter(searchItems, tempViewModel.Model);
                        }

                        searchItems = this.Sort(searchItems, propertyName, direction, 100000);
                    });
                }

                if (this.TabSelectedItem.Value != viewModel) return;

                searchViewModel.Model.IsUpdated = false;

                _contents.Clear();
                _contents.AddRange(searchItems);

                searchViewModel.Count.Value = _contents.Count;
            }
        }

        private IEnumerable<SearchListViewItemInfo> Sort(IEnumerable<SearchListViewItemInfo> collection, string propertyName, ListSortDirection direction, int maxCount)
        {
            var list = new List<SearchListViewItemInfo>(collection);

            if (propertyName == "Name")
            {
                list.Sort((x, y) =>
                {
                    int c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (propertyName == "Signature")
            {
                list.Sort((x, y) =>
                {
                    int c = (x.Signature?.ToString() ?? "").CompareTo(y.Signature?.ToString() ?? "");
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
                var searchItems = new List<SearchListViewItemInfo>();

                {
                    var seedInfos = new HashSet<(Seed, Signature)>();

                    var storeMetadatas = new HashSet<Metadata>();
                    var cacheMetadatas = new HashSet<Metadata>();
                    var downloadingMetadatas = new HashSet<Metadata>();
                    var downloadedMetadatas = new HashSet<Metadata>();

                    foreach (var store in _messageManager.GetStores())
                    {
                        var seedHashSet = new HashSet<Seed>();
                        {
                            var boxList = new List<Box>();
                            boxList.AddRange(store.Value.Boxes);

                            for (int i = 0; i < boxList.Count; i++)
                            {
                                boxList.AddRange(boxList[i].Boxes);
                                seedHashSet.UnionWith(boxList[i].Seeds);
                            }
                        }

                        foreach (var seed in seedHashSet)
                        {
                            seedInfos.Add((seed, store.AuthorSignature));
                            storeMetadatas.Add(seed.Metadata);
                        }
                    }

                    {
                        var signature = SettingsManager.Instance.AccountInfo.DigitalSignature.GetSignature();

                        foreach (var seed in _serviceManager.GetContentInformations()
                            .Select(n => new Seed(Path.GetFileName(n.GetValue<string>("Path")), n.GetValue<long>("Length"), n.GetValue<DateTime>("CreationTime"), n.GetValue<Metadata>("Metadata"))).ToArray())
                        {
                            seedInfos.Add((seed, signature));
                            cacheMetadatas.Add(seed.Metadata);
                        }
                    }

                    downloadingMetadatas.UnionWith(_serviceManager.GetDownloadInformations().Select(n => n.GetValue<Metadata>("Metadata")));

                    {
                        var downloadedSeeds = SettingsManager.Instance.DownloadedSeeds.ToArray();

                        foreach (var seed in SettingsManager.Instance.DownloadedSeeds.ToArray())
                        {
                            if (!storeMetadatas.Contains(seed.Metadata) && !cacheMetadatas.Contains(seed.Metadata))
                            {
                                seedInfos.Add((seed, null));
                            }

                            downloadedMetadatas.Add(seed.Metadata);
                        }
                    }

                    foreach (var (seed, signature) in seedInfos)
                    {
                        var viewModel = new SearchListViewItemInfo();
                        viewModel.Icon = IconUtils.GetImage(seed.Name);
                        viewModel.Name = seed.Name;
                        viewModel.Signature = signature;
                        viewModel.Length = seed.Length;
                        viewModel.CreationTime = seed.CreationTime;

                        SearchState state = 0;
                        if (storeMetadatas.Contains(seed.Metadata)) state |= SearchState.Store;
                        if (cacheMetadatas.Contains(seed.Metadata)) state |= SearchState.Cache;
                        if (downloadingMetadatas.Contains(seed.Metadata)) state |= SearchState.Downloading;
                        if (downloadedMetadatas.Contains(seed.Metadata)) state |= SearchState.Downloaded;

                        viewModel.State = state;
                        viewModel.Model = seed;

                        searchItems.Add(viewModel);
                    }
                }

                lock (_cache_SearchItems.LockObject)
                {
                    _cache_SearchItems.Clear();
                    _cache_SearchItems.AddRange(searchItems);
                }

                this.SetCount(this.TabViewModel.Value, searchItems, token);

                if (token.WaitHandle.WaitOne(1000 * 20)) return;
            }
        }

        private void SetCount(SearchViewModel viewModel, IEnumerable<SearchListViewItemInfo> items, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            var tempList = Filter(items, viewModel.Model).ToList();

            App.Current.Dispatcher.InvokeAsync(() =>
            {
                if (token.IsCancellationRequested) return;

                try
                {
                    if (viewModel.Count.Value != 0 && viewModel.Count.Value < tempList.Count)
                    {
                        viewModel.Model.IsUpdated = true;
                    }

                    viewModel.Count.Value = tempList.Count;
                }
                catch (Exception)
                {

                }
            });

            {
                var searchViewModels = new List<SearchViewModel>();

                try
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        searchViewModels.AddRange(viewModel.Children);
                    }, DispatcherPriority.Background, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                foreach (var searchViewModel in searchViewModels)
                {
                    this.SetCount(searchViewModel, tempList, token);
                }
            }
        }

        private static IEnumerable<SearchListViewItemInfo> Filter(IEnumerable<SearchListViewItemInfo> items, SearchInfo searchInfo)
        {
            var comparer = EqualityComparer<Metadata>.Default;

            var list = items.Where(item =>
            {
                {
                    foreach (var searchContains in searchInfo.Conditions.SearchStates)
                    {
                        if (!searchContains.IsContains)
                        {
                            if (item.State.HasFlag(searchContains.Value)) return false;
                        }
                    }

                    foreach (var searchContains in searchInfo.Conditions.SearchLengthRanges)
                    {
                        if (!searchContains.IsContains)
                        {
                            if (searchContains.Value.Verify(item.Length)) return false;
                        }
                    }

                    foreach (var searchContains in searchInfo.Conditions.SearchCreationTimeRanges)
                    {
                        if (!searchContains.IsContains)
                        {
                            if (searchContains.Value.Verify(item.CreationTime)) return false;
                        }
                    }

                    foreach (var searchContains in searchInfo.Conditions.SearchSignatures)
                    {
                        if (!searchContains.IsContains)
                        {
                            if (searchContains.Value == item.Signature) return false;
                        }
                    }

                    foreach (var searchContains in searchInfo.Conditions.SearchNames)
                    {
                        if (!searchContains.IsContains)
                        {
                            if (searchContains.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                .All(n => item.Name.Contains(n, StringComparison.OrdinalIgnoreCase))) return false;
                        }
                    }

                    foreach (var searchContains in searchInfo.Conditions.SearchRegexes)
                    {
                        if (!searchContains.IsContains)
                        {
                            if (searchContains.Value.IsMatch(item.Name)) return false;
                        }
                    }
                }

                {
                    bool flag = false;

                    foreach (var searchContains in searchInfo.Conditions.SearchStates)
                    {
                        if (searchContains.IsContains)
                        {
                            if (item.State.HasFlag(searchContains.Value)) return true;
                            flag = true;
                        }
                    }

                    foreach (var searchContains in searchInfo.Conditions.SearchLengthRanges)
                    {
                        if (searchContains.IsContains)
                        {
                            if (searchContains.Value.Verify(item.Length)) return true;
                            flag = true;
                        }
                    }

                    foreach (var searchContains in searchInfo.Conditions.SearchCreationTimeRanges)
                    {
                        if (searchContains.IsContains)
                        {
                            if (searchContains.Value.Verify(item.CreationTime)) return true;
                            flag = true;
                        }
                    }

                    foreach (var searchContains in searchInfo.Conditions.SearchSignatures)
                    {
                        if (searchContains.IsContains)
                        {
                            if (searchContains.Value == item.Signature) return true;
                            flag = true;
                        }
                    }

                    foreach (var searchContains in searchInfo.Conditions.SearchNames)
                    {
                        if (searchContains.IsContains)
                        {
                            if (searchContains.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                .All(n => item.Name.Contains(n, StringComparison.OrdinalIgnoreCase))) return true;
                            flag = true;
                        }
                    }

                    foreach (var searchContains in searchInfo.Conditions.SearchRegexes)
                    {
                        if (searchContains.IsContains)
                        {
                            if (searchContains.Value.IsMatch(item.Name)) return true;
                            flag = true;
                        }
                    }

                    return !flag;
                }
            }).ToList();

            return list;
        }

        private void TabNewSearch()
        {
            if (this.TabSelectedItem.Value is SearchViewModel searchViewModel)
            {
                var viewModel = new SearchInfoEditWindowViewModel(new SearchInfo() { Name = "default" });
                viewModel.Callback += (info) =>
                {
                    searchViewModel.Model.Children.Add(info);
                };

                Messenger.Instance.GetEvent<SearchInfoEditWindowShowEvent>()
                    .Publish(viewModel);
            }
        }

        private void TabEdit()
        {
            if (this.TabSelectedItem.Value is SearchViewModel searchViewModel)
            {
                var viewModel = new SearchInfoEditWindowViewModel(searchViewModel.Model);

                Messenger.Instance.GetEvent<SearchInfoEditWindowShowEvent>()
                    .Publish(viewModel);
            }
        }

        private void TabDelete()
        {
            var viewModel = new ConfirmWindowViewModel(ConfirmWindowType.Delete);
            viewModel.Callback += () =>
            {
                if (this.TabSelectedItem.Value is SearchViewModel searchViewModel)
                {
                    if (searchViewModel.Parent == null) return;
                    searchViewModel.Parent.TryRemove(searchViewModel);
                }
            };

            Messenger.Instance.GetEvent<ConfirmWindowShowEvent>()
                .Publish(viewModel);
        }

        private void TabCut()
        {
            if (this.TabSelectedItem.Value is SearchViewModel searchViewModel)
            {
                if (searchViewModel.Parent == null) return;
                Clipboard.SetSearchInfos(new SearchInfo[] { searchViewModel.Model });
                searchViewModel.Parent.TryRemove(searchViewModel);
            }
        }

        private void TabCopy()
        {
            if (this.TabSelectedItem.Value is SearchViewModel searchViewModel)
            {
                Clipboard.SetSearchInfos(new SearchInfo[] { searchViewModel.Model });
            }
        }

        private void TabPaste()
        {
            if (this.TabSelectedItem.Value is SearchViewModel searchViewModel)
            {
                searchViewModel.Model.Children.AddRange(Clipboard.GetSearchInfos());
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

        private void Copy()
        {
            Clipboard.SetSeeds(this.SelectedItems.OfType<SearchListViewItemInfo>()
                .Select(n => n.Model).ToArray());
        }

        private void Download()
        {
            foreach (var seed in this.SelectedItems.OfType<SearchListViewItemInfo>()
                .Select(n => n.Model).ToArray())
            {
                SettingsManager.Instance.DownloadItemInfos.Add(new DownloadItemInfo(seed, seed.Name));
            }
        }

        private void AdvancedCopy(string type)
        {
            var selectItems = this.SelectedItems.OfType<SearchListViewItemInfo>().ToArray();

            if (type == "Name")
            {
                Clipboard.SetText(string.Join(Environment.NewLine, new HashSet<string>(selectItems.Select(n => n.Name))));
            }
            else if (type == "Signature")
            {
                Clipboard.SetText(string.Join(Environment.NewLine, new HashSet<Signature>(selectItems.Select(n => n.Signature))));
            }
        }

        private void RemoveCacheItem()
        {
            var hashSet = new HashSet<Metadata>(this.SelectedItems.OfType<SearchListViewItemInfo>()
                .Where(n => n.State.HasFlag(SearchState.Cache))
                .Select(n => n.Model.Metadata).ToArray());
            if (hashSet.Count == 0) return;

            var viewModel = new ConfirmWindowViewModel(ConfirmWindowType.Delete);
            viewModel.Callback += () =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        foreach (var (metadata, path) in _serviceManager.GetContentInformations()
                            .Select(n => (n.GetValue<Metadata>("Metadata"), n.GetValue<string>("Path"))).ToArray())
                        {
                            if (!hashSet.Contains(metadata)) continue;
                            _serviceManager.RemoveContent(path);
                        }
                    }
                    catch (Exception)
                    {

                    }
                });
            };

            Messenger.Instance.GetEvent<ConfirmWindowShowEvent>()
                .Publish(viewModel);
        }

        private void RemoveDownloadingItem()
        {
            var hashSet = new HashSet<Seed>(this.SelectedItems.OfType<SearchListViewItemInfo>()
                .Where(n => n.State.HasFlag(SearchState.Downloading))
                .Select(n => n.Model).ToArray());
            if (hashSet.Count == 0) return;

            var viewModel = new ConfirmWindowViewModel(ConfirmWindowType.Delete);
            viewModel.Callback += () =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        foreach (var item in SettingsManager.Instance.DownloadItemInfos.ToArray())
                        {
                            if (!hashSet.Contains(item.Seed)) continue;
                            SettingsManager.Instance.DownloadItemInfos.Remove(item);
                        }
                    }
                    catch (Exception)
                    {

                    }
                });
            };

            Messenger.Instance.GetEvent<ConfirmWindowShowEvent>()
                .Publish(viewModel);
        }

        private void RemoveDownloadedItem()
        {
            var hashSet = new HashSet<Metadata>(this.SelectedItems.OfType<SearchListViewItemInfo>()
                .Where(n => n.State.HasFlag(SearchState.Downloaded))
                .Select(n => n.Model.Metadata).ToArray());
            if (hashSet.Count == 0) return;

            var viewModel = new ConfirmWindowViewModel(ConfirmWindowType.Delete);
            viewModel.Callback += () =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        foreach (var seed in SettingsManager.Instance.DownloadedSeeds.ToArray())
                        {
                            if (!hashSet.Contains(seed.Metadata)) continue;
                            SettingsManager.Instance.DownloadedSeeds.Remove(seed);
                        }
                    }
                    catch (Exception)
                    {

                    }
                });
            };

            Messenger.Instance.GetEvent<ConfirmWindowShowEvent>()
                .Publish(viewModel);
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save("SearchInfo", this.TabViewModel.Value.Model);
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
