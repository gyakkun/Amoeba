using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml;
using Amoeba.Properties;
using Library;
using Library.Collections;
using Library.Io;
using Library.Net.Amoeba;

namespace Amoeba.Windows
{
    /// <summary>
    /// SearchControl.xaml の相互作用ロジック
    /// </summary>
    partial class SearchControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;

        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private AutoResetEvent _updateEvent = new AutoResetEvent(false);
        private volatile bool _refreshing = false;
        private AutoResetEvent _cacheUpdateEvent = new AutoResetEvent(false);
        private volatile bool _autoUpdate;

        private SearchTreeViewModel _treeViewModel;
        private ObservableCollectionEx<SearchListViewModel> _listViewModelCollection = new ObservableCollectionEx<SearchListViewModel>();
        private LockedList<SearchListViewModel> _searchingCache = new LockedList<SearchListViewModel>();

        private Thread _cacheThread;
        private Thread _searchThread;

        private const HashAlgorithm _hashAlgorithm = HashAlgorithm.Sha256;

        public SearchControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            _treeViewModel = new SearchTreeViewModel(null, Settings.Instance.SearchControl_SearchTreeItem);

            InitializeComponent();

            _treeView.Items.Add(_treeViewModel);

            _listView.ItemsSource = _listViewModelCollection;

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                if (e.OriginalSource != _mainWindow._tabControl) return;

                if (_mainWindow.SelectedTab == MainWindowTabType.Search)
                {
                    this.Update_Title();
                    _cacheUpdateEvent.Set();
                }
            };

            _cacheThread = new Thread(this.Cache);
            _cacheThread.Priority = ThreadPriority.Highest;
            _cacheThread.IsBackground = true;
            _cacheThread.Name = "SearchControl_CacheThread";
            _cacheThread.Start();

            _searchThread = new Thread(this.Search);
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "SearchControl_SearchThread";
            _searchThread.Start();

            _searchRowDefinition.Height = new GridLength(0);

            LanguagesManager.UsingLanguageChangedEvent += this.LanguagesManager_UsingLanguageChangedEvent;

            this.Update_Cache();
        }

        private void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {
            _listView.Items.Refresh();
        }

        private void Cache()
        {
            try
            {
                for (;;)
                {
                    var sw = new Stopwatch();
                    sw.Start();

                    var seedsDictionary = new ConcurrentDictionary<Seed, SeedsState>();

                    {
                        foreach (var seed in _amoebaManager.CacheSeeds)
                        {
                            var item = seedsDictionary.GetOrAdd(seed, _ => new SeedsState());

                            item.State |= SearchState.Cache;
                        }

                        {
                            var seedList = new List<Seed>();

                            {
                                var boxList = new List<Box>();
                                boxList.Add(Settings.Instance.LibraryControl_Box);

                                {
                                    var storeCategorizeTreeItems = new List<StoreCategorizeTreeItem>();
                                    storeCategorizeTreeItems.Add(Settings.Instance.StoreUploadControl_StoreCategorizeTreeItem);
                                    storeCategorizeTreeItems.Add(Settings.Instance.StoreDownloadControl_StoreCategorizeTreeItem);

                                    for (int i = 0; i < storeCategorizeTreeItems.Count; i++)
                                    {
                                        storeCategorizeTreeItems.AddRange(storeCategorizeTreeItems[i].Children);

                                        foreach (var storeTreeItem in storeCategorizeTreeItems[i].StoreTreeItems)
                                        {
                                            boxList.AddRange(storeTreeItem.Boxes);
                                        }
                                    }
                                }

                                for (int i = 0; i < boxList.Count; i++)
                                {
                                    boxList.AddRange(boxList[i].Boxes);
                                    seedList.AddRange(boxList[i].Seeds);
                                }
                            }

                            foreach (var seed in seedList)
                            {
                                var item = seedsDictionary.GetOrAdd(seed, _ => new SeedsState());

                                item.State |= SearchState.Store;
                            }
                        }

                        {
                            var seedList = new List<Seed>();

                            {
                                var boxList = new List<Box>();

                                {
                                    var searchSignatures = new HashSet<string>();

                                    foreach (var linkItem in Settings.Instance.Cache_LinkItems.Values.ToArray())
                                    {
                                        searchSignatures.Add(linkItem.Signature);
                                        searchSignatures.UnionWith(linkItem.TrustSignatures);
                                    }

                                    foreach (var signature in searchSignatures)
                                    {
                                        var store = _amoebaManager.GetStore(signature);
                                        if (store == null) continue;

                                        boxList.AddRange(store.Boxes);
                                    }
                                }

                                for (int i = 0; i < boxList.Count; i++)
                                {
                                    boxList.AddRange(boxList[i].Boxes);
                                    seedList.AddRange(boxList[i].Seeds);
                                }
                            }

                            foreach (var seed in seedList)
                            {
                                var item = seedsDictionary.GetOrAdd(seed, _ => new SeedsState());

                                item.State |= SearchState.Link;
                            }
                        }

                        foreach (var information in _amoebaManager.UploadingInformation)
                        {
                            if (information.Contains("Seed") && ((UploadState)information["State"]) != UploadState.Completed)
                            {
                                var seed = (Seed)information["Seed"];

                                var item = seedsDictionary.GetOrAdd(seed, _ => new SeedsState());

                                {
                                    item.State |= SearchState.Uploading;
                                    item.Seeds.Add(seed);
                                    item.UploadIds.Add((int)information["Id"]);
                                }
                            }
                        }

                        foreach (var information in _amoebaManager.DownloadingInformation)
                        {
                            if (information.Contains("Seed") && ((DownloadState)information["State"]) != DownloadState.Completed)
                            {
                                var seed = (Seed)information["Seed"];

                                var item = seedsDictionary.GetOrAdd(seed, _ => new SeedsState());

                                {
                                    item.State |= SearchState.Downloading;
                                    item.Seeds.Add(seed);
                                    item.DownloadIds.Add((int)information["Id"]);
                                }
                            }
                        }

                        foreach (var seed in _amoebaManager.UploadedSeeds)
                        {
                            var item = seedsDictionary.GetOrAdd(seed, _ => new SeedsState());

                            {
                                item.State |= SearchState.Uploaded;
                                item.Seeds.Add(seed);
                            }
                        }

                        foreach (var seed in _amoebaManager.DownloadedSeeds)
                        {
                            var item = seedsDictionary.GetOrAdd(seed, _ => new SeedsState());

                            {
                                item.State |= SearchState.Downloaded;
                                item.Seeds.Add(seed);
                            }
                        }
                    }

                    lock (_searchingCache.ThisLock)
                    {
                        _searchingCache.Clear();

                        foreach (var pair in seedsDictionary)
                        {
                            var seed = pair.Key;
                            var value = pair.Value;

                            var searchItem = new SearchListViewModel();

                            lock (seed.ThisLock)
                            {
                                searchItem.Name = seed.Name;
                                if (seed.Certificate != null) searchItem.Signature = seed.Certificate.ToString();
                                searchItem.Length = seed.Length;
                                searchItem.Keywords = string.Join(", ", seed.Keywords.Where(n => !string.IsNullOrWhiteSpace(n)));
                                searchItem.CreationTime = seed.CreationTime;
                                searchItem.State = value.State;
                                searchItem.Value = seed;

                                searchItem.Seeds = value.Seeds;
                                searchItem.UploadIds = value.UploadIds;
                                searchItem.DownloadIds = value.DownloadIds;
                                //if (seed.Key != null && seed.Key.Hash != null) searchItem.Id = NetworkConverter.ToHexString(seed.Key.Hash);
                            }

                            _searchingCache.Add(searchItem);
                        }
                    }

                    {
                        this.SetCount(_treeViewModel, _searchingCache.ToList());
                    }

                    sw.Stop();
                    Debug.WriteLine("SearchControl_Cache {0}", sw.ElapsedMilliseconds);

                    if (_autoUpdate)
                    {
                        _autoUpdate = false;

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            this.Update();
                        }));
                    }

                    _cacheUpdateEvent.WaitOne(1000 * 60 * 1);
                }
            }
            catch (Exception)
            {

            }
        }

        private void Search()
        {
            try
            {
                for (;;)
                {
                    _updateEvent.WaitOne();

                    try
                    {
                        _refreshing = true;

                        SearchTreeViewModel tempTreeViewModel = null;

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            tempTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
                        }));

                        if (tempTreeViewModel == null) continue;

                        var newList = new List<SearchListViewModel>(_searchingCache);

                        var searchTreeViewModels = new List<SearchTreeViewModel>();

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            searchTreeViewModels.AddRange(tempTreeViewModel.GetAncestors().OfType<SearchTreeViewModel>());
                        }));

                        foreach (var searchTreeViewModel in searchTreeViewModels)
                        {
                            {
                                var tempList = SearchControl.Filter(newList, searchTreeViewModel.Value.SearchItem);
                                newList.Clear();
                                newList.AddRange(tempList);
                            }

                            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                searchTreeViewModel.Count = newList.Count;
                                searchTreeViewModel.Update();
                            }));
                        }

                        {
                            string[] words = null;

                            {
                                string searchText = null;

                                this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                {
                                    searchText = _searchTextBox.Text;
                                }));

                                if (!string.IsNullOrWhiteSpace(searchText))
                                {
                                    words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                                }
                            }

                            var tempList = new List<SearchListViewModel>();

                            foreach (var item in newList)
                            {
                                if (words != null)
                                {
                                    var text = (item.Name ?? "").ToLower();
                                    if (!words.All(n => text.Contains(n))) continue;
                                }

                                tempList.Add(item);
                            }

                            newList.Clear();
                            newList.AddRange(tempList);
                        }

                        var sortList = this.Sort(newList, 100000);

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            if (tempTreeViewModel != _treeView.SelectedItem) return;

                            _listViewModelCollection.Clear();
                            _listViewModelCollection.AddRange(sortList);

                            this.Update_Title();
                        }));
                    }
                    finally
                    {
                        _refreshing = false;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void SetCount(SearchTreeViewModel targetTreeViewModel, IEnumerable<SearchListViewModel> items)
        {
            var tempList = SearchControl.Filter(items, targetTreeViewModel.Value.SearchItem).ToList();

            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                targetTreeViewModel.Count = tempList.Count;
                targetTreeViewModel.Update();
            }));

            {
                var searchTreeViewModels = new List<SearchTreeViewModel>();

                this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    searchTreeViewModels.AddRange(targetTreeViewModel.Children.OfType<SearchTreeViewModel>());
                }));

                foreach (var searchTreeViewModel in searchTreeViewModels)
                {
                    this.SetCount(searchTreeViewModel, tempList);
                }
            }
        }

        private static IEnumerable<SearchListViewModel> Filter(IEnumerable<SearchListViewModel> items, SearchItem searchItem)
        {
            var comparer = EqualityComparer<Seed>.Default;

            lock (searchItem.ThisLock)
            {
                var list = items.Where(item =>
                {
                    {
                        foreach (var searchContains in searchItem.SearchStateCollection)
                        {
                            if (!searchContains.Contains)
                            {
                                if (item.State.HasFlag(searchContains.Value)) return false;
                            }
                        }

                        foreach (var searchContains in searchItem.SearchLengthRangeCollection)
                        {
                            if (!searchContains.Contains)
                            {
                                if (searchContains.Value.Verify(item.Value.Length)) return false;
                            }
                        }

                        foreach (var searchContains in searchItem.SearchCreationTimeRangeCollection)
                        {
                            if (!searchContains.Contains)
                            {
                                if (searchContains.Value.Verify(item.Value.CreationTime)) return false;
                            }
                        }

                        foreach (var searchContains in searchItem.SearchKeywordCollection)
                        {
                            if (!searchContains.Contains)
                            {
                                if (item.Value.Keywords.Any(n => !string.IsNullOrWhiteSpace(n)
                                    && n.Contains(searchContains.Value, StringComparison.OrdinalIgnoreCase))) return false;
                            }
                        }

                        foreach (var searchContains in searchItem.SearchSignatureCollection)
                        {
                            if (!searchContains.Contains)
                            {
                                if (item.Signature == null)
                                {
                                    if (searchContains.Value.IsMatch("Anonymous")) return false;
                                }
                                else
                                {
                                    if (searchContains.Value.IsMatch(item.Signature)) return false;
                                }
                            }
                        }

                        foreach (var searchContains in searchItem.SearchNameCollection)
                        {
                            if (!searchContains.Contains)
                            {
                                if (searchContains.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                    .All(n => item.Value.Name.Contains(n, StringComparison.OrdinalIgnoreCase))) return false;
                            }
                        }

                        foreach (var searchContains in searchItem.SearchNameRegexCollection)
                        {
                            if (!searchContains.Contains)
                            {
                                if (searchContains.Value.IsMatch(item.Value.Name)) return false;
                            }
                        }

                        foreach (var searchContains in searchItem.SearchSeedCollection)
                        {
                            if (!searchContains.Contains)
                            {
                                if (comparer.Equals(item.Value, searchContains.Value)) return false;
                            }
                        }
                    }

                    {
                        bool flag = false;

                        foreach (var searchContains in searchItem.SearchStateCollection)
                        {
                            if (searchContains.Contains)
                            {
                                if (item.State.HasFlag(searchContains.Value)) return true;
                                flag = true;
                            }
                        }

                        foreach (var searchContains in searchItem.SearchLengthRangeCollection)
                        {
                            if (searchContains.Contains)
                            {
                                if (searchContains.Value.Verify(item.Value.Length)) return true;
                                flag = true;
                            }
                        }

                        foreach (var searchContains in searchItem.SearchCreationTimeRangeCollection)
                        {
                            if (searchContains.Contains)
                            {
                                if (searchContains.Value.Verify(item.Value.CreationTime)) return true;
                                flag = true;
                            }
                        }

                        foreach (var searchContains in searchItem.SearchKeywordCollection)
                        {
                            if (searchContains.Contains)
                            {
                                if (item.Value.Keywords.Any(n => !string.IsNullOrWhiteSpace(n)
                                    && n.Contains(searchContains.Value, StringComparison.OrdinalIgnoreCase))) return true;
                                flag = true;
                            }
                        }

                        foreach (var searchContains in searchItem.SearchSignatureCollection)
                        {
                            if (searchContains.Contains)
                            {
                                if (item.Signature == null)
                                {
                                    if (searchContains.Value.IsMatch("Anonymous")) return true;
                                }
                                else
                                {
                                    if (searchContains.Value.IsMatch(item.Signature)) return true;
                                }
                                flag = true;
                            }
                        }

                        foreach (var searchContains in searchItem.SearchNameCollection)
                        {
                            if (searchContains.Contains)
                            {
                                if (searchContains.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                    .All(n => item.Value.Name.Contains(n, StringComparison.OrdinalIgnoreCase))) return true;
                                flag = true;
                            }
                        }

                        foreach (var searchContains in searchItem.SearchNameRegexCollection)
                        {
                            if (searchContains.Contains)
                            {
                                if (searchContains.Value.IsMatch(item.Value.Name)) return true;
                                flag = true;
                            }
                        }

                        foreach (var searchContains in searchItem.SearchSeedCollection)
                        {
                            if (searchContains.Contains)
                            {
                                if (comparer.Equals(item.Value, searchContains.Value)) return true;
                                flag = true;
                            }
                        }

                        return !flag;
                    }
                }).ToList();

                return list;
            }
        }

        private class SeedsState
        {
            private SmallList<Seed> _seeds;
            private SmallList<int> _downloadIds;
            private SmallList<int> _uploadIds;

            public SearchState State { get; set; }

            public SmallList<Seed> Seeds
            {
                get
                {
                    if (_seeds == null)
                        _seeds = new SmallList<Seed>();

                    return _seeds;
                }
            }

            public SmallList<int> DownloadIds
            {
                get
                {
                    if (_downloadIds == null)
                        _downloadIds = new SmallList<int>();

                    return _downloadIds;
                }
            }

            public SmallList<int> UploadIds
            {
                get
                {
                    if (_uploadIds == null)
                        _uploadIds = new SmallList<int>();

                    return _uploadIds;
                }
            }
        }

        private void Update()
        {
            _mainWindow.Title = string.Format("Amoeba {0}", _serviceManager.AmoebaVersion);
            _updateEvent.Set();
        }

        private void Update_Cache()
        {
            this.Update_Cache(true);
        }

        private void Update_Cache(bool update)
        {
            _autoUpdate = update;
            _cacheUpdateEvent.Set();
        }

        private void Update_Title()
        {
            if (_mainWindow.SelectedTab == MainWindowTabType.Search)
            {
                if (_treeView.SelectedItem is SearchTreeViewModel)
                {
                    var selectTreeViewModel = (SearchTreeViewModel)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Amoeba {0} - {1}", _serviceManager.AmoebaVersion, selectTreeViewModel.Value.SearchItem.Name);
                }
            }
        }

        private void _textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
                if (selectTreeViewModel == null) selectTreeViewModel = _treeViewModel;

                if (string.IsNullOrWhiteSpace(_textBox.Text)) return;

                var searchTreeItem = new SearchTreeItem(new SearchItem());
                searchTreeItem.SearchItem.Name = string.Format("Name - \"{0}\"", _textBox.Text);
                searchTreeItem.SearchItem.SearchNameCollection.Add(new SearchContains<string>(true, _textBox.Text));

                selectTreeViewModel.Value.Children.Add(searchTreeItem);

                selectTreeViewModel.Update();

                _textBox.Text = "";

                {
                    var targetTreeViewModel = selectTreeViewModel.Children.OfType<SearchTreeViewModel>().FirstOrDefault(n => n.Value == searchTreeItem);

                    if (targetTreeViewModel != null)
                    {
                        targetTreeViewModel.IsSelected = true;
                    }
                }

                this.Update();

                e.Handled = true;
            }
        }

        #region _treeView

        private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private void TreeViewItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = ((UIElement)e.OriginalSource).FindAncestor<TreeViewItem>();
            if (element == null) return;

            var item = _treeView.SearchItemFromElement(element) as TreeViewModelBase;
            if (item == null) return;

            item.IsSelected = true;

            e.Handled = true;
        }

        private Point _startPoint = new Point(-1, -1);

        private void _treeView_PreviewDragOver(object sender, DragEventArgs e)
        {
            Point position = MouseUtils.GetMousePosition(_treeView);

            if (position.Y < 50)
            {
                var peer = ItemsControlAutomationPeer.CreatePeerForElement(_treeView);
                var scrollProvider = peer.GetPattern(PatternInterface.Scroll) as IScrollProvider;

                try
                {
                    scrollProvider.Scroll(System.Windows.Automation.ScrollAmount.NoAmount, System.Windows.Automation.ScrollAmount.SmallDecrement);
                }
                catch (Exception)
                {

                }
            }
            else if ((_treeView.ActualHeight - position.Y) < 50)
            {
                var peer = ItemsControlAutomationPeer.CreatePeerForElement(_treeView);
                var scrollProvider = peer.GetPattern(PatternInterface.Scroll) as IScrollProvider;

                try
                {
                    scrollProvider.Scroll(System.Windows.Automation.ScrollAmount.NoAmount, System.Windows.Automation.ScrollAmount.SmallIncrement);
                }
                catch (Exception)
                {

                }
            }
        }

        private void _treeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released)
            {
                if (_listView.ContextMenu.IsVisible) return;
                if (_startPoint.X == -1 && _startPoint.Y == -1) return;

                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (_treeViewModel == _treeView.SelectedItem) return;

                    var data = new DataObject("TreeViewModel", _treeView.SelectedItem);
                    DragDrop.DoDragDrop(_treeView, data, DragDropEffects.Move);
                }
            }
        }

        private void _treeView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("TreeViewModel"))
            {
                var sourceItem = (TreeViewModelBase)e.Data.GetData("TreeViewModel");

                if (sourceItem is SearchTreeViewModel)
                {
                    var destinationItem = this.GetDropDestination((UIElement)e.OriginalSource);

                    if (destinationItem is SearchTreeViewModel)
                    {
                        var s = (SearchTreeViewModel)sourceItem;
                        var d = (SearchTreeViewModel)destinationItem;

                        if (s == d) return;
                        if (d.Value.Children.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (d.GetAncestors().Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = s.Parent;

                        if (parentItem is SearchTreeViewModel)
                        {
                            var p = (SearchTreeViewModel)parentItem;

                            var tItems = p.Value.Children.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            p.Value.Children.Clear();
                            p.Value.Children.AddRange(tItems);
                            p.Update();
                        }

                        d.IsSelected = true;
                        d.Value.Children.Add(s.Value);
                        d.Update();
                    }
                }
            }
        }

        private TreeViewModelBase GetDropDestination(UIElement originalSource)
        {
            var element = originalSource.FindAncestor<TreeViewItem>();
            if (element == null) return null;

            return (TreeViewModelBase)_treeView.SearchItemFromElement(element) as TreeViewModelBase;
        }

        private void _treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var element = ((UIElement)e.OriginalSource).FindAncestor<TreeViewItem>();
            if (element == null) return;

            var item = _treeView.SearchItemFromElement(element) as TreeViewModelBase;
            if (item == null)
            {
                _startPoint = new Point(-1, -1);

                return;
            }

            if (item.IsSelected == true)
            {
                _startPoint = e.GetPosition(null);
                _treeView.RaiseEvent(new RoutedPropertyChangedEventArgs<object>(null, null, TreeView.SelectedItemChangedEvent));
            }
            else
            {
                _startPoint = new Point(-1, -1);
            }
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.Update();
        }

        private void _treeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var treeViewItem = sender as TreeViewItem;
            if (treeViewItem == null) return;

            var treeViewModel = (TreeViewModelBase)_treeView.SearchItemFromElement((DependencyObject)treeViewItem);
            if (_treeView.SelectedItem != treeViewModel) return;

            var contextMenu = treeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            _startPoint = new Point(-1, -1);

            MenuItem treeViewItemDeleteMenuItem = contextMenu.GetItem<MenuItem>("_treeViewItemDeleteMenuItem");
            MenuItem treeViewItemCutMenuItem = contextMenu.GetItem<MenuItem>("_treeViewItemCutMenuItem");
            MenuItem treeViewItemPasteMenuItem = contextMenu.GetItem<MenuItem>("_treeViewItemPasteMenuItem");

            treeViewItemDeleteMenuItem.IsEnabled = !(_treeViewModel == treeViewModel);
            treeViewItemCutMenuItem.IsEnabled = !(_treeViewModel == treeViewModel);
            treeViewItemPasteMenuItem.IsEnabled = Clipboard.ContainsSearchTreeItems();
        }

        private void _treeViewItemNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            var searchTreeItem = new SearchTreeItem(new SearchItem());

            var window = new SearchItemEditWindow(searchTreeItem.SearchItem);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewModel.Value.Children.Add(searchTreeItem);

                selectTreeViewModel.Update();
            }

            this.Update();
        }

        private void _treeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            var window = new SearchItemEditWindow(selectTreeViewModel.Value.SearchItem);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewModel.Update();
            }

            this.Update();
        }

        private void _treeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null || selectTreeViewModel == _treeViewModel) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parentItem = selectTreeViewModel.Parent;

            if (parentItem is SearchTreeViewModel)
            {
                var p = (SearchTreeViewModel)parentItem;

                p.Value.Children.Remove(selectTreeViewModel.Value);

                p.Update();
            }

            this.Update();
        }

        private void _treeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null || selectTreeViewModel == _treeViewModel) return;

            Clipboard.SetSearchTreeItems(new List<SearchTreeItem>() { selectTreeViewModel.Value });

            var parentItem = selectTreeViewModel.Parent;

            if (parentItem is SearchTreeViewModel)
            {
                var p = (SearchTreeViewModel)parentItem;

                p.Value.Children.Remove(selectTreeViewModel.Value);

                p.Update();
            }

            this.Update();
        }

        private void _treeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            Clipboard.SetSearchTreeItems(new List<SearchTreeItem>() { selectTreeViewModel.Value });
        }

        private void _treeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            selectTreeViewModel.Value.Children.AddRange(Clipboard.GetSearchTreeItems());

            selectTreeViewModel.Update();

            this.Update();
        }

        private void _treeViewItemExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            var box = new Box();
            box.Name = selectTreeViewModel.Value.SearchItem.Name;
            box.CreationTime = DateTime.UtcNow;

            foreach (var seed in _listViewModelCollection.Cast<SearchListViewModel>().Select(n => n.Value))
            {
                box.Seeds.Add(seed);
            }

            var window = new BoxEditWindow(box);
            window.Owner = _mainWindow;
            window.ShowDialog();

            if (window.DialogResult != true) return;

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.RestoreDirectory = true;
                dialog.FileName = box.Name;
                dialog.DefaultExt = ".box";
                dialog.Filter = "Box (*.box)|*.box";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileName = dialog.FileName;

                    using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                    using (Stream boxStream = AmoebaConverter.ToBoxStream(box))
                    using (var safeBuffer = _bufferManager.CreateSafeBuffer(1024 * 4))
                    {
                        int length;

                        while ((length = boxStream.Read(safeBuffer.Value, 0, safeBuffer.Value.Length)) > 0)
                        {
                            fileStream.Write(safeBuffer.Value, 0, length);
                        }
                    }

                    this.Update();
                }
            }
        }

        #endregion

        #region _listView

        private void _listView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_listView.GetCurrentIndex(e.GetPosition) < 0) return;

            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var list = new HashSet<Seed>();

            foreach (var item in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                if (item.Value == null) continue;

                list.Add(item.Value);
            }

            if (list.Count == 0) return;

            Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var seed in list)
                    {
                        _amoebaManager.Download(seed, 3);
                    }

                    this.Update_Cache(false);
                }
                catch (Exception)
                {

                }
            });
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_refreshing)
            {
                _listViewContextMenu.IsEnabled = false;

                e.Handled = true;
            }
            else
            {
                _listViewContextMenu.IsEnabled = true;

                var selectItems = _listView.SelectedItems;

                _listViewEditMenuItem.IsEnabled = (selectItems != null && selectItems.OfType<SearchListViewModel>().Any(n => n.Seeds.Count > 0));
                _listViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _listViewCopyInfoMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _listViewFilterMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _listViewSearchMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _listViewDownloadMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _listViewInformationMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

                if (!_listViewDeleteMenuItem_IsEnabled) _listViewDeleteMenuItem.IsEnabled = false;
                else _listViewDeleteMenuItem.IsEnabled = (selectItems != null
                    && selectItems.OfType<SearchListViewModel>().Any(n => (n.State & (SearchState.Cache | SearchState.Downloading | SearchState.Uploading | SearchState.Downloaded | SearchState.Uploaded)) > 0));

                if (_listViewDeleteMenuItem.IsEnabled)
                {
                    if (!_listViewDeleteCacheMenuItem_IsEnabled) _listViewDeleteCacheMenuItem.IsEnabled = false;
                    else _listViewDeleteCacheMenuItem.IsEnabled = (selectItems != null && selectItems.OfType<SearchListViewModel>().Any(n => n.State.HasFlag(SearchState.Cache)));
                    if (!_listViewDeleteDownloadMenuItem_IsEnabled) _listViewDeleteDownloadMenuItem.IsEnabled = false;
                    else _listViewDeleteDownloadMenuItem.IsEnabled = (selectItems != null && selectItems.OfType<SearchListViewModel>().Any(n => n.State.HasFlag(SearchState.Downloading)));
                    if (!_listViewDeleteUploadMenuItem_IsEnabled) _listViewDeleteUploadMenuItem.IsEnabled = false;
                    else _listViewDeleteUploadMenuItem.IsEnabled = (selectItems != null && selectItems.OfType<SearchListViewModel>().Any(n => n.State.HasFlag(SearchState.Uploading)));
                    if (!_listViewDeleteDownloadHistoryMenuItem_IsEnabled) _listViewDeleteDownloadHistoryMenuItem.IsEnabled = false;
                    else _listViewDeleteDownloadHistoryMenuItem.IsEnabled = (selectItems != null && selectItems.OfType<SearchListViewModel>().Any(n => n.State.HasFlag(SearchState.Downloaded)));
                    if (!_listViewDeleteUploadHistoryMenuItem_IsEnabled) _listViewDeleteUploadHistoryMenuItem.IsEnabled = false;
                    else _listViewDeleteUploadHistoryMenuItem.IsEnabled = (selectItems != null && selectItems.OfType<SearchListViewModel>().Any(n => n.State.HasFlag(SearchState.Uploaded)));
                }
                else
                {
                    _listViewDeleteCacheMenuItem.IsEnabled = false;
                    _listViewDeleteDownloadMenuItem.IsEnabled = false;
                    _listViewDeleteUploadMenuItem.IsEnabled = false;
                    _listViewDeleteDownloadHistoryMenuItem.IsEnabled = false;
                    _listViewDeleteUploadHistoryMenuItem.IsEnabled = false;
                }
            }
        }

        private void _listViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems.OfType<SearchListViewModel>();
            if (selectSearchListViewModels == null) return;

            var list = new List<Seed>();

            foreach (var seeds in selectSearchListViewModels.Select(n => n.Seeds))
            {
                foreach (var seed in seeds)
                {
                    list.Add(seed);
                }
            }

            var window = new SeedEditWindow(list);
            window.Owner = _mainWindow;

            if (true == window.ShowDialog())
            {
                this.Update();
            }
        }

        volatile bool _listViewDeleteMenuItem_IsEnabled = true;

        private void _listViewDeleteAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var list = new HashSet<Seed>();
            var downloadList = new HashSet<int>();
            var uploadList = new HashSet<int>();

            foreach (var item in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                if (item.Value == null) continue;

                list.Add(item.Value);

                downloadList.UnionWith(item.DownloadIds);
                uploadList.UnionWith(item.UploadIds);
            }

            if ((list.Count + downloadList.Count + uploadList.Count) == 0) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _listViewDeleteMenuItem_IsEnabled = false;

            Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in list)
                    {
                        _amoebaManager.RemoveCache(item);
                    }

                    foreach (var item in downloadList)
                    {
                        _amoebaManager.RemoveDownload(item);
                    }

                    foreach (var item in uploadList)
                    {
                        _amoebaManager.RemoveUpload(item);
                    }

                    _amoebaManager.DownloadedSeeds.RemoveAll(n => list.Contains(n));

                    _amoebaManager.UploadedSeeds.RemoveAll(n => list.Contains(n));

                    this.Update_Cache();
                }
                catch (Exception)
                {

                }

                _listViewDeleteMenuItem_IsEnabled = true;
            });
        }

        volatile bool _listViewDeleteCacheMenuItem_IsEnabled = true;

        private void _listViewDeleteCacheMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var list = new HashSet<Seed>();

            foreach (var item in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                if (item.Value == null || !item.State.HasFlag(SearchState.Cache)) continue;

                list.Add(item.Value);
            }

            if (list.Count == 0) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _listViewDeleteCacheMenuItem_IsEnabled = false;

            Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in list)
                    {
                        _amoebaManager.RemoveCache(item);
                    }

                    this.Update_Cache();
                }
                catch (Exception)
                {

                }

                _listViewDeleteCacheMenuItem_IsEnabled = true;
            });
        }

        volatile bool _listViewDeleteDownloadMenuItem_IsEnabled = true;

        private void _listViewDeleteDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var list = new HashSet<int>();

            foreach (var item in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                if (item.DownloadIds == null || !item.State.HasFlag(SearchState.Downloading)) continue;

                list.UnionWith(item.DownloadIds);
            }

            if (list.Count == 0) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _listViewDeleteDownloadMenuItem_IsEnabled = false;

            Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in list)
                    {
                        _amoebaManager.RemoveDownload(item);
                    }

                    this.Update_Cache();
                }
                catch (Exception)
                {

                }

                _listViewDeleteDownloadMenuItem_IsEnabled = true;
            });
        }

        volatile bool _listViewDeleteUploadMenuItem_IsEnabled = true;

        private void _listViewDeleteUploadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var list = new HashSet<int>();

            foreach (var item in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                list.UnionWith(item.UploadIds);
            }

            if (list.Count == 0) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _listViewDeleteUploadMenuItem_IsEnabled = false;

            Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in list)
                    {
                        _amoebaManager.RemoveUpload(item);
                    }

                    this.Update_Cache();
                }
                catch (Exception)
                {

                }

                _listViewDeleteUploadMenuItem_IsEnabled = true;
            });
        }

        volatile bool _listViewDeleteDownloadHistoryMenuItem_IsEnabled = true;

        private void _listViewDeleteDownloadHistoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var list = new HashSet<Seed>();

            foreach (var item in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                if (item.Value == null || !item.State.HasFlag(SearchState.Downloaded)) continue;

                list.Add(item.Value);
            }

            if (list.Count == 0) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _listViewDeleteDownloadHistoryMenuItem_IsEnabled = false;

            Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    _amoebaManager.DownloadedSeeds.RemoveAll(n => list.Contains(n));

                    this.Update_Cache();
                }
                catch (Exception)
                {

                }

                _listViewDeleteDownloadHistoryMenuItem_IsEnabled = true;
            });
        }

        volatile bool _listViewDeleteUploadHistoryMenuItem_IsEnabled = true;

        private void _listViewDeleteUploadHistoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var list = new HashSet<Seed>();

            foreach (var item in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                if (item.Value == null || !item.State.HasFlag(SearchState.Uploaded)) continue;

                list.Add(item.Value);
            }

            if (list.Count == 0) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _listViewDeleteUploadHistoryMenuItem_IsEnabled = false;

            Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    _amoebaManager.UploadedSeeds.RemoveAll(n => list.Contains(n));

                    this.Update_Cache();
                }
                catch (Exception)
                {

                }

                _listViewDeleteUploadHistoryMenuItem_IsEnabled = true;
            });
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetSeeds(_listView.SelectedItems.OfType<SearchListViewModel>().Select(n => n.Value));
        }

        private void _listViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var seed in _listView.SelectedItems.Cast<SearchListViewModel>().Select(n => n.Value))
            {
                sb.AppendLine(AmoebaConverter.ToSeedString(seed));
                sb.AppendLine(MessageConverter.ToInfoMessage(seed));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        private void _listViewSearchSignatureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            foreach (var listItem in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                var searchTreeItem = new SearchTreeItem(new SearchItem());

                var signature = !string.IsNullOrWhiteSpace(listItem.Signature) ? listItem.Signature : "Anonymous";

                var item = new SearchContains<SearchRegex>(
                    true,
                    new SearchRegex(Regex.Escape(signature), false)
                );

                searchTreeItem.SearchItem.Name = string.Format("Signature - \"{0}\"", signature);
                searchTreeItem.SearchItem.SearchSignatureCollection.Add(item);

                if (selectTreeViewModel.Value.Children.Any(n => n.SearchItem.Name == searchTreeItem.SearchItem.Name)) continue;
                selectTreeViewModel.Value.Children.Add(searchTreeItem);

                selectTreeViewModel.Update();
            }
        }

        private void _listViewSearchKeywordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            foreach (var listItem in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                foreach (var keyword in listItem.Value.Keywords)
                {
                    var searchTreeItem = new SearchTreeItem(new SearchItem());

                    var item = new SearchContains<string>(
                        true,
                        keyword
                    );

                    searchTreeItem.SearchItem.Name = string.Format("Keyword - \"{0}\"", keyword);
                    searchTreeItem.SearchItem.SearchKeywordCollection.Add(item);

                    if (selectTreeViewModel.Value.Children.Any(n => n.SearchItem.Name == searchTreeItem.SearchItem.Name)) continue;
                    selectTreeViewModel.Value.Children.Add(searchTreeItem);

                    selectTreeViewModel.Update();
                }
            }
        }

        private void _listViewSearchCreationTimeRangeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            foreach (var listItem in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                var searchTreeItem = new SearchTreeItem(new SearchItem());

                var item = new SearchContains<SearchRange<DateTime>>(
                    true,
                    new SearchRange<DateTime>(listItem.Value.CreationTime, listItem.Value.CreationTime)
                );

                searchTreeItem.SearchItem.Name = string.Format("CreationTime - \"{0}\"", listItem.Value.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo));
                searchTreeItem.SearchItem.SearchCreationTimeRangeCollection.Add(item);

                if (selectTreeViewModel.Value.Children.Any(n => n.SearchItem.Name == searchTreeItem.SearchItem.Name)) continue;
                selectTreeViewModel.Value.Children.Add(searchTreeItem);

                selectTreeViewModel.Update();
            }
        }

        private void _listViewSearchStateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            var states = new HashSet<SearchState>();

            foreach (var listItem in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                foreach (var state in Enum.GetValues(typeof(SearchState)).Cast<SearchState>())
                {
                    if (listItem.State.HasFlag(state))
                    {
                        states.Add(state);
                    }
                }
            }

            var converter = new SearchStateFlagToStringConverter();

            foreach (var state in states)
            {
                var searchTreeItem = new SearchTreeItem(new SearchItem());

                var item = new SearchContains<SearchState>(
                    true,
                    state
                );

                searchTreeItem.SearchItem.Name = string.Format("State - \"{0}\"", converter.Convert(state, typeof(string), null, System.Globalization.CultureInfo.CurrentUICulture));
                searchTreeItem.SearchItem.SearchStateCollection.Add(item);

                if (selectTreeViewModel.Value.Children.Any(n => n.SearchItem.Name == searchTreeItem.SearchItem.Name)) continue;
                selectTreeViewModel.Value.Children.Add(searchTreeItem);

                selectTreeViewModel.Update();
            }
        }

        private void _listViewFilterNameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            foreach (var listItem in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                if (string.IsNullOrWhiteSpace(listItem.Name)) continue;

                var item = new SearchContains<string>(
                    false,
                    listItem.Name
                );

                if (selectTreeViewModel.Value.SearchItem.SearchNameCollection.Contains(item)) continue;
                selectTreeViewModel.Value.SearchItem.SearchNameCollection.Add(item);
            }

            this.Update();
        }

        private void _listViewFilterSignatureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            foreach (var listItem in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                var signature = !string.IsNullOrWhiteSpace(listItem.Signature) ? listItem.Signature : "Anonymous";

                var item = new SearchContains<SearchRegex>(
                    false,
                    new SearchRegex(Regex.Escape(signature), false)
                );

                if (selectTreeViewModel.Value.SearchItem.SearchSignatureCollection.Contains(item)) continue;
                selectTreeViewModel.Value.SearchItem.SearchSignatureCollection.Add(item);
            }

            this.Update();
        }

        private void _listViewFilterKeywordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            foreach (var listItem in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                foreach (var keyword in listItem.Value.Keywords)
                {
                    if (string.IsNullOrWhiteSpace(keyword)) continue;

                    var item = new SearchContains<string>(
                        false,
                        keyword
                    );

                    if (selectTreeViewModel.Value.SearchItem.SearchKeywordCollection.Contains(item)) continue;
                    selectTreeViewModel.Value.SearchItem.SearchKeywordCollection.Add(item);
                }
            }

            this.Update();
        }

        private void _listViewFilterCreationTimeRangeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            foreach (var listItem in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                var item = new SearchContains<SearchRange<DateTime>>(
                    false,
                    new SearchRange<DateTime>(listItem.Value.CreationTime, listItem.Value.CreationTime)
                );

                if (selectTreeViewModel.Value.SearchItem.SearchCreationTimeRangeCollection.Contains(item)) continue;
                selectTreeViewModel.Value.SearchItem.SearchCreationTimeRangeCollection.Add(item);
            }

            this.Update();
        }

        private void _listViewFilterSeedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var selectTreeViewModel = _treeView.SelectedItem as SearchTreeViewModel;
            if (selectTreeViewModel == null) return;

            foreach (var listitem in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                if (listitem.Value == null) continue;

                var item = new SearchContains<Seed>(
                    false,
                    listitem.Value
                );

                if (selectTreeViewModel.Value.SearchItem.SearchSeedCollection.Contains(item)) continue;
                selectTreeViewModel.Value.SearchItem.SearchSeedCollection.Add(item);
            }

            this.Update();
        }

        private void _listViewDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModels = _listView.SelectedItems;
            if (selectSearchListViewModels == null) return;

            var list = new HashSet<Seed>();

            foreach (var item in selectSearchListViewModels.Cast<SearchListViewModel>())
            {
                if (item.Value == null) continue;

                list.Add(item.Value);
            }

            if (list.Count == 0) return;

            Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var seed in list)
                    {
                        _amoebaManager.Download(seed, 3);
                    }

                    this.Update_Cache(false);
                }
                catch (Exception)
                {

                }
            });
        }

        private void _listViewInformationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewModel = _listView.SelectedItem as SearchListViewModel;
            if (selectSearchListViewModel == null) return;

            var window = new SeedInformationWindow(selectSearchListViewModel.Value, _amoebaManager);
            window.Owner = _mainWindow;
            window.Show();
        }

        #endregion

        private void _searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.Update();
            }
        }

        #region Sort

        private void _listView_GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var item = e.OriginalSource as GridViewColumnHeader;
            if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

            var headerClicked = item.Column.Header as string;
            if (headerClicked == null) return;

            ListSortDirection direction;

            if (headerClicked != Settings.Instance.SearchControl_LastHeaderClicked)
            {
                direction = ListSortDirection.Ascending;
            }
            else
            {
                if (Settings.Instance.SearchControl_ListSortDirection == ListSortDirection.Ascending)
                {
                    direction = ListSortDirection.Descending;
                }
                else
                {
                    direction = ListSortDirection.Ascending;
                }
            }

            Settings.Instance.SearchControl_LastHeaderClicked = headerClicked;
            Settings.Instance.SearchControl_ListSortDirection = direction;

            this.Update();
        }

        private IEnumerable<SearchListViewModel> Sort(IEnumerable<SearchListViewModel> collection, int maxCount)
        {
            var sortBy = Settings.Instance.SearchControl_LastHeaderClicked;
            var direction = Settings.Instance.SearchControl_ListSortDirection;

            var list = new List<SearchListViewModel>(collection);

            if (sortBy == LanguagesManager.Instance.SearchControl_Name)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Index.CompareTo(y.Index);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Signature)
            {
                list.Sort((x, y) =>
                {
                    int c = 0;

                    if (x.Signature != null)
                    {
                        if (y.Signature != null)
                        {
                            c = x.Signature.CompareTo(y.Signature);
                        }
                        else
                        {
                            c = 1;
                        }
                    }
                    else
                    {
                        if (y.Signature != null)
                        {
                            c = -1;
                        }
                    }

                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Index.CompareTo(y.Index);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Length)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Length.CompareTo(y.Length);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Index.CompareTo(y.Index);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Keywords)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Keywords.CompareTo(y.Keywords);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Index.CompareTo(y.Index);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_CreationTime)
            {
                list.Sort((x, y) =>
                {
                    int c = x.CreationTime.CompareTo(y.CreationTime);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Index.CompareTo(y.Index);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_State)
            {
                list.Sort((x, y) =>
                {
                    int c = x.State.CompareTo(y.State);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Index.CompareTo(y.Index);
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

        #endregion

        private class SearchListViewModel : IEquatable<SearchListViewModel>
        {
            public int Index { get { return this.Length.GetHashCode(); } }
            public string Name { get; set; }
            public string Signature { get; set; }
            public long Length { get; set; }
            public string Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public SearchState State { get; set; }
            public Seed Value { get; set; }

            public SmallList<Seed> Seeds { get; set; }
            public SmallList<int> DownloadIds { get; set; }
            public SmallList<int> UploadIds { get; set; }

            public override int GetHashCode()
            {
                if (this.Name == null) return 0;
                else return this.Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if ((object)obj == null || !(obj is SearchListViewModel)) return false;

                return this.Equals((SearchListViewModel)obj);
            }

            public bool Equals(SearchListViewModel other)
            {
                if ((object)other == null) return false;
                if (object.ReferenceEquals(this, other)) return true;

                if (this.Name != other.Name
                    || this.Signature != other.Signature
                    || this.Length != other.Length
                    || this.Keywords != other.Keywords
                    || this.CreationTime != other.CreationTime
                    || this.State != other.State
                    || this.Value != other.Value

                    || (this.Seeds == null) != (other.Seeds == null)
                    || (this.DownloadIds == null) != (other.DownloadIds == null)
                    || (this.UploadIds == null) != (other.UploadIds == null))
                {
                    return false;
                }

                if (this.Seeds != null && other.Seeds != null)
                {
                    if (!CollectionUtils.Equals(this.Seeds, other.Seeds)) return false;
                }

                if (this.DownloadIds != null && other.DownloadIds != null)
                {
                    if (!CollectionUtils.Equals(this.DownloadIds, other.DownloadIds)) return false;
                }

                if (this.UploadIds != null && other.UploadIds != null)
                {
                    if (!CollectionUtils.Equals(this.UploadIds, other.UploadIds)) return false;
                }

                return true;
            }
        }

        private void Execute_New(object sender, ExecutedRoutedEventArgs e)
        {
            var contextMenu = _treeView.FindResource("_treeViewItemContextMenu") as ContextMenu;
            if (contextMenu == null) return;

            contextMenu.GetItem<MenuItem>("_treeViewItemNewMenuItem").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            var contextMenu = _treeView.FindResource("_treeViewItemContextMenu") as ContextMenu;
            if (contextMenu == null) return;

            if (_listView.SelectedItems.Count == 0)
            {
                contextMenu.GetItem<MenuItem>("_treeViewItemDeleteMenuItem").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }
            else
            {
                _listViewDeleteAllMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            var contextMenu = _treeView.FindResource("_treeViewItemContextMenu") as ContextMenu;
            if (contextMenu == null) return;

            if (_listView.SelectedItems.Count == 0)
            {
                contextMenu.GetItem<MenuItem>("_treeViewItemCopyMenuItem").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }
            else
            {
                _listViewCopyMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            var contextMenu = _treeView.FindResource("_treeViewItemContextMenu") as ContextMenu;
            if (contextMenu == null) return;

            if (_listView.SelectedItems.Count == 0)
            {
                contextMenu.GetItem<MenuItem>("_treeViewItemCutMenuItem").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }
            else
            {

            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            var contextMenu = _treeView.FindResource("_treeViewItemContextMenu") as ContextMenu;
            if (contextMenu == null) return;

            contextMenu.GetItem<MenuItem>("_treeViewItemPasteMenuItem").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }

        private void Execute_Search(object sender, ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(24);
            _searchTextBox.Focus();
        }

        private void Execute_Close(object sender, ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(0);
            _searchTextBox.Text = "";

            this.Update();
        }
    }
}
