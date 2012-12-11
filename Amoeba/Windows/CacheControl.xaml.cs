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
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Amoeba.Windows
{
    /// <summary>
    /// CacheControl.xaml の相互作用ロジック
    /// </summary>
    partial class CacheControl : UserControl
    {
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private volatile bool _refresh = false;
        private volatile bool _recache = false;

        private volatile List<SearchListViewItem> _searchingCache = new List<SearchListViewItem>();
        private Stopwatch _updateStopwatch = new Stopwatch();

        private Thread _searchThread = null;

        public CacheControl(MainWindow mainWindow, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _treeViewItem.Value = Settings.Instance.CacheControl_SearchTreeItem;
            _treeViewItem.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(_treeViewItem_PreviewMouseLeftButtonDown);
            try
            {
                _treeViewItem.IsSelected = true;
            }
            catch (Exception)
            {

            }

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                _recache = true;

                var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;

                if (App.SelectTab == "Search" && !_refresh)
                    _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectTreeViewItem.Value.SearchItem.Name);
            };

            _searchThread = new Thread(new ThreadStart(this.Search));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "CacheControl_SearchThread";
            _searchThread.Start();

            _searchRowDefinition.Height = new GridLength(0);

            LanguagesManager.UsingLanguageChangedEvent += new UsingLanguageChangedEventHandler(this.LanguagesManager_UsingLanguageChangedEvent);
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

                    SearchTreeViewItem selectTreeViewItem = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
                    }), null);

                    if (selectTreeViewItem == null) continue;

                    HashSet<SearchListViewItem> newList = new HashSet<SearchListViewItem>(this.GetSearchListViewItems());
                    List<SearchTreeViewItem> searchTreeViewItems = new List<SearchTreeViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        searchTreeViewItems.AddRange(_treeViewItem.GetLineage(selectTreeViewItem).OfType<SearchTreeViewItem>());
                    }), null);

                    foreach (var searchTreeViewItem in searchTreeViewItems)
                    {
                        CacheControl.Filter(ref newList, searchTreeViewItem.Value.SearchItem);

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            searchTreeViewItem.Hit = newList.Count;
                            searchTreeViewItem.Update();
                        }), null);
                    }

                    {
                        string searchText = null;

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            searchText = _searchTextBox.Text;
                        }), null);

                        if (!string.IsNullOrWhiteSpace(searchText))
                        {
                            var words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                            List<SearchListViewItem> list = new List<SearchListViewItem>();

                            foreach (var item in newList)
                            {
                                var text = (item.Name ?? "").ToLower();

                                foreach (var word in words)
                                {
                                    if (!text.Contains(word))
                                    {
                                        goto End;
                                    }
                                }

                                list.Add(item);

                            End: ;
                            }

                            newList.Clear();
                            newList.UnionWith(list);
                        }
                    }

                    HashSet<SearchListViewItem> oldList = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        oldList = new HashSet<SearchListViewItem>(_listView.Items.OfType<SearchListViewItem>().ToArray());
                    }), null);

                    var removeList = new List<SearchListViewItem>();
                    var addList = new List<SearchListViewItem>();

                    foreach (var item in oldList)
                    {
                        if (!newList.Contains(item))
                        {
                            removeList.Add(item);
                        }
                    }

                    foreach (var item in newList)
                    {
                        if (!oldList.Contains(item))
                        {
                            addList.Add(item);
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        if (selectTreeViewItem != _treeView.SelectedItem) return;
                        _refresh = false;

                        _listView.SelectedItems.Clear();

                        bool sortFlag = false;

                        if (removeList.Count > 100)
                        {
                            sortFlag = true;

                            _listView.Items.Clear();

                            foreach (var item in newList)
                            {
                                _listView.Items.Add(item);
                            }
                        }
                        else
                        {
                            if (addList.Count != 0) sortFlag = true;
                            if (removeList.Count != 0) sortFlag = true;

                            foreach (var item in addList)
                            {
                                _listView.Items.Add(item);
                            }

                            foreach (var item in removeList)
                            {
                                _listView.Items.Remove(item);
                            }
                        }

                        if (sortFlag) this.Sort();

                        if (App.SelectTab == "Search")
                            _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectTreeViewItem.Value.SearchItem.Name);
                    }), null);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private static void Filter(ref HashSet<SearchListViewItem> items, SearchItem searchItem)
        {
            lock (searchItem.ThisLock)
            {
                items.IntersectWith(items.ToArray().Where(item =>
                {
                    bool flag = true;

                    lock (searchItem.SearchStateCollection.ThisLock)
                    {
                        if (searchItem.SearchStateCollection.Any(n => n.Contains == true))
                        {
                            flag = searchItem.SearchStateCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return item.State.HasFlag(searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchLengthRangeCollection.ThisLock)
                    {
                        if (searchItem.SearchLengthRangeCollection.Any(n => n.Contains == true))
                        {
                            flag = searchItem.SearchLengthRangeCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchContains.Value.Verify(item.Value.Length);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchCreationTimeRangeCollection.ThisLock)
                    {
                        if (searchItem.SearchCreationTimeRangeCollection.Any(n => n.Contains == true))
                        {
                            flag = searchItem.SearchCreationTimeRangeCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchContains.Value.Verify(item.Value.CreationTime);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchKeywordCollection.ThisLock)
                    {
                        if (searchItem.SearchKeywordCollection.Any(n => n.Contains == true))
                        {
                            flag = searchItem.SearchKeywordCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return item.Value.Keywords.Any(n => !string.IsNullOrWhiteSpace(n) && n == searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchSignatureCollection.ThisLock)
                    {
                        if (searchItem.SearchSignatureCollection.Any(n => n.Contains == true))
                        {
                            flag = searchItem.SearchSignatureCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains)
                                {
                                    if (searchContains.Value == "Anonymous")
                                    {
                                        return item.Signature == null;
                                    }
                                    else
                                    {
                                        return item.Signature == searchContains.Value;
                                    }
                                }

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchNameCollection.ThisLock)
                    {
                        if (searchItem.SearchNameCollection.Any(n => n.Contains == true))
                        {
                            flag = searchItem.SearchNameCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains)
                                {
                                    return searchContains.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                        .All(n => item.Value.Name.ToLower().Contains(n.ToLower()));
                                }

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchNameRegexCollection.ThisLock)
                    {
                        if (searchItem.SearchNameRegexCollection.Any(n => n.Contains == true))
                        {
                            flag = searchItem.SearchNameRegexCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchContains.Value.IsMatch(item.Value.Name);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchSeedCollection.ThisLock)
                    {
                        if (searchItem.SearchSeedCollection.Any(n => n.Contains == true))
                        {
                            SeedHashEqualityComparer comparer = new SeedHashEqualityComparer();

                            flag = searchItem.SearchSeedCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return comparer.Equals(item.Value, searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    return flag;
                }));

                items.ExceptWith(items.ToArray().Where(item =>
                {
                    bool flag = false;

                    lock (searchItem.SearchStateCollection.ThisLock)
                    {
                        if (searchItem.SearchStateCollection.Any(n => n.Contains == false))
                        {
                            flag = searchItem.SearchStateCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return item.State.HasFlag(searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchLengthRangeCollection.ThisLock)
                    {
                        if (searchItem.SearchLengthRangeCollection.Any(n => n.Contains == false))
                        {
                            flag = searchItem.SearchLengthRangeCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value.Verify(item.Value.Length);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchCreationTimeRangeCollection.ThisLock)
                    {
                        if (searchItem.SearchCreationTimeRangeCollection.Any(n => n.Contains == false))
                        {
                            flag = searchItem.SearchCreationTimeRangeCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value.Verify(item.Value.CreationTime);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchKeywordCollection.ThisLock)
                    {
                        if (searchItem.SearchKeywordCollection.Any(n => n.Contains == false))
                        {
                            flag = searchItem.SearchKeywordCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return item.Value.Keywords.Any(n => !string.IsNullOrWhiteSpace(n) && n == searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchSignatureCollection.ThisLock)
                    {
                        if (searchItem.SearchSignatureCollection.Any(n => n.Contains == false))
                        {
                            flag = searchItem.SearchSignatureCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains)
                                {
                                    if (searchContains.Value == "Anonymous")
                                    {
                                        return item.Signature == null;
                                    }
                                    else
                                    {
                                        return item.Signature == searchContains.Value;
                                    }
                                }

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchNameCollection.ThisLock)
                    {
                        if (searchItem.SearchNameCollection.Any(n => n.Contains == false))
                        {
                            flag = searchItem.SearchNameCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains)
                                {
                                    return searchContains.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                        .All(n => item.Value.Name.Contains(n));
                                }

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchNameRegexCollection.ThisLock)
                    {
                        if (searchItem.SearchNameRegexCollection.Any(n => n.Contains == false))
                        {
                            flag = searchItem.SearchNameRegexCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value.IsMatch(item.Value.Name);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (searchItem.SearchSeedCollection.ThisLock)
                    {
                        if (searchItem.SearchSeedCollection.Any(n => n.Contains == false))
                        {
                            SeedHashEqualityComparer comparer = new SeedHashEqualityComparer();

                            flag = searchItem.SearchSeedCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return comparer.Equals(item.Value, searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    return flag;
                }));
            }
        }

        private IEnumerable<SearchListViewItem> GetSearchListViewItems()
        {
            try
            {
                if (!_recache && _updateStopwatch.IsRunning && _updateStopwatch.Elapsed.TotalSeconds < 60)
                {
                    return _searchingCache;
                }

                _recache = false;

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

                    foreach (var seed in _amoebaManager.ShareSeeds)
                    {
                        SeedsAndSearchState item = null;

                        if (seedsDictionary.TryGetValue(seed, out item))
                        {
                            item.State |= SearchState.Share;
                            item.Seeds.Add(seed);
                        }
                        else
                        {
                            item = new SeedsAndSearchState();
                            item.State = SearchState.Share;
                            item.Seeds.Add(seed);

                            seedsDictionary.Add(seed, item);
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

                                if (item.UploadIds == null)
                                    item.UploadIds = new List<int>();

                                item.UploadIds.Add((int)information["Id"]);
                            }
                            else
                            {
                                item = new SeedsAndSearchState();
                                item.State = SearchState.Uploading;
                                item.Seeds.Add(seed);

                                if (item.UploadIds == null)
                                    item.UploadIds = new List<int>();

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

                                if (item.DownloadIds == null)
                                    item.DownloadIds = new List<int>();

                                item.DownloadIds.Add((int)information["Id"]);
                            }
                            else
                            {
                                item = new SeedsAndSearchState();
                                item.State = SearchState.Downloading;
                                item.Seeds.Add(seed);

                                if (item.DownloadIds == null)
                                    item.DownloadIds = new List<int>();

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
                        searchItem.Signature = MessageConverter.ToSignatureString(seed.Certificate);
                        searchItem.Keywords = string.Join(", ", seed.Keywords.Where(n => !string.IsNullOrWhiteSpace(n)));
                        searchItem.CreationTime = seed.CreationTime;
                        searchItem.Length = seed.Length;
                        searchItem.Comment = seed.Comment;
                        searchItem.Value = seed;
                        searchItem.Seeds = seedsDictionary[seed].Seeds;
                        searchItem.State = seedsDictionary[seed].State;
                        searchItem.UploadIds = seedsDictionary[seed].UploadIds;
                        searchItem.DownloadIds = seedsDictionary[seed].DownloadIds;

                        using (BufferStream stream = new BufferStream(_bufferManager))
                        {
                            stream.Write(BitConverter.GetBytes(seed.Length), 0, 8);
                            stream.Write(BitConverter.GetBytes(seed.Rank), 0, 4);
                            if (seed.Key != null) stream.Write(BitConverter.GetBytes((int)seed.Key.HashAlgorithm), 0, 4);
                            if (seed.Key != null && seed.Key.Hash != null) stream.Write(seed.Key.Hash, 0, seed.Key.Hash.Length);
                            stream.Write(BitConverter.GetBytes((int)seed.CompressionAlgorithm), 0, 4);
                            stream.Write(BitConverter.GetBytes((int)seed.CryptoAlgorithm), 0, 4);
                            if (seed.CryptoKey != null) stream.Write(seed.CryptoKey, 0, seed.CryptoKey.Length);

                            stream.Seek(0, SeekOrigin.Begin);

                            searchItem.Hash = NetworkConverter.ToHexString(Sha512.ComputeHash(stream));
                        }
                    }

                    searchItems.Add(searchItem);
                }

                sw.Stop();
                Debug.WriteLine("Search {0}", sw.ElapsedMilliseconds);

                Random random = new Random();
                _searchingCache = searchItems.OrderBy(n => random.Next()).Take(1000000).ToList();

                _updateStopwatch.Restart();

                return _searchingCache;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return new SearchListViewItem[0];
        }

        class SeedsAndSearchState
        {
            private List<Seed> _seeds = new List<Seed>();

            public SearchState State { get; set; }
            public List<Seed> Seeds { get { return _seeds; } }

            public List<int> DownloadIds { get; set; }
            public List<int> UploadIds { get; set; }
        }

        class SeedHashEqualityComparer : IEqualityComparer<Seed>
        {
            public bool Equals(Seed x, Seed y)
            {
                if (x == null && y == null) return true;
                if ((x == null) != (y == null)) return false;
                if (object.ReferenceEquals(x, y)) return true;

                if (x.Length != y.Length
                    //|| ((x.Keywords == null) != (y.Keywords == null))
                    //|| x.CreationTime != y.CreationTime
                    //|| x.Name != y.Name
                    //|| x.Comment != y.Comment
                    || x.Rank != y.Rank

                    || x.Key != y.Key

                    || x.CompressionAlgorithm != y.CompressionAlgorithm

                    || x.CryptoAlgorithm != y.CryptoAlgorithm
                    || ((x.CryptoKey == null) != (y.CryptoKey == null)))

                //|| x.Certificate != y.Certificate)
                {
                    return false;
                }

                //if (x.Keywords != null && y.Keywords != null)
                //{
                //    if (!Collection.Equals(x.Keywords, y.Keywords)) return false;
                //}

                if (x.CryptoKey != null && y.CryptoKey != null)
                {
                    if (!Collection.Equals(x.CryptoKey, y.CryptoKey)) return false;
                }

                return true;
            }

            public int GetHashCode(Seed obj)
            {
                if (obj == null) return 0;
                else if (obj.Key == null) return 0;
                else return obj.Key.GetHashCode();
            }
        }

        private void Update()
        {
            Settings.Instance.CacheControl_SearchTreeItem = _treeViewItem.Value;

            _treeView_SelectedItemChanged(this, null);
            _treeViewItem.Sort();
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
                searchTreeItem.SearchItem.SearchNameCollection.Add(new SearchContains<string>()
                {
                    Contains = true,
                    Value = _textBox.Text
                });

                selectTreeViewItem.Value.Items.Add(searchTreeItem);

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
                if (_startPoint.X == -1 && _startPoint.Y == -1) return;

                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (_treeViewItem == _treeView.SelectedItem) return;

                    DataObject data = new DataObject("item", _treeView.SelectedItem);
                    DragDrop.DoDragDrop(_treeView, data, DragDropEffects.Move);
                }
            }
        }

        private void _treeView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("item"))
            {
                var s = e.Data.GetData("item") as SearchTreeViewItem;
                var t = _treeView.GetCurrentItem(e.GetPosition) as SearchTreeViewItem;
                if (t == null || s == t
                    || t.Value.Items.Any(n => object.ReferenceEquals(n, s.Value))) return;

                if (_treeViewItem.GetLineage(t).OfType<SearchTreeViewItem>().Any(n => object.ReferenceEquals(n, s))) return;

                var list = _treeViewItem.GetLineage(s).OfType<SearchTreeViewItem>().ToList();

                t.IsSelected = true;

                var tItems = list[list.Count - 2].Value.Items.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                list[list.Count - 2].Value.Items.Clear();
                list[list.Count - 2].Value.Items.AddRange(tItems);

                t.Value.Items.Add(s.Value);

                list[list.Count - 2].Update();
                t.Update();

                this.Update();
            }
        }

        private void _treeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        void _treeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _treeView.GetCurrentItem(e.GetPosition) as SearchTreeViewItem;
            if (item == null)
            {
                _startPoint = new Point(-1, -1);

                return;
            }

            if (item.IsSelected == true)
            {
                _startPoint = e.GetPosition(null);
                _treeView_SelectedItemChanged(null, null);
            }
        }

        private void _treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = new Point(-1, -1);
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
            _refresh = true;
        }

        private void _treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _startPoint = new Point(-1, -1);

            if (_refresh)
            {
                _treeViewExportMenuItem.IsEnabled = false;

                return;
            }

            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            _treeViewDeleteMenuItem.IsEnabled = !(selectTreeViewItem == _treeViewItem);
            _treeViewCutMenuItem.IsEnabled = !(selectTreeViewItem == _treeViewItem);
            _treeViewExportMenuItem.IsEnabled = true;

            {
                var searchTreeItems = Clipboard.GetSearchTreeItems();

                _treeViewPasteMenuItem.IsEnabled = (searchTreeItems.Count() > 0) ? true : false;
            }
        }

        private void _treeViewNewMenuItem_Click(object sender, RoutedEventArgs e)
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
                selectTreeViewItem.Value.Items.Add(searchTreeItem);

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _treeViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            var searchItem = selectTreeViewItem.Value.SearchItem;
            SearchItemEditWindow window = new SearchItemEditWindow(ref searchItem);
            window.Owner = _mainWindow;
            window.ShowDialog();

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _treeViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;
            
            var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<SearchTreeViewItem>().ToList();

            list[list.Count - 2].IsSelected = true;

            list[list.Count - 2].Value.Items.Remove(selectTreeViewItem.Value);
            list[list.Count - 2].Update();

            this.Update();
        }

        private void _treeViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;
            
            Clipboard.SetSearchTreeItems(new List<SearchTreeItem>() { selectTreeViewItem.Value });

            var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<SearchTreeViewItem>().ToList();

            list[list.Count - 2].IsSelected = true;

            list[list.Count - 2].Value.Items.Remove(selectTreeViewItem.Value);
            list[list.Count - 2].Update();

            this.Update();
        }

        private void _treeViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetSearchTreeItems(new List<SearchTreeItem>() { selectTreeViewItem.Value });
        }

        private void _treeViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var searchTreeitem in Clipboard.GetSearchTreeItems())
            {
                selectTreeViewItem.Value.Items.Add(searchTreeitem);
            }

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _treeViewExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            Box box = new Box();
            box.Name = selectTreeViewItem.Value.SearchItem.Name;
            box.CreationTime = DateTime.UtcNow;

            foreach (var seed in _listView.Items.OfType<SearchListViewItem>().Select(n => n.Value))
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

            _recache = true;
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_refresh)
            {
                _listViewEditMenuItem.IsEnabled = false;
                _listViewCopyMenuItem.IsEnabled = false;
                _listViewCopyInfoMenuItem.IsEnabled = false;
                _listViewDeleteCacheMenuItem.IsEnabled = false;
                _listViewDeleteShareMenuItem.IsEnabled = false;
                _listViewDeleteDownloadHistoryMenuItem.IsEnabled = false;
                _listViewDeleteUploadHistoryMenuItem.IsEnabled = false;
                _listViewFilterMenuItem.IsEnabled = false;
                _listViewSearchMenuItem.IsEnabled = false;
                _listViewDownloadMenuItem.IsEnabled = false;

                return;
            }

            var selectItems = _listView.SelectedItems;

            _listViewEditMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewCopyInfoMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewFilterMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewSearchMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewDownloadMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            if (!_listViewDeleteMenuItem_IsEnabled) _listViewDeleteMenuItem.IsEnabled = false;
            else _listViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            if (_listViewDeleteMenuItem.IsEnabled)
            {
                if (!_listViewDeleteCacheMenuItem_IsEnabled) _listViewDeleteCacheMenuItem.IsEnabled = false;
                else _listViewDeleteCacheMenuItem.IsEnabled = selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Cache));
                if (!_listViewDeleteShareMenuItem_IsEnabled) _listViewDeleteShareMenuItem.IsEnabled = false;
                else _listViewDeleteShareMenuItem.IsEnabled = selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Share));
                if (!_listViewDeleteDownloadMenuItem_IsEnabled) _listViewDeleteDownloadMenuItem.IsEnabled = false;
                else _listViewDeleteDownloadMenuItem.IsEnabled = selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Downloading));
                if (!_listViewDeleteUploadMenuItem_IsEnabled) _listViewDeleteUploadMenuItem.IsEnabled = false;
                else _listViewDeleteUploadMenuItem.IsEnabled = selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Uploading));
                if (!_listViewDeleteDownloadHistoryMenuItem_IsEnabled) _listViewDeleteDownloadHistoryMenuItem.IsEnabled = false;
                else _listViewDeleteDownloadHistoryMenuItem.IsEnabled = selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Downloaded));
                if (!_listViewDeleteUploadHistoryMenuItem_IsEnabled) _listViewDeleteUploadHistoryMenuItem.IsEnabled = false;
                else _listViewDeleteUploadHistoryMenuItem.IsEnabled = selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Uploaded));
            }
            else
            {
                _listViewDeleteCacheMenuItem.IsEnabled = false;
                _listViewDeleteShareMenuItem.IsEnabled = false;
                _listViewDeleteDownloadMenuItem.IsEnabled = false;
                _listViewDeleteUploadMenuItem.IsEnabled = false;
                _listViewDeleteDownloadHistoryMenuItem.IsEnabled = false;
                _listViewDeleteUploadHistoryMenuItem.IsEnabled = false;
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
                _recache = true;

                this.Update();
            }
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var seeds = _listView.SelectedItems.OfType<SearchListViewItem>().Select(n => n.Value);

            Clipboard.SetSeeds(seeds);
        }

        private void _listViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var sb = new StringBuilder();

            foreach (var seed in selectSearchListViewItems.Cast<SearchListViewItem>().Select(n => n.Value))
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

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in list)
                    {
                        _amoebaManager.RemoveCacheSeed(item);
                    }

                    foreach (var item in list)
                    {
                        _amoebaManager.RemoveShareSeed(item);
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

                    _recache = true;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            this.Update();
                        }
                        catch (Exception)
                        {

                        }
                    }), null);
                }
                catch (Exception)
                {

                }
                finally
                {
                    _listViewDeleteMenuItem_IsEnabled = true;
                }
            }));
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

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in list)
                    {
                        _amoebaManager.RemoveCacheSeed(item);
                    }

                    _recache = true;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            this.Update();
                        }
                        catch (Exception)
                        {

                        }
                    }), null);
                }
                catch (Exception)
                {

                }
                finally
                {
                    _listViewDeleteCacheMenuItem_IsEnabled = true;
                }
            }));
        }

        volatile bool _listViewDeleteShareMenuItem_IsEnabled = true;

        private void _listViewDeleteShareMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var list = new HashSet<Seed>();

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Value == null || !item.State.HasFlag(SearchState.Share)) continue;

                list.Add(item.Value);
            }

            if (list.Count == 0) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Cache", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _listViewDeleteShareMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in list)
                    {
                        _amoebaManager.RemoveShareSeed(item);
                    }

                    _recache = true;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            this.Update();
                        }
                        catch (Exception)
                        {

                        }
                    }), null);
                }
                catch (Exception)
                {

                }
                finally
                {
                    _listViewDeleteShareMenuItem_IsEnabled = true;
                }
            }));
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

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in list)
                    {
                        _amoebaManager.RemoveDownload(item);
                    }

                    _recache = true;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            this.Update();
                        }
                        catch (Exception)
                        {

                        }
                    }), null);
                }
                catch (Exception)
                {

                }
                finally
                {
                    _listViewDeleteDownloadMenuItem_IsEnabled = true;
                }
            }));
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

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in list)
                    {
                        _amoebaManager.RemoveUpload(item);
                    }

                    _recache = true;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            this.Update();
                        }
                        catch (Exception)
                        {

                        }
                    }), null);
                }
                catch (Exception)
                {

                }
                finally
                {
                    _listViewDeleteUploadMenuItem_IsEnabled = true;
                }
            }));
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

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
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

                    _recache = true;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            this.Update();
                        }
                        catch (Exception)
                        {

                        }
                    }), null);
                }
                catch (Exception)
                {

                }
                finally
                {
                    _listViewDeleteDownloadHistoryMenuItem_IsEnabled = true;
                }
            }));
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

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
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

                    _recache = true;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            this.Update();
                        }
                        catch (Exception)
                        {

                        }
                    }), null);
                }
                catch (Exception)
                {

                }
                finally
                {
                    _listViewDeleteUploadHistoryMenuItem_IsEnabled = true;
                }
            }));
        }

        private void _listViewDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                _amoebaManager.Download(item.Value, 3);
            }

            _recache = true;
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

                var item = new SearchContains<string>()
                {
                    Contains = true,
                    Value = signature,
                };

                searchTreeItem.SearchItem.Name = string.Format("Signature - \"{0}\"", signature);
                searchTreeItem.SearchItem.SearchSignatureCollection.Add(item);

                if (selectTreeViewItem.Value.Items.Any(n => n.SearchItem.Name == searchTreeItem.SearchItem.Name)) continue;
                selectTreeViewItem.Value.Items.Add(searchTreeItem);

                selectTreeViewItem.Update();
            }

            this.Update();
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

                    var item = new SearchContains<string>()
                    {
                        Contains = true,
                        Value = keyword,
                    };

                    searchTreeItem.SearchItem.Name = string.Format("Keyword - \"{0}\"", keyword);
                    searchTreeItem.SearchItem.SearchKeywordCollection.Add(item);

                    if (selectTreeViewItem.Value.Items.Any(n => n.SearchItem.Name == searchTreeItem.SearchItem.Name)) continue;
                    selectTreeViewItem.Value.Items.Add(searchTreeItem);

                    selectTreeViewItem.Update();
                }
            }

            this.Update();
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

                var item = new SearchContains<SearchRange<DateTime>>()
                {
                    Contains = true,
                    Value = new SearchRange<DateTime>() { Min = listItem.Value.CreationTime },
                };

                searchTreeItem.SearchItem.Name = string.Format("CreationTime - \"{0}\"", listItem.Value.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo));
                searchTreeItem.SearchItem.SearchCreationTimeRangeCollection.Add(item);

                if (selectTreeViewItem.Value.Items.Any(n => n.SearchItem.Name == searchTreeItem.SearchItem.Name)) continue;
                selectTreeViewItem.Value.Items.Add(searchTreeItem);

                selectTreeViewItem.Update();
            }

            this.Update();
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

                var item = new SearchContains<string>()
                {
                    Contains = false,
                    Value = listItem.Name,
                };

                if (selectTreeViewItem.Value.SearchItem.SearchNameCollection.Contains(item)) continue;
                selectTreeViewItem.Value.SearchItem.SearchNameCollection.Add(item);
            }

            _recache = true;

            this.Update();
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

                var item = new SearchContains<string>()
                {
                    Contains = false,
                    Value = signature,
                };

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

                    var item = new SearchContains<string>()
                    {
                        Contains = false,
                        Value = keyword,
                    };

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
                var item = new SearchContains<SearchRange<DateTime>>()
                {
                    Contains = false,
                    Value = new SearchRange<DateTime>() { Min = listItem.Value.CreationTime },
                };

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

                var item = new SearchContains<Seed>()
                {
                    Contains = false,
                    Value = listitem.Value
                };

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

        private void Sort()
        {
            this.GridViewColumnHeaderClickedHandler(null, null);
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                var item = e.OriginalSource as GridViewColumnHeader;
                if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

                string headerClicked = item.Column.Header as string;
                if (headerClicked == null) return;

                ListSortDirection direction;

                if (headerClicked != Settings.Instance.CacheControl_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.CacheControl_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                Settings.Instance.CacheControl_LastHeaderClicked = headerClicked;
                Settings.Instance.CacheControl_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.CacheControl_LastHeaderClicked != null)
                {
                    Sort(Settings.Instance.CacheControl_LastHeaderClicked, Settings.Instance.CacheControl_ListSortDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.CacheControl_Name)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Name", direction));
            }
            else if (sortBy == LanguagesManager.Instance.CacheControl_Signature)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Signature", direction));
            }
            else if (sortBy == LanguagesManager.Instance.CacheControl_Length)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Length", direction));
            }
            else if (sortBy == LanguagesManager.Instance.CacheControl_Keywords)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Keywords", direction));
            }
            else if (sortBy == LanguagesManager.Instance.CacheControl_CreationTime)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("CreationTime", direction));
            }
            else if (sortBy == LanguagesManager.Instance.CacheControl_Comment)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Comment", direction));
            }
            else if (sortBy == LanguagesManager.Instance.CacheControl_State)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("State", direction));
            }
            else if (sortBy == LanguagesManager.Instance.CacheControl_Hash)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Hash", direction));
            }

            _listView.Items.SortDescriptions.Add(new SortDescription("Index", direction));
        }

        #endregion

        private class SearchListViewItem
        {
            public int Index { get { return this.Name.GetHashCode(); } }
            public string Name { get; set; }
            public string Signature { get; set; }
            public string Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public long Length { get; set; }
            public string Comment { get; set; }
            public string Hash { get; set; }
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
                    || this.Comment != other.Comment
                    || this.Hash != other.Hash
                    || this.Value != other.Value
                    || this.State != other.State

                    || (this.Seeds == null) != (other.Seeds == null)
                    || (this.DownloadIds == null) != (other.DownloadIds == null)
                    || (this.UploadIds == null) != (other.UploadIds == null))
                {
                    return false;
                }

                if (this.Seeds != null && other.Seeds != null && !Collection.Equals(this.Seeds, other.Seeds)) return false;
                if (this.DownloadIds != null && other.DownloadIds != null && !Collection.Equals(this.DownloadIds, other.DownloadIds)) return false;
                if (this.UploadIds != null && other.UploadIds != null && !Collection.Equals(this.UploadIds, other.UploadIds)) return false;

                return true;
            }
        }

        private void Execute_New(object sender, ExecutedRoutedEventArgs e)
        {
            _treeViewNewMenuItem_Click(null, null);
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                _treeViewDeleteMenuItem_Click(null, null);
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
                _treeViewCopyMenuItem_Click(null, null);
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
                _treeViewCutMenuItem_Click(null, null);
            }
            else
            {

            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            _treeViewPasteMenuItem_Click(null, null);
        }

        private void Execute_Search(object sender, ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(24);
            _searchTextBox.Focus();
        }
    }

    class SearchTreeViewItem : TreeViewItem
    {
        private int _hit;
        private SearchTreeItem _value;
        private ObservableCollection<SearchTreeViewItem> _listViewItemCollection = new ObservableCollection<SearchTreeViewItem>();

        public SearchTreeViewItem()
            : base()
        {
            this.Value = new SearchTreeItem()
            {
                SearchItem = new SearchItem()
                {
                    Name = "",
                },
            };

            base.ItemsSource = _listViewItemCollection;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        public SearchTreeViewItem(SearchTreeItem searchTreeItem)
            : base()
        {
            this.Value = searchTreeItem;

            base.ItemsSource = _listViewItemCollection;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        protected override void OnExpanded(RoutedEventArgs e)
        {
            base.OnExpanded(e);

            this.Value.IsExpanded = true;
        }

        protected override void OnCollapsed(RoutedEventArgs e)
        {
            base.OnCollapsed(e);

            this.Value.IsExpanded = false;
        }

        public void Update()
        {
            base.Header = string.Format("{0} ({1})", _value.SearchItem.Name, _hit);

            base.IsExpanded = this.Value.IsExpanded;

            List<SearchTreeViewItem> list = new List<SearchTreeViewItem>();

            foreach (var item in this.Value.Items)
            {
                list.Add(new SearchTreeViewItem(item));
            }

            foreach (var item in _listViewItemCollection.OfType<SearchTreeViewItem>().ToArray())
            {
                if (!list.Any(n => object.ReferenceEquals(n.Value.SearchItem, item.Value.SearchItem)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in list)
            {
                if (!_listViewItemCollection.OfType<SearchTreeViewItem>().Any(n => object.ReferenceEquals(n.Value.SearchItem, item.Value.SearchItem)))
                {
                    _listViewItemCollection.Add(item);
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.OfType<SearchTreeViewItem>().ToList();

            list.Sort(delegate(SearchTreeViewItem x, SearchTreeViewItem y)
            {
                int c = x.Value.SearchItem.Name.CompareTo(y.Value.SearchItem.Name);
                if (c != 0) return c;
                c = x.Hit.CompareTo(y.Hit);
                if (c != 0) return c;

                return x.GetHashCode().CompareTo(y.GetHashCode());
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _listViewItemCollection.IndexOf(list[i]);

                if (i != o) _listViewItemCollection.Move(o, i);
            }

            foreach (var item in this.Items.OfType<SearchTreeViewItem>())
            {
                item.Sort();
            }
        }

        public SearchTreeItem Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                this.Update();
            }
        }

        public int Hit
        {
            get
            {
                return _hit;
            }
            set
            {
                _hit = value;

                this.Update();
            }
        }
    }

    [DataContract(Name = "SearchTreeItem", Namespace = "http://Amoeba/Windows")]
    class SearchTreeItem : IDeepCloneable<SearchTreeItem>, IThisLock
    {
        private SearchItem _searchItem;
        private List<SearchTreeItem> _items;
        private bool _isExpanded = true;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "SearchItem")]
        public SearchItem SearchItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _searchItem;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _searchItem = value;
                }
            }
        }

        [DataMember(Name = "Items")]
        public List<SearchTreeItem> Items
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_items == null)
                        _items = new List<SearchTreeItem>();

                    return _items;
                }
            }
        }

        [DataMember(Name = "IsExpanded")]
        public bool IsExpanded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isExpanded;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isExpanded = value;
                }
            }
        }

        #region IDeepClone<SearchTreeItem>

        public SearchTreeItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SearchTreeItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchTreeItem)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }

    [DataContract(Name = "SearchItem", Namespace = "http://Amoeba/Windows")]
    class SearchItem : IDeepCloneable<SearchItem>, IThisLock
    {
        private string _name;
        private LockedList<SearchContains<string>> _searchNameCollection;
        private LockedList<SearchContains<SearchRegex>> _searchNameRegexCollection;
        private LockedList<SearchContains<string>> _searchSignatureCollection;
        private LockedList<SearchContains<string>> _searchKeywordCollection;
        private LockedList<SearchContains<SearchRange<DateTime>>> _searchCreationTimeRangeCollection;
        private LockedList<SearchContains<SearchRange<long>>> _searchLengthRangeCollection;
        private LockedList<SearchContains<Seed>> _searchSeedCollection;
        private LockedList<SearchContains<SearchState>> _searchStateCollection;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Name")]
        public string Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _name;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _name = value;
                }
            }
        }

        [DataMember(Name = "SearchNameCollection")]
        public LockedList<SearchContains<string>> SearchNameCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchNameCollection == null)
                        _searchNameCollection = new LockedList<SearchContains<string>>();

                    return _searchNameCollection;
                }
            }
        }

        [DataMember(Name = "SearchNameRegexCollection")]
        public LockedList<SearchContains<SearchRegex>> SearchNameRegexCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchNameRegexCollection == null)
                        _searchNameRegexCollection = new LockedList<SearchContains<SearchRegex>>();

                    return _searchNameRegexCollection;
                }
            }
        }

        [DataMember(Name = "SearchSignatureCollection")]
        public LockedList<SearchContains<string>> SearchSignatureCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchSignatureCollection == null)
                        _searchSignatureCollection = new LockedList<SearchContains<string>>();

                    return _searchSignatureCollection;
                }
            }
        }

        [DataMember(Name = "SearchKeywordCollection")]
        public LockedList<SearchContains<string>> SearchKeywordCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchKeywordCollection == null)
                        _searchKeywordCollection = new LockedList<SearchContains<string>>();

                    return _searchKeywordCollection;
                }
            }
        }

        [DataMember(Name = "SearchCreationTimeRangeCollection")]
        public LockedList<SearchContains<SearchRange<DateTime>>> SearchCreationTimeRangeCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchCreationTimeRangeCollection == null)
                        _searchCreationTimeRangeCollection = new LockedList<SearchContains<SearchRange<DateTime>>>();

                    return _searchCreationTimeRangeCollection;
                }
            }
        }

        [DataMember(Name = "SearchLengthRangeCollection")]
        public LockedList<SearchContains<SearchRange<long>>> SearchLengthRangeCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchLengthRangeCollection == null)
                        _searchLengthRangeCollection = new LockedList<SearchContains<SearchRange<long>>>();

                    return _searchLengthRangeCollection;
                }
            }
        }

        [DataMember(Name = "SearchSeedCollection")]
        public LockedList<SearchContains<Seed>> SearchSeedCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchSeedCollection == null)
                        _searchSeedCollection = new LockedList<SearchContains<Seed>>();

                    return _searchSeedCollection;
                }
            }
        }

        [DataMember(Name = "SearchStateCollection")]
        public LockedList<SearchContains<SearchState>> SearchStateCollection
        {
            get
            {
                lock (this.ThisLock)
                {

                    if (_searchStateCollection == null)
                        _searchStateCollection = new LockedList<SearchContains<SearchState>>();

                    return _searchStateCollection;
                }
            }
        }

        public override string ToString()
        {
            lock (this.ThisLock)
            {
                return string.Format("Name = {0}", this.Name);
            }
        }

        #region IDeepClone<SearchItem>

        public SearchItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SearchItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchItem)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }

    [Flags]
    [DataContract(Name = "SearchState", Namespace = "http://Amoeba/Windows")]
    enum SearchState
    {
        [EnumMember(Value = "Cache")]
        Cache = 0x1,

        [EnumMember(Value = "Share")]
        Share = 0x2,

        [EnumMember(Value = "Uploading")]
        Uploading = 0x4,

        [EnumMember(Value = "Uploaded")]
        Uploaded = 0x8,

        [EnumMember(Value = "Downloading")]
        Downloading = 0x10,

        [EnumMember(Value = "Downloaded")]
        Downloaded = 0x20,
    }

    [DataContract(Name = "SearchContains", Namespace = "http://Amoeba/Windows")]
    class SearchContains<T> : IEquatable<SearchContains<T>>, IDeepCloneable<SearchContains<T>>, IThisLock
    {
        private bool _contains;
        private T _value;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Contains")]
        public bool Contains
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _contains;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _contains = value;
                }
            }
        }

        [DataMember(Name = "Value")]
        public T Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _value;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _value = value;
                }
            }
        }

        public override int GetHashCode()
        {
            lock (this.ThisLock)
            {
                return this.Value.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchContains<T>)) return false;

            return this.Equals((SearchContains<T>)obj);
        }

        public bool Equals(SearchContains<T> other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if ((this.Contains != other.Contains)
                || (!this.Value.Equals(other.Value)))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            lock (this.ThisLock)
            {
                return string.Format("{0} {1}", this.Contains, this.Value);
            }
        }

        #region IDeepClone<SearchContains<T>>

        public SearchContains<T> DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SearchContains<T>));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchContains<T>)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }

    [DataContract(Name = "SearchRegex", Namespace = "http://Amoeba/Windows")]
    class SearchRegex : IEquatable<SearchRegex>, IDeepCloneable<SearchRegex>, IThisLock
    {
        private string _value;
        private bool _isIgnoreCase;

        private Regex _regex;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Value")]
        public string Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _value;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _value = value;

                    this.RegexUpdate();
                }
            }
        }

        [DataMember(Name = "IsIgnoreCase")]
        public bool IsIgnoreCase
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isIgnoreCase;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isIgnoreCase = value;

                    this.RegexUpdate();
                }
            }
        }

        private void RegexUpdate()
        {
            lock (this.ThisLock)
            {
                var o = RegexOptions.Compiled | RegexOptions.Singleline;
                if (_isIgnoreCase) o |= RegexOptions.IgnoreCase;

                if (_value != null) _regex = new Regex(_value, o);
                else _regex = null;
            }
        }

        public bool IsMatch(string value)
        {
            lock (this.ThisLock)
            {
                if (_regex == null) return false;

                return _regex.IsMatch(value);
            }
        }

        public override int GetHashCode()
        {
            lock (this.ThisLock)
            {
                return this.Value.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchRegex)) return false;

            return this.Equals((SearchRegex)obj);
        }

        public bool Equals(SearchRegex other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if ((this.IsIgnoreCase != other.IsIgnoreCase)
                || (this.Value != other.Value))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            lock (this.ThisLock)
            {
                return string.Format("{0} {1}", this.IsIgnoreCase, this.Value);
            }
        }

        #region IDeepClone<SearchRegex>

        public SearchRegex DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SearchRegex));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchRegex)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }

    [DataContract(Name = "SearchRange", Namespace = "http://Amoeba/Windows")]
    class SearchRange<T> : IEquatable<SearchRange<T>>, IDeepCloneable<SearchRange<T>>, IThisLock
        where T : IComparable
    {
        T _max;
        T _min;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Max")]
        public T Max
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _max;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _max = value;
                    _min = (_min.CompareTo(_max) > 0) ? _max : _min;
                }
            }
        }

        [DataMember(Name = "Min")]
        public T Min
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _min;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _min = value;
                    _max = (_max.CompareTo(_min) < 0) ? _min : _max;
                }
            }
        }

        public bool Verify(T value)
        {
            lock (this.ThisLock)
            {
                if (value.CompareTo(this.Min) < 0 || value.CompareTo(this.Max) > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public override int GetHashCode()
        {
            lock (this.ThisLock)
            {
                return this.Min.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchRange<T>)) return false;

            return this.Equals((SearchRange<T>)obj);
        }

        public bool Equals(SearchRange<T> other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if ((!this.Min.Equals(other.Min))
                || (!this.Max.Equals(other.Max)))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            lock (this.ThisLock)
            {
                return string.Format("Max = {0}, Min = {1}", this.Max, this.Min);
            }
        }

        #region IDeepClone<SearchRange<T>>

        public SearchRange<T> DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SearchRange<T>));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchRange<T>)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }
}
