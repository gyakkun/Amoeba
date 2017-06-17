using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using Omnius.Utilities;
using Omnius.Security;
using Prism.Events;
using Prism.Interactivity.InteractionRequest;
using Omnius.Collections;

namespace Amoeba.Interface
{
    class StoreSearchControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TrustManager _trustManager;

        private TaskManager _watchTaskManager;

        private Settings _settings;

        private LockedList<SearchItemViewModel> _cache_SearchItems = new LockedList<SearchItemViewModel>();

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

        public ICollectionView ContentsView => CollectionViewSource.GetDefaultView(_contents);
        private ObservableCollection<SearchItemViewModel> _contents = new ObservableCollection<SearchItemViewModel>();
        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public ReactiveCommand CopyCommand { get; private set; }
        public ReactiveCommand DownloadCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public StoreSearchControlViewModel(ServiceManager serviceManager, TrustManager trustManager)
        {
            _serviceManager = serviceManager;
            _trustManager = trustManager;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();

            this.DragAcceptDescription = new DragAcceptDescription() { Effects = DragDropEffects.Move, Format = "Store" };
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
                this.TabViewModel = new ReactiveProperty<SearchViewModel>().AddTo(_disposable);

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.TabClickCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabClickCommand.Subscribe(() => this.TabSelectChanged(this.TabSelectedItem.Value)).AddTo(_disposable);

                this.TabNewSearchCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabNewSearchCommand.Subscribe(() => this.TabNewSearch()).AddTo(_disposable);

                this.TabEditCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabEditCommand.Subscribe(() => this.TabEdit()).AddTo(_disposable);

                this.TabDeleteCommand = this.TabSelectedItem.Select(n => n != this.TabViewModel.Value).ToReactiveCommand().AddTo(_disposable);
                this.TabDeleteCommand.Subscribe(() => this.TabDelete()).AddTo(_disposable);

                this.TabCutCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabCutCommand.Subscribe(() => this.TabCut()).AddTo(_disposable);

                this.TabCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabCopyCommand.Subscribe(() => this.TabCopy()).AddTo(_disposable);

                this.TabPasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabPasteCommand.Subscribe(() => this.TabPaste()).AddTo(_disposable);

                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.CopyCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);

                this.DownloadCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.DownloadCommand.Subscribe(() => this.Download()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(StoreSearchControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                {
                    var model = _settings.Load("SearchInfo", () =>
                    {
                        var searchInfo = new SearchInfo() { Name = "Search", IsExpanded = true };
                        {
                            var pictureSearchItem = new SearchInfo() { Name = "Type - \"Picture\"" };
                            pictureSearchItem.Condition.SearchRegexes.Add(new SearchContains<SearchRegex>(true, new SearchRegex(@"\.(jpeg|jpg|jfif|gif|png|bmp)$", true)));
                            searchInfo.Children.Add(pictureSearchItem);

                            var movieSearchItem = new SearchInfo() { Name = "Type - \"Movie\"" };
                            movieSearchItem.Condition.SearchRegexes.Add(new SearchContains<SearchRegex>(true, new SearchRegex(@"\.(mpeg|mpg|avi|divx|asf|wmv|rm|ogm|mov|flv|vob)$", true)));
                            searchInfo.Children.Add(movieSearchItem);

                            var musicSearchItem = new SearchInfo() { Name = "Type - \"Music\"" };
                            musicSearchItem.Condition.SearchRegexes.Add(new SearchContains<SearchRegex>(true, new SearchRegex(@"\.(mp3|wma|m4a|ogg|wav|mid|mod|flac|sid)$", true)));
                            searchInfo.Children.Add(musicSearchItem);

                            var archiveSearchItem = new SearchInfo() { Name = "Type - \"Archive\"" };
                            archiveSearchItem.Condition.SearchRegexes.Add(new SearchContains<SearchRegex>(true, new SearchRegex(@"\.(zip|rar|7z|lzh|iso|gz|bz|xz|tar|tgz|tbz|txz)$", true)));
                            searchInfo.Children.Add(archiveSearchItem);

                            var documentSearchItem = new SearchInfo() { Name = "Type - \"Document\"" };
                            documentSearchItem.Condition.SearchRegexes.Add(new SearchContains<SearchRegex>(true, new SearchRegex(@"\.(doc|txt|pdf|odt|rtf)$", true)));
                            searchInfo.Children.Add(documentSearchItem);

                            var executableSearchItem = new SearchInfo() { Name = "Type - \"Executable\"" };
                            executableSearchItem.Condition.SearchRegexes.Add(new SearchContains<SearchRegex>(true, new SearchRegex(@"\.(exe|jar|sh|bat)$", true)));
                            searchInfo.Children.Add(executableSearchItem);
                        }

                        return searchInfo;
                    });

                    this.TabViewModel.Value = new SearchViewModel(null, model);
                }

                _sortInfo = _settings.Load("SortInfo", () => new ListSortInfo());
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += () => this.Save();
            }

            {
                this.Sort(null);
            }
        }

        private async void TabSelectChanged(TreeViewModelBase viewModel)
        {
            if (viewModel is SearchViewModel searchViewModel)
            {
                IEnumerable<SearchItemViewModel> searchItems = null;
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
                    });
                }

                if (this.TabSelectedItem.Value != viewModel) return;

                searchViewModel.Model.IsUpdated = false;

                _contents.Clear();
                _contents.AddRange(searchItems);
            }
        }

        private void WatchThread(CancellationToken token)
        {
            for (;;)
            {
                var searchItems = new List<SearchItemViewModel>();

                {
                    var downloadMetadatas = new HashSet<Metadata>();
                    downloadMetadatas.UnionWith(_serviceManager.GetDownloadInformations().Select(n => n.GetValue<Metadata>("Metadata")));

                    var cacheMetadatas = new HashSet<Metadata>();
                    cacheMetadatas.UnionWith(_serviceManager.GetContentInformations().Select(n => n.GetValue<Metadata>("Metadata")));

                    foreach (var store in _trustManager.GetStores())
                    {
                        var seedList = new List<Seed>();
                        {
                            var boxList = new List<Box>();
                            boxList.AddRange(store.Value.Boxes);

                            for (int i = 0; i < boxList.Count; i++)
                            {
                                boxList.AddRange(boxList[i].Boxes);
                                seedList.AddRange(boxList[i].Seeds);
                            }
                        }

                        foreach (var seed in seedList)
                        {
                            var viewModel = new SearchItemViewModel();
                            viewModel.Icon = IconUtils.GetImage(seed.Name);
                            viewModel.Name = seed.Name;
                            viewModel.Signature = store.AuthorSignature;
                            viewModel.Length = seed.Length;
                            viewModel.CreationTime = seed.CreationTime;

                            SearchState state = 0;
                            if (downloadMetadatas.Contains(seed.Metadata)) state |= SearchState.Download;
                            if (cacheMetadatas.Contains(seed.Metadata)) state |= SearchState.Cache;

                            viewModel.State = state;

                            searchItems.Add(viewModel);
                        }
                    }
                }

                lock (_cache_SearchItems.LockObject)
                {
                    _cache_SearchItems.Clear();
                    _cache_SearchItems.AddRange(searchItems);
                }

                this.SetCount(this.TabViewModel.Value, searchItems);

                if (token.WaitHandle.WaitOne(1000 * 20)) return;
            }
        }

        private void SetCount(SearchViewModel viewModel, IEnumerable<SearchItemViewModel> items)
        {
            var tempList = Filter(items, viewModel.Model).ToList();

            App.Current.Dispatcher.InvokeAsync(() =>
            {
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

                App.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        searchViewModels.AddRange(viewModel.Children);
                    }
                    catch (Exception)
                    {

                    }
                });

                foreach (var searchViewModel in searchViewModels)
                {
                    this.SetCount(searchViewModel, tempList);
                }
            }
        }

        private static IEnumerable<SearchItemViewModel> Filter(IEnumerable<SearchItemViewModel> items, SearchInfo searchInfo)
        {
            var comparer = EqualityComparer<Metadata>.Default;

            var list = items.Where(item =>
            {
                {
                    foreach (var searchContains in searchInfo.Condition.SearchStates)
                    {
                        if (!searchContains.Contains)
                        {
                            if (item.State.HasFlag(searchContains.Value)) return false;
                        }
                    }

                    foreach (var searchContains in searchInfo.Condition.SearchLengthRanges)
                    {
                        if (!searchContains.Contains)
                        {
                            if (searchContains.Value.Verify(item.Length)) return false;
                        }
                    }

                    foreach (var searchContains in searchInfo.Condition.SearchCreationTimeRanges)
                    {
                        if (!searchContains.Contains)
                        {
                            if (searchContains.Value.Verify(item.CreationTime)) return false;
                        }
                    }

                    foreach (var searchContains in searchInfo.Condition.SearchSignatures)
                    {
                        if (!searchContains.Contains)
                        {
                            if (searchContains.Value == item.Signature) return false;
                        }
                    }

                    foreach (var searchContains in searchInfo.Condition.SearchNames)
                    {
                        if (!searchContains.Contains)
                        {
                            if (searchContains.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                .All(n => item.Name.Contains(n, StringComparison.OrdinalIgnoreCase))) return false;
                        }
                    }

                    foreach (var searchContains in searchInfo.Condition.SearchRegexes)
                    {
                        if (!searchContains.Contains)
                        {
                            if (searchContains.Value.IsMatch(item.Name)) return false;
                        }
                    }

                    foreach (var searchContains in searchInfo.Condition.SearchMetadatas)
                    {
                        if (!searchContains.Contains)
                        {
                            if (comparer.Equals(item.Model.Metadata, searchContains.Value)) return false;
                        }
                    }
                }

                {
                    bool flag = false;

                    foreach (var searchContains in searchInfo.Condition.SearchStates)
                    {
                        if (searchContains.Contains)
                        {
                            if (item.State.HasFlag(searchContains.Value)) return true;
                            flag = true;
                        }
                    }

                    foreach (var searchContains in searchInfo.Condition.SearchLengthRanges)
                    {
                        if (searchContains.Contains)
                        {
                            if (searchContains.Value.Verify(item.Length)) return true;
                            flag = true;
                        }
                    }

                    foreach (var searchContains in searchInfo.Condition.SearchCreationTimeRanges)
                    {
                        if (searchContains.Contains)
                        {
                            if (searchContains.Value.Verify(item.CreationTime)) return true;
                            flag = true;
                        }
                    }

                    foreach (var searchContains in searchInfo.Condition.SearchSignatures)
                    {
                        if (searchContains.Contains)
                        {
                            if (searchContains.Value == item.Signature) return true;
                            flag = true;
                        }
                    }

                    foreach (var searchContains in searchInfo.Condition.SearchNames)
                    {
                        if (searchContains.Contains)
                        {
                            if (searchContains.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                .All(n => item.Name.Contains(n, StringComparison.OrdinalIgnoreCase))) return true;
                            flag = true;
                        }
                    }

                    foreach (var searchContains in searchInfo.Condition.SearchRegexes)
                    {
                        if (searchContains.Contains)
                        {
                            if (searchContains.Value.IsMatch(item.Name)) return true;
                            flag = true;
                        }
                    }

                    foreach (var searchContains in searchInfo.Condition.SearchMetadatas)
                    {
                        if (searchContains.Contains)
                        {
                            if (comparer.Equals(item.Model.Metadata, searchContains.Value)) return true;
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

            }
        }

        private void TabEdit()
        {
            if (this.TabSelectedItem.Value is SearchViewModel searchViewModel)
            {

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
            switch (propertyName)
            {
                case "Name":
                    this.ContentsView.SortDescriptions.Add(new SortDescription("Name", direction));
                    break;
                case "Signature":
                    {
                        var view = ((ListCollectionView)this.ContentsView);
                        view.CustomSort = new CustomSortComparer(direction, (x, y) => ((Signature)x).Name.CompareTo(((Signature)y).Name));
                        view.Refresh();
                    }
                    break;

                case "Length":
                    this.ContentsView.SortDescriptions.Add(new SortDescription("Length", direction));
                    break;
                case "CreationTime":
                    this.ContentsView.SortDescriptions.Add(new SortDescription("CreationTime", direction));
                    break;
                case "State":
                    this.ContentsView.SortDescriptions.Add(new SortDescription("State", direction));
                    break;
            }
        }

        private void Copy()
        {
            Clipboard.SetSeeds(this.SelectedItems.OfType<SearchItemViewModel>()
                .Select(n => n.Model).ToArray());
        }

        private void Download()
        {
            foreach (var seed in this.SelectedItems.OfType<SearchItemViewModel>()
                .Select(n => n.Model).ToArray())
            {
                SettingsManager.Instance.DownloadItemInfos.Add(new DownloadItemInfo(seed, seed.Name));
            }
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
                _watchTaskManager.Stop();
                _watchTaskManager.Dispose();

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
