using System;
using System.Collections;
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
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private volatile bool _refresh;
        private volatile bool _cacheUpdate;
        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private SearchTreeViewItem _treeViewItem;
        private LockedList<SearchListViewItem> _searchingCache = new LockedList<SearchListViewItem>();

        private Thread _searchThread;
        private Thread _cacheThread;

        public SearchControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            _treeViewItem = new SearchTreeViewItem(Settings.Instance.SearchControl_SearchTreeItem);

            InitializeComponent();

            _treeView.Items.Add(_treeViewItem);

            //try
            //{
            //    _treeViewItem.IsSelected = true;
            //}
            //catch (Exception)
            //{

            //}

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                if (e.OriginalSource != _mainWindow._tabControl) return;

                if (_mainWindow.SelectedTab == MainWindowTabType.Search)
                {
                    if (!_refresh) this.Update_Title();
                    _autoResetEvent.Set();
                }
            };

            _searchThread = new Thread(this.Search);
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "SearchControl_SearchThread";
            _searchThread.Start();

            _cacheThread = new Thread(this.Cache);
            _cacheThread.Priority = ThreadPriority.Highest;
            _cacheThread.IsBackground = true;
            _cacheThread.Name = "LibraryControl_CacheThread";
            _cacheThread.Start();

            _searchRowDefinition.Height = new GridLength(0);

            LanguagesManager.UsingLanguageChangedEvent += this.LanguagesManager_UsingLanguageChangedEvent;

            this.Update_Cache();
        }

        private void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {
            _listView.Items.Refresh();
        }

        private void Search()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(100);
                    if (!_refresh) continue;

                    SearchTreeViewItem tempTreeViewItem = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        tempTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
                    }));

                    if (tempTreeViewItem == null) continue;

                    HashSet<SearchListViewItem> newList = new HashSet<SearchListViewItem>();

                    string[] words = null;

                    {
                        string searchText = null;

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            searchText = _searchTextBox.Text;
                        }));

                        if (!string.IsNullOrWhiteSpace(searchText))
                        {
                            words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                        }
                    }

                    foreach (var item in _searchingCache)
                    {
                        if (words != null && words.Length != 0)
                        {
                            var text = (item.Name ?? "").ToLower();
                            if (!words.All(n => text.Contains(n))) continue;
                        }

                        newList.Add(item);
                    }

                    List<SearchTreeViewItem> searchTreeViewItems = new List<SearchTreeViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        searchTreeViewItems.AddRange(_treeView.GetAncestors(tempTreeViewItem).OfType<SearchTreeViewItem>());
                    }));

                    foreach (var searchTreeViewItem in searchTreeViewItems)
                    {
                        SearchControl.Filter(ref newList, searchTreeViewItem.Value.SearchItem);

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            searchTreeViewItem.Hit = newList.Count;
                            searchTreeViewItem.Update();
                        }));
                    }

                    var sortList = this.Sort(newList, 100000); // 10万件

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        if (tempTreeViewItem != _treeView.SelectedItem) return;
                        _refresh = false;

                        _listView.Items.Clear();

                        foreach (var item in sortList)
                        {
                            _listView.Items.Add(item);

                            if ((_listView.Items.Count % 256) == 0)
                            {
                                if (_refresh) return;

                                this.DoEvents();
                            }
                        }

                        this.Update_Title();
                    }));
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private static void Filter(ref HashSet<SearchListViewItem> items, SearchItem searchItem)
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
                                if (item.Value.Keywords.Any(n => !string.IsNullOrWhiteSpace(n) && n == searchContains.Value)) return false;
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
                                    .All(n => item.Value.Name.Contains(n))) return false;
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
                                if (item.Value.Keywords.Any(n => !string.IsNullOrWhiteSpace(n) && n == searchContains.Value)) return true;
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
                                     .All(n => item.Value.Name.Contains(n))) return true;
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

                items.Clear();
                items.UnionWith(list);
            }
        }

        private void Cache()
        {
            try
            {
                for (; ; )
                {
                    _autoResetEvent.WaitOne(1000 * 60 * 3);

                    while (_mainWindow.SelectedTab != MainWindowTabType.Search)
                    {
                        Thread.Sleep(1000);
                    }

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    Dictionary<Seed, SeedsAndSearchState> seedsDictionary = new Dictionary<Seed, SeedsAndSearchState>();

                    {
                        foreach (var seed in _amoebaManager.CacheSeeds)
                        {
                            SeedsAndSearchState item = null;

                            if (seedsDictionary.TryGetValue(seed, out item))
                            {
                                item.Seeds.Add(seed);
                            }
                            else
                            {
                                item = new SeedsAndSearchState();
                                item.State = SearchState.Cache;
                                item.Seeds.Add(seed);

                                seedsDictionary.Add(seed, item);
                            }
                        }

                        {
                            var seedList = new List<Seed>();

                            {
                                var boxList = new List<Box>();
                                boxList.Add(Settings.Instance.LibraryControl_Box);

                                {
                                    List<StoreCategorizeTreeItem> storeCategorizeTreeItems = new List<StoreCategorizeTreeItem>();
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
                                SeedsAndSearchState item = null;

                                if (seedsDictionary.TryGetValue(seed, out item))
                                {
                                    item.State |= SearchState.Store;
                                }
                                else
                                {
                                    item = new SeedsAndSearchState();
                                    item.State = SearchState.Store;

                                    seedsDictionary.Add(seed, item);
                                }
                            }
                        }

                        {
                            var seedList = new List<Seed>();

                            {
                                var boxList = new List<Box>();

                                foreach (var item in Settings.Instance.LinkOptionsWindow_DownloadLinkItems)
                                {
                                    foreach (var signature in item.TrustSignatures)
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
                                SeedsAndSearchState item = null;

                                if (seedsDictionary.TryGetValue(seed, out item))
                                {
                                    item.State |= SearchState.Link;
                                }
                                else
                                {
                                    item = new SeedsAndSearchState();
                                    item.State = SearchState.Link;

                                    seedsDictionary.Add(seed, item);
                                }
                            }
                        }

                        foreach (var information in _amoebaManager.UploadingInformation)
                        {
                            if (information.Contains("Seed") && ((UploadState)information["State"]) != UploadState.Completed)
                            {
                                var seed = (Seed)information["Seed"];
                                SeedsAndSearchState item = null;

                                if (seedsDictionary.TryGetValue(seed, out item))
                                {
                                    item.State |= SearchState.Uploading;
                                    item.Seeds.Add(seed);
                                    item.UploadIds.Add((int)information["Id"]);
                                }
                                else
                                {
                                    item = new SeedsAndSearchState();
                                    item.State = SearchState.Uploading;
                                    item.Seeds.Add(seed);
                                    item.UploadIds.Add((int)information["Id"]);

                                    seedsDictionary.Add(seed, item);
                                }
                            }
                        }

                        foreach (var information in _amoebaManager.DownloadingInformation)
                        {
                            if (information.Contains("Seed") && ((DownloadState)information["State"]) != DownloadState.Completed)
                            {
                                var seed = (Seed)information["Seed"];
                                SeedsAndSearchState item = null;

                                if (seedsDictionary.TryGetValue(seed, out item))
                                {
                                    item.State |= SearchState.Downloading;
                                    item.Seeds.Add(seed);
                                    item.DownloadIds.Add((int)information["Id"]);
                                }
                                else
                                {
                                    item = new SeedsAndSearchState();
                                    item.State = SearchState.Downloading;
                                    item.Seeds.Add(seed);
                                    item.DownloadIds.Add((int)information["Id"]);

                                    seedsDictionary.Add(seed, item);
                                }
                            }
                        }

                        foreach (var seed in _amoebaManager.UploadedSeeds)
                        {
                            SeedsAndSearchState item = null;

                            if (seedsDictionary.TryGetValue(seed, out item))
                            {
                                item.State |= SearchState.Uploaded;
                                item.Seeds.Add(seed);
                            }
                            else
                            {
                                item = new SeedsAndSearchState();
                                item.State = SearchState.Uploaded;
                                item.Seeds.Add(seed);

                                seedsDictionary.Add(seed, item);
                            }
                        }

                        foreach (var seed in _amoebaManager.DownloadedSeeds)
                        {
                            SeedsAndSearchState item = null;

                            if (seedsDictionary.TryGetValue(seed, out item))
                            {
                                item.State |= SearchState.Downloaded;
                                item.Seeds.Add(seed);
                            }
                            else
                            {
                                item = new SeedsAndSearchState();
                                item.State = SearchState.Downloaded;
                                item.Seeds.Add(seed);

                                seedsDictionary.Add(seed, item);
                            }
                        }
                    }

                    List<SearchListViewItem> searchItems = new List<SearchListViewItem>();

                    foreach (var seed in seedsDictionary.Keys)
                    {
                        var searchItem = new SearchListViewItem();

                        lock (seed.ThisLock)
                        {
                            searchItem.Name = seed.Name;
                            if (seed.Certificate != null) searchItem.Signature = seed.Certificate.ToString();
                            searchItem.Keywords = string.Join(", ", seed.Keywords.Where(n => !string.IsNullOrWhiteSpace(n)));
                            searchItem.CreationTime = seed.CreationTime;
                            searchItem.Length = seed.Length;
                            //searchItem.Comment = seed.Comment;
                            searchItem.Value = seed;
                            searchItem.Seeds = seedsDictionary[seed].Seeds;
                            searchItem.State = seedsDictionary[seed].State;
                            searchItem.UploadIds = seedsDictionary[seed].UploadIds;
                            searchItem.DownloadIds = seedsDictionary[seed].DownloadIds;
                            //if (seed.Key != null && seed.Key.Hash != null) searchItem.Id = NetworkConverter.ToHexString(seed.Key.Hash);
                        }

                        searchItems.Add(searchItem);
                    }

                    lock (_searchingCache.ThisLock)
                    {
                        _searchingCache.Clear();
                        _searchingCache.AddRange(searchItems);
                    }

                    sw.Stop();
                    Debug.WriteLine("SearchControl_Cache {0}", sw.ElapsedMilliseconds);

                    if (_cacheUpdate)
                    {
                        _cacheUpdate = false;

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            this.Update();
                        }));
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        class SeedsAndSearchState
        {
            private List<Seed> _seeds = new List<Seed>();
            private List<int> _downloadIds = new List<int>();
            private List<int> _uploadIds = new List<int>();

            public SearchState State { get; set; }

            public List<Seed> Seeds { get { return _seeds; } }
            public List<int> DownloadIds { get { return _downloadIds; } }
            public List<int> UploadIds { get { return _uploadIds; } }
        }

        private void Update()
        {
            _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
            _refresh = true;
        }

        private void Update_Cache()
        {
            _cacheUpdate = true;
            _autoResetEvent.Set();
        }

        private void Update_Cache(bool update)
        {
            _cacheUpdate = update;
            _autoResetEvent.Set();
        }

        private void Update_Title()
        {
            if (_refresh) return;

            if (_mainWindow.SelectedTab == MainWindowTabType.Search)
            {
                if (_treeView.SelectedItem is SearchTreeViewItem)
                {
                    var selectTreeViewItem = (SearchTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectTreeViewItem.Value.SearchItem.Name);
                }
            }
        }

        private void _textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
                if (selectTreeViewItem == null) return;
                if (_textBox.Text == "") return;

                var searchTreeItem = new SearchTreeItem();
                searchTreeItem.SearchItem = new SearchItem();
                searchTreeItem.SearchItem.Name = string.Format("Name - \"{0}\"", _textBox.Text);
                searchTreeItem.SearchItem.SearchNameCollection.Add(new SearchContains<string>(true, _textBox.Text));

                selectTreeViewItem.Value.Children.Add(searchTreeItem);

                selectTreeViewItem.Update();

                _textBox.Text = "";

                e.Handled = true;
            }
        }

        #region _treeView

        private Point _startPoint = new Point(-1, -1);

        private void _treeView_PreviewDragOver(object sender, DragEventArgs e)
        {
            Point position = MouseUtilities.GetMousePosition(_treeView);

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
                    if (_treeViewItem == _treeView.SelectedItem) return;

                    DataObject data = new DataObject("TreeViewItem", _treeView.SelectedItem);
                    DragDrop.DoDragDrop(_treeView, data, DragDropEffects.Move);
                }
            }
        }

        private void _treeView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("TreeViewItem"))
            {
                var sourceItem = (TreeViewItem)e.Data.GetData("TreeViewItem");

                if (sourceItem is SearchTreeViewItem)
                {
                    var destinationItem = (TreeViewItem)_treeView.GetCurrentItem(e.GetPosition);

                    if (destinationItem is SearchTreeViewItem)
                    {
                        var s = (SearchTreeViewItem)sourceItem;
                        var d = (SearchTreeViewItem)destinationItem;

                        if (s == d) return;
                        if (d.Value.Children.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetAncestors(d).Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = s.Parent;

                        if (parentItem is SearchTreeViewItem)
                        {
                            var p = (SearchTreeViewItem)parentItem;

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

        private void _treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _treeView.GetCurrentItem(e.GetPosition) as TreeViewItem;
            if (item == null)
            {
                _startPoint = new Point(-1, -1);

                return;
            }

            Point lposition = e.GetPosition(_treeView);

            if ((_treeView.ActualWidth - lposition.X) < 15
                || (_treeView.ActualHeight - lposition.Y) < 15)
            {
                _startPoint = new Point(-1, -1);

                return;
            }

            if (item.IsSelected == true)
            {
                _startPoint = e.GetPosition(null);
                _treeView_SelectedItemChanged(null, null);
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
            var selectTreeViewItem = sender as SearchTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            _startPoint = new Point(-1, -1);

            MenuItem treeViewItemDeleteMenuItem = contextMenu.GetMenuItem("_treeViewItemDeleteMenuItem");
            MenuItem treeViewItemCutMenuItem = contextMenu.GetMenuItem("_treeViewItemCutMenuItem");
            MenuItem treeViewItemPasteMenuItem = contextMenu.GetMenuItem("_treeViewItemPasteMenuItem");

            treeViewItemDeleteMenuItem.IsEnabled = !(selectTreeViewItem == _treeViewItem);
            treeViewItemCutMenuItem.IsEnabled = !(selectTreeViewItem == _treeViewItem);
            treeViewItemPasteMenuItem.IsEnabled = Clipboard.ContainsSearchTreeItems();
        }

        private void _treeViewItemNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            var searchTreeItem = new SearchTreeItem();
            searchTreeItem.SearchItem = new SearchItem();

            var searchItem = searchTreeItem.SearchItem;

            SearchItemEditWindow window = new SearchItemEditWindow(ref searchItem);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.Children.Add(searchTreeItem);

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _treeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            var searchItem = selectTreeViewItem.Value.SearchItem;

            SearchItemEditWindow window = new SearchItemEditWindow(ref searchItem);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _treeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parentItem = selectTreeViewItem.Parent;

            if (parentItem is SearchTreeViewItem)
            {
                var p = (SearchTreeViewItem)parentItem;

                p.Value.Children.Remove(selectTreeViewItem.Value);

                p.Update();
            }

            this.Update();
        }

        private void _treeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            Clipboard.SetSearchTreeItems(new List<SearchTreeItem>() { selectTreeViewItem.Value });

            var parentItem = selectTreeViewItem.Parent;

            if (parentItem is SearchTreeViewItem)
            {
                var p = (SearchTreeViewItem)parentItem;

                p.Value.Children.Remove(selectTreeViewItem.Value);

                p.Update();
            }

            this.Update();
        }

        private void _treeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetSearchTreeItems(new List<SearchTreeItem>() { selectTreeViewItem.Value });
        }

        private void _treeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.Children.AddRange(Clipboard.GetSearchTreeItems());

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _treeViewItemExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            Box box = new Box();
            box.Name = selectTreeViewItem.Value.SearchItem.Name;
            box.CreationTime = DateTime.UtcNow;

            foreach (var seed in _listView.Items.Cast<SearchListViewItem>().Select(n => n.Value))
            {
                box.Seeds.Add(seed);
            }

            BoxEditWindow window = new BoxEditWindow(box);
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

                    using (FileStream stream = new FileStream(fileName, FileMode.Create))
                    using (Stream directoryStream = AmoebaConverter.ToBoxStream(box))
                    {
                        int i = -1;
                        byte[] buffer = _bufferManager.TakeBuffer(1024);

                        while ((i = directoryStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            stream.Write(buffer, 0, i);
                        }

                        _bufferManager.ReturnBuffer(buffer);
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

            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                _amoebaManager.Download(item.Value, 3);
            }

            this.Update_Cache(false);
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_refresh)
            {
                _listViewContextMenu.IsEnabled = false;

                e.Handled = true;
            }
            else
            {
                _listViewContextMenu.IsEnabled = true;

                var selectItems = _listView.SelectedItems;

                _listViewEditMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.OfType<SearchListViewItem>().Any(n => n.Seeds.Count > 0));
                _listViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
                _listViewCopyInfoMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
                _listViewFilterMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
                _listViewSearchMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
                _listViewDownloadMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

                if (!_listViewDeleteMenuItem_IsEnabled) _listViewDeleteMenuItem.IsEnabled = false;
                else _listViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : selectItems.OfType<SearchListViewItem>()
                    .Any(n => (n.State & (SearchState.Cache | SearchState.Downloading | SearchState.Uploading | SearchState.Downloaded | SearchState.Uploaded)) != 0);

                if (_listViewDeleteMenuItem.IsEnabled)
                {
                    if (!_listViewDeleteCacheMenuItem_IsEnabled) _listViewDeleteCacheMenuItem.IsEnabled = false;
                    else _listViewDeleteCacheMenuItem.IsEnabled = (selectItems == null) ? false : selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Cache));
                    if (!_listViewDeleteDownloadMenuItem_IsEnabled) _listViewDeleteDownloadMenuItem.IsEnabled = false;
                    else _listViewDeleteDownloadMenuItem.IsEnabled = (selectItems == null) ? false : selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Downloading));
                    if (!_listViewDeleteUploadMenuItem_IsEnabled) _listViewDeleteUploadMenuItem.IsEnabled = false;
                    else _listViewDeleteUploadMenuItem.IsEnabled = (selectItems == null) ? false : selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Uploading));
                    if (!_listViewDeleteDownloadHistoryMenuItem_IsEnabled) _listViewDeleteDownloadHistoryMenuItem.IsEnabled = false;
                    else _listViewDeleteDownloadHistoryMenuItem.IsEnabled = (selectItems == null) ? false : selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Downloaded));
                    if (!_listViewDeleteUploadHistoryMenuItem_IsEnabled) _listViewDeleteUploadHistoryMenuItem.IsEnabled = false;
                    else _listViewDeleteUploadHistoryMenuItem.IsEnabled = (selectItems == null) ? false : selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Uploaded));
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
            var selectSearchListViewItems = _listView.SelectedItems.OfType<SearchListViewItem>();
            if (selectSearchListViewItems == null) return;

            IList<Seed> list = new List<Seed>();

            foreach (var seeds in selectSearchListViewItems.Select(n => n.Seeds))
            {
                foreach (var seed in seeds)
                {
                    list.Add(seed);
                }
            }

            SeedEditWindow window = new SeedEditWindow(list.ToArray());
            window.Owner = _mainWindow;

            if (true == window.ShowDialog())
            {
                this.Update_Cache();
            }
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetSeeds(_listView.SelectedItems.OfType<SearchListViewItem>().Select(n => n.Value));
        }

        private void _listViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var seed in _listView.SelectedItems.Cast<SearchListViewItem>().Select(n => n.Value))
            {
                sb.AppendLine(AmoebaConverter.ToSeedString(seed));
                sb.AppendLine(MessageConverter.ToInfoMessage(seed));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        volatile bool _listViewDeleteMenuItem_IsEnabled = true;

        private void _listViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var list = new HashSet<Seed>();
            var downloadList = new HashSet<int>();
            var uploadList = new HashSet<int>();

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Value == null) continue;

                list.Add(item.Value);

                if (item.DownloadIds != null) downloadList.UnionWith(item.DownloadIds);
                if (item.UploadIds != null) uploadList.UnionWith(item.UploadIds);
            }

            if ((list.Count + downloadList.Count + uploadList.Count) == 0) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _listViewDeleteMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem((object wstate) =>
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

                    foreach (var item in list)
                    {
                        for (; ; )
                        {
                            if (!_amoebaManager.DownloadedSeeds.Remove(item)) break;
                        }
                    }

                    foreach (var item in list)
                    {
                        for (; ; )
                        {
                            if (!_amoebaManager.UploadedSeeds.Remove(item)) break;
                        }
                    }

                    this.Update_Cache();
                }
                catch (Exception)
                {

                }
                finally
                {
                    _listViewDeleteMenuItem_IsEnabled = true;
                }
            });
        }

        volatile bool _listViewDeleteCacheMenuItem_IsEnabled = true;

        private void _listViewDeleteCacheMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var list = new HashSet<Seed>();

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Value == null || !item.State.HasFlag(SearchState.Cache)) continue;

                list.Add(item.Value);
            }

            if (list.Count == 0) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _listViewDeleteCacheMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem((object wstate) =>
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
                finally
                {
                    _listViewDeleteCacheMenuItem_IsEnabled = true;
                }
            });
        }

        volatile bool _listViewDeleteDownloadMenuItem_IsEnabled = true;

        private void _listViewDeleteDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var list = new HashSet<int>();

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.DownloadIds == null || !item.State.HasFlag(SearchState.Downloading)) continue;

                list.UnionWith(item.DownloadIds);
            }

            if (list.Count == 0) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _listViewDeleteDownloadMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem((object wstate) =>
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
                finally
                {
                    _listViewDeleteDownloadMenuItem_IsEnabled = true;
                }
            });
        }

        volatile bool _listViewDeleteUploadMenuItem_IsEnabled = true;

        private void _listViewDeleteUploadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var list = new HashSet<int>();

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.UploadIds == null || !item.State.HasFlag(SearchState.Uploading)) continue;

                list.UnionWith(item.UploadIds);
            }

            if (list.Count == 0) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _listViewDeleteUploadMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem((object wstate) =>
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
                finally
                {
                    _listViewDeleteUploadMenuItem_IsEnabled = true;
                }
            });
        }

        volatile bool _listViewDeleteDownloadHistoryMenuItem_IsEnabled = true;

        private void _listViewDeleteDownloadHistoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var list = new HashSet<Seed>();

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Value == null || !item.State.HasFlag(SearchState.Downloaded)) continue;

                list.Add(item.Value);
            }

            if (list.Count == 0) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _listViewDeleteDownloadHistoryMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in list)
                    {
                        for (; ; )
                        {
                            if (!_amoebaManager.DownloadedSeeds.Remove(item)) break;
                        }
                    }

                    this.Update_Cache();
                }
                catch (Exception)
                {

                }
                finally
                {
                    _listViewDeleteDownloadHistoryMenuItem_IsEnabled = true;
                }
            });
        }

        volatile bool _listViewDeleteUploadHistoryMenuItem_IsEnabled = true;

        private void _listViewDeleteUploadHistoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var list = new HashSet<Seed>();

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Value == null || !item.State.HasFlag(SearchState.Uploaded)) continue;

                list.Add(item.Value);
            }

            if (list.Count == 0) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _listViewDeleteUploadHistoryMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in list)
                    {
                        for (; ; )
                        {
                            if (!_amoebaManager.UploadedSeeds.Remove(item)) break;
                        }
                    }

                    this.Update_Cache();
                }
                catch (Exception)
                {

                }
                finally
                {
                    _listViewDeleteUploadHistoryMenuItem_IsEnabled = true;
                }
            });
        }

        private void _listViewDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                _amoebaManager.Download(item.Value, 3);
            }

            this.Update_Cache(false);
        }

        private void _listViewSearchSignatureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var listItem in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                var searchTreeItem = new SearchTreeItem();
                searchTreeItem.SearchItem = new SearchItem();

                var signature = !string.IsNullOrWhiteSpace(listItem.Signature) ? listItem.Signature : "Anonymous";

                var item = new SearchContains<SearchRegex>(
                    true,
                    new SearchRegex(Regex.Escape(signature), false)
                );

                searchTreeItem.SearchItem.Name = string.Format("Signature - \"{0}\"", signature);
                searchTreeItem.SearchItem.SearchSignatureCollection.Add(item);

                if (selectTreeViewItem.Value.Children.Any(n => n.SearchItem.Name == searchTreeItem.SearchItem.Name)) continue;
                selectTreeViewItem.Value.Children.Add(searchTreeItem);

                selectTreeViewItem.Update();
            }
        }

        private void _listViewSearchKeywordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var listItem in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                foreach (var keyword in listItem.Value.Keywords)
                {
                    var searchTreeItem = new SearchTreeItem();
                    searchTreeItem.SearchItem = new SearchItem();

                    var item = new SearchContains<string>(
                        true,
                        keyword
                    );

                    searchTreeItem.SearchItem.Name = string.Format("Keyword - \"{0}\"", keyword);
                    searchTreeItem.SearchItem.SearchKeywordCollection.Add(item);

                    if (selectTreeViewItem.Value.Children.Any(n => n.SearchItem.Name == searchTreeItem.SearchItem.Name)) continue;
                    selectTreeViewItem.Value.Children.Add(searchTreeItem);

                    selectTreeViewItem.Update();
                }
            }
        }

        private void _listViewSearchCreationTimeRangeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var listItem in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                var searchTreeItem = new SearchTreeItem();
                searchTreeItem.SearchItem = new SearchItem();

                var item = new SearchContains<SearchRange<DateTime>>(
                    true,
                    new SearchRange<DateTime>(listItem.Value.CreationTime, listItem.Value.CreationTime)
                );

                searchTreeItem.SearchItem.Name = string.Format("CreationTime - \"{0}\"", listItem.Value.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo));
                searchTreeItem.SearchItem.SearchCreationTimeRangeCollection.Add(item);

                if (selectTreeViewItem.Value.Children.Any(n => n.SearchItem.Name == searchTreeItem.SearchItem.Name)) continue;
                selectTreeViewItem.Value.Children.Add(searchTreeItem);

                selectTreeViewItem.Update();
            }
        }

        private void _listViewSearchStateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            HashSet<SearchState> states = new HashSet<SearchState>();

            foreach (var listItem in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                foreach (var state in Enum.GetValues(typeof(SearchState)).Cast<SearchState>())
                {
                    if (listItem.State.HasFlag(state))
                    {
                        states.Add(state);
                    }
                }
            }

            SearchStateFlagToStringConverter converter = new SearchStateFlagToStringConverter();

            foreach (var state in states)
            {
                var searchTreeItem = new SearchTreeItem();
                searchTreeItem.SearchItem = new SearchItem();

                var item = new SearchContains<SearchState>(
                    true,
                    state
                );

                searchTreeItem.SearchItem.Name = string.Format("State - \"{0}\"", converter.Convert(state, typeof(string), null, System.Globalization.CultureInfo.CurrentUICulture));
                searchTreeItem.SearchItem.SearchStateCollection.Add(item);

                if (selectTreeViewItem.Value.Children.Any(n => n.SearchItem.Name == searchTreeItem.SearchItem.Name)) continue;
                selectTreeViewItem.Value.Children.Add(searchTreeItem);

                selectTreeViewItem.Update();
            }
        }

        private void _listViewFilterNameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var listItem in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (string.IsNullOrWhiteSpace(listItem.Name)) continue;

                var item = new SearchContains<string>(
                    false,
                    listItem.Name
                );

                if (selectTreeViewItem.Value.SearchItem.SearchNameCollection.Contains(item)) continue;
                selectTreeViewItem.Value.SearchItem.SearchNameCollection.Add(item);
            }

            this.Update_Cache();
        }

        private void _listViewFilterSignatureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var listItem in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                var signature = !string.IsNullOrWhiteSpace(listItem.Signature) ? listItem.Signature : "Anonymous";

                var item = new SearchContains<SearchRegex>(
                    false,
                    new SearchRegex(Regex.Escape(signature), false)
                );

                if (selectTreeViewItem.Value.SearchItem.SearchSignatureCollection.Contains(item)) continue;
                selectTreeViewItem.Value.SearchItem.SearchSignatureCollection.Add(item);
            }

            this.Update();
        }

        private void _listViewFilterKeywordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var listItem in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                foreach (var keyword in listItem.Value.Keywords)
                {
                    if (string.IsNullOrWhiteSpace(keyword)) continue;

                    var item = new SearchContains<string>(
                        false,
                        keyword
                    );

                    if (selectTreeViewItem.Value.SearchItem.SearchKeywordCollection.Contains(item)) continue;
                    selectTreeViewItem.Value.SearchItem.SearchKeywordCollection.Add(item);
                }
            }

            this.Update();
        }

        private void _listViewFilterCreationTimeRangeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var listItem in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                var item = new SearchContains<SearchRange<DateTime>>(
                    false,
                    new SearchRange<DateTime>(listItem.Value.CreationTime, listItem.Value.CreationTime)
                );

                if (selectTreeViewItem.Value.SearchItem.SearchCreationTimeRangeCollection.Contains(item)) continue;
                selectTreeViewItem.Value.SearchItem.SearchCreationTimeRangeCollection.Add(item);
            }

            this.Update();
        }

        private void _listViewFilterSeedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var listitem in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (listitem.Value == null) continue;

                var item = new SearchContains<Seed>(
                    false,
                    listitem.Value
                );

                if (selectTreeViewItem.Value.SearchItem.SearchSeedCollection.Contains(item)) continue;
                selectTreeViewItem.Value.SearchItem.SearchSeedCollection.Add(item);
            }

            this.Update();
        }

        #endregion

        private void _serachCloseButton_Click(object sender, RoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(0);
            _searchTextBox.Text = "";

            this.Update();
        }

        private void _searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.Update();
            }
        }

        #region Sort

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            var item = e.OriginalSource as GridViewColumnHeader;
            if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

            string headerClicked = item.Column.Header as string;
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

        private IEnumerable<SearchListViewItem> Sort(IEnumerable<SearchListViewItem> collection, int maxCount)
        {
            var sortBy = Settings.Instance.SearchControl_LastHeaderClicked;
            var direction = Settings.Instance.SearchControl_ListSortDirection;

            List<SearchListViewItem> list = new List<SearchListViewItem>(collection);

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

        private class SearchListViewItem
        {
            public int Index { get { return this.Length.GetHashCode(); } }
            public string Name { get; set; }
            public string Signature { get; set; }
            public string Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public long Length { get; set; }
            //public string Comment { get; set; }
            //public string Id { get; set; }
            public Seed Value { get; set; }
            public SearchState State { get; set; }

            public List<Seed> Seeds { get; set; }
            public List<int> DownloadIds { get; set; }
            public List<int> UploadIds { get; set; }

            public override int GetHashCode()
            {
                if (this.Name == null) return 0;
                else return this.Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (!(obj is SearchListViewItem)) return false;
                if (obj == null) return false;
                if (object.ReferenceEquals(this, obj)) return true;
                if (this.GetHashCode() != obj.GetHashCode()) return false;

                var other = (SearchListViewItem)obj;

                if (this.Name != other.Name
                    || this.Signature != other.Signature
                    || this.Keywords != other.Keywords
                    || this.CreationTime != other.CreationTime
                    || this.Length != other.Length
                    //|| this.Comment != other.Comment
                    //|| this.Id != other.Id
                    || this.Value != other.Value
                    || this.State != other.State

                    || (this.Seeds == null) != (other.Seeds == null)
                    || (this.DownloadIds == null) != (other.DownloadIds == null)
                    || (this.UploadIds == null) != (other.UploadIds == null))
                {
                    return false;
                }

                if (this.Seeds != null && other.Seeds != null)
                {
                    if (!CollectionUtilities.Equals(this.Seeds, other.Seeds)) return false;
                }

                if (this.DownloadIds != null && other.DownloadIds != null)
                {
                    if (!CollectionUtilities.Equals(this.DownloadIds, other.DownloadIds)) return false;
                }

                if (this.UploadIds != null && other.UploadIds != null)
                {
                    if (!CollectionUtilities.Equals(this.UploadIds, other.UploadIds)) return false;
                }

                return true;
            }
        }

        private void Execute_New(object sender, ExecutedRoutedEventArgs e)
        {
            _treeViewItemNewMenuItem_Click(null, null);
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                _treeViewItemDeleteMenuItem_Click(null, null);
            }
            else
            {
                _listViewDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                _treeViewItemCopyMenuItem_Click(null, null);
            }
            else
            {
                _listViewCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                _treeViewItemCutMenuItem_Click(null, null);
            }
            else
            {

            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            _treeViewItemPasteMenuItem_Click(null, null);
        }

        private void Execute_Search(object sender, ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(24);
            _searchTextBox.Focus();
        }
    }
}
