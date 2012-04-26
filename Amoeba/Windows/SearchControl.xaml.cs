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
using Library.Net.Amoeba;

namespace Amoeba.Windows
{
    /// <summary>
    /// SearchControl.xaml の相互作用ロジック
    /// </summary>
    partial class SearchControl : UserControl
    {
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollection<SearchListViewItem> _searchListViewItemCollection;
        private Thread _searchThread = null;
        private volatile bool _refresh = false;
        private volatile bool _recache = false;

        private volatile List<SearchListViewItem> _searchingCache = new List<SearchListViewItem>();
        private Stopwatch _updateStopwatch = new Stopwatch();
        private Dictionary<Seed, byte[]> _seedHash = new Dictionary<Seed, byte[]>(new SeedReferenceEqualityComparer());
        private Dictionary<Seed, byte[]> _seedBinaryHash = new Dictionary<Seed, byte[]>(new SeedReferenceEqualityComparer());

        public SearchControl(MainWindow mainWindow, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            _searchListViewItemCollection = new ObservableCollection<SearchListViewItem>();

            InitializeComponent();

            _searchListView.ItemsSource = _searchListViewItemCollection;
            _searchTreeViewItem.Value = Settings.Instance.SearchControl_SearchTreeItem;

            try
            {
                _searchTreeViewItem.IsSelected = true;
            }
            catch (Exception)
            {

            }

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                _recache = true;
            };

            _amoebaManager.GetFilterSeedsEvent = (object sender, IEnumerable<Seed> seeds) =>
            {
                HashSet<SearchListViewItem> searchItems = new HashSet<SearchListViewItem>();

                foreach (var item in seeds)
                {
                    var searchItem = new SearchListViewItem();

                    searchItem.Name = item.Name;
                    searchItem.Signature = MessageConverter.ToSignatureString(item.Certificate);
                    searchItem.Keywords = item.Keywords.Where(n => n != null || n.Value != null).Select(m => m.Value);
                    searchItem.Value = item;

                    searchItems.Add(searchItem);
                }

                SearchControl.Filter(ref searchItems, _searchTreeViewItem.Value);

                return searchItems.Select(n => n.Value);
            };

            _searchThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    for (; ; )
                    {
                        Thread.Sleep(100);
                        if (!_refresh) continue;
                        _refresh = false;

                        SearchTreeViewItem selectSearchTreeViewItem = null;

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
                        }), null);

                        if (selectSearchTreeViewItem == null) continue;

                        HashSet<SearchListViewItem> newList = new HashSet<SearchListViewItem>(this.GetSearchListViewItems());
                        List<SearchTreeViewItem> searchTreeViewItems = new List<SearchTreeViewItem>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            searchTreeViewItems.AddRange(_searchTreeViewItem.GetLineage(selectSearchTreeViewItem).OfType<SearchTreeViewItem>());
                        }), null);

                        foreach (var searchTreeViewItem in searchTreeViewItems)
                        {
                            SearchControl.Filter(ref newList, searchTreeViewItem.Value);

                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                            {
                                searchTreeViewItem.Hit = newList.Count;
                                searchTreeViewItem.Update();
                            }), null);
                        }

                        {
                            List<SearchListViewItem> oldList = null;

                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                            {
                                oldList = _searchListViewItemCollection.ToList();
                            }), null);

                            Dictionary<Seed, SearchState> tempList1 = new Dictionary<Seed, SearchState>(new SeedReferenceEqualityComparer());

                            foreach (var item in oldList)
                            {
                                tempList1.Add(item.Value, item.State);
                            }

                            Dictionary<Seed, SearchState> tempList2 = new Dictionary<Seed, SearchState>(new SeedReferenceEqualityComparer());

                            foreach (var item in newList)
                            {
                                tempList2.Add(item.Value, item.State);
                            }

                            var removeList = new List<SearchListViewItem>();
                            var addList = new List<SearchListViewItem>();

                            foreach (var item in oldList)
                            {
                                if (!tempList2.ContainsKey(item.Value) || tempList2[item.Value] != item.State)
                                {
                                    removeList.Add(item);
                                }
                            }

                            foreach (var item in newList)
                            {
                                if (!tempList1.ContainsKey(item.Value) || tempList1[item.Value] != item.State)
                                {
                                    addList.Add(item);
                                }
                            }

                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                            {
                                bool sortFlag = false;

                                if (removeList.Count > 100)
                                {
                                    sortFlag = true;

                                    _searchListViewItemCollection.Clear();

                                    foreach (var item in newList)
                                    {
                                        _searchListViewItemCollection.Add(item);
                                    }
                                }
                                else
                                {
                                    if (addList.Count != 0) sortFlag = true;
                                    if (removeList.Count != 0) sortFlag = true;

                                    foreach (var item in addList)
                                    {
                                        _searchListViewItemCollection.Add(item);
                                    }

                                    foreach (var item in removeList)
                                    {
                                        _searchListViewItemCollection.Remove(item);
                                    }
                                }

                                if (sortFlag && _searchListViewItemCollection.Count < 10000) this.Sort();

                                if (App.SelectTab == "Search")
                                    _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectSearchTreeViewItem.Value.SearchItem.Name);
                            }), null);
                        }
                    }
                }
                catch (Exception)
                {

                }
            }));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "SearchThread";
            _searchThread.Start();
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

                Dictionary<Seed, SearchState> seedsDictionary = new Dictionary<Seed, SearchState>(new SeedReferenceEqualityComparer());

                {
                    if (!Settings.Instance.Global_SearchFilterSettings_State.HasFlag(SearchState.Searching))
                    {
                        foreach (var seed in _amoebaManager.Seeds)
                        {
                            seedsDictionary[seed] = SearchState.Searching;
                        }
                    }

                    if (!Settings.Instance.Global_SearchFilterSettings_State.HasFlag(SearchState.Uploading))
                    {
                        foreach (var information in _amoebaManager.UploadingInformation)
                        {
                            if (information.Contains("Seed") && ((UploadState)information["State"]) != UploadState.Completed)
                            {
                                var seed = (Seed)information["Seed"];

                                if (!seedsDictionary.ContainsKey(seed))
                                {
                                    seedsDictionary[seed] = SearchState.Uploading;
                                }
                                else
                                {
                                    seedsDictionary[seed] |= SearchState.Uploading;
                                }
                            }
                        }
                    }

                    if (!Settings.Instance.Global_SearchFilterSettings_State.HasFlag(SearchState.Downloading))
                    {
                        foreach (var information in _amoebaManager.DownloadingInformation)
                        {
                            if (information.Contains("Seed") && ((DownloadState)information["State"]) != DownloadState.Completed)
                            {
                                var seed = (Seed)information["Seed"];

                                if (!seedsDictionary.ContainsKey(seed))
                                {
                                    seedsDictionary[seed] = SearchState.Downloading;
                                }
                                else
                                {
                                    seedsDictionary[seed] |= SearchState.Downloading;
                                }
                            }
                        }
                    }

                    if (!Settings.Instance.Global_SearchFilterSettings_State.HasFlag(SearchState.Uploaded))
                    {
                        foreach (var seed in _amoebaManager.UploadedSeeds)
                        {
                            if (!seedsDictionary.ContainsKey(seed))
                            {
                                seedsDictionary[seed] = SearchState.Uploaded;
                            }
                            else
                            {
                                seedsDictionary[seed] |= SearchState.Uploaded;
                            }
                        }
                    }

                    if (!Settings.Instance.Global_SearchFilterSettings_State.HasFlag(SearchState.Downloaded))
                    {
                        foreach (var seed in _amoebaManager.DownloadedSeeds)
                        {
                            if (!seedsDictionary.ContainsKey(seed))
                            {
                                seedsDictionary[seed] = SearchState.Downloaded;
                            }
                            else
                            {
                                seedsDictionary[seed] |= SearchState.Downloaded;
                            }
                        }
                    }
                }

                {
                    UTF8Encoding encoding = new UTF8Encoding(false);

                    using (var sha1 = System.Security.Cryptography.SHA1.Create())
                    {
                        foreach (var item in seedsDictionary.Keys)
                        {
                            if (!_seedHash.ContainsKey(item))
                            {
                                using (MemoryStream stream = new MemoryStream())
                                {
                                    if (item.Name != null)
                                    {
                                        var nameBuffer = encoding.GetBytes(item.Name);
                                        stream.Write(nameBuffer, 0, nameBuffer.Length);
                                    }

                                    stream.Write(BitConverter.GetBytes(item.Length), 0, 8);

                                    stream.Write(BitConverter.GetBytes(item.Rank), 0, 4);

                                    if (item.Key != null)
                                    {
                                        stream.Write(BitConverter.GetBytes((int)item.Key.HashAlgorithm), 0, 4);
                                        if (item.Key.Hash != null) stream.Write(item.Key.Hash, 0, item.Key.Hash.Length);
                                    }

                                    stream.Write(BitConverter.GetBytes((int)item.CompressionAlgorithm), 0, 4);

                                    stream.Write(BitConverter.GetBytes((int)item.CryptoAlgorithm), 0, 4);
                                    if (item.CryptoKey != null) stream.Write(item.CryptoKey, 0, item.CryptoKey.Length);

                                    if (item.Certificate != null)
                                    {
                                        stream.Write(BitConverter.GetBytes((int)item.Certificate.DigitalSignatureAlgorithm), 0, 4);
                                        if (item.Certificate.PublicKey != null) stream.Write(item.Certificate.PublicKey, 0, item.Certificate.PublicKey.Length);
                                        if (item.Certificate.Signature != null) stream.Write(item.Certificate.Signature, 0, item.Certificate.Signature.Length);
                                    }

                                    _seedHash.Add(item, sha1.ComputeHash(stream));
                                }
                            }
                        }
                    }

                    foreach (var item in _seedHash.Keys.ToArray())
                    {
                        if (!seedsDictionary.ContainsKey(item))
                        {
                            _seedHash.Remove(item);
                        }
                    }

                    using (var sha1 = System.Security.Cryptography.SHA1.Create())
                    {
                        foreach (var item in seedsDictionary.Keys)
                        {
                            if (!_seedBinaryHash.ContainsKey(item))
                            {
                                using (MemoryStream stream = new MemoryStream())
                                {
                                    stream.Write(BitConverter.GetBytes(item.Rank), 0, 4);

                                    if (item.Key != null)
                                    {
                                        stream.Write(BitConverter.GetBytes((int)item.Key.HashAlgorithm), 0, 4);
                                        if (item.Key.Hash != null) stream.Write(item.Key.Hash, 0, item.Key.Hash.Length);
                                    }

                                    stream.Write(BitConverter.GetBytes((int)item.CompressionAlgorithm), 0, 4);

                                    stream.Write(BitConverter.GetBytes((int)item.CryptoAlgorithm), 0, 4);
                                    if (item.CryptoKey != null) stream.Write(item.CryptoKey, 0, item.CryptoKey.Length);

                                    _seedBinaryHash.Add(item, sha1.ComputeHash(stream));
                                }
                            }
                        }
                    }

                    foreach (var item in _seedBinaryHash.Keys.ToArray())
                    {
                        if (!seedsDictionary.ContainsKey(item))
                        {
                            _seedBinaryHash.Remove(item);
                        }
                    }
                }

                Dictionary<Seed, SearchListViewItem> searchItems = new Dictionary<Seed, SearchListViewItem>(new CacheSeedEqualityComparer(_seedHash));
                Dictionary<Seed, SearchState> stateDic = new Dictionary<Seed, SearchState>(new CacheSeedEqualityComparer(_seedBinaryHash));

                foreach (var item in seedsDictionary)
                {
                    var seed = item.Key;

                    if (searchItems.ContainsKey(seed))
                    {
                        var target = searchItems[seed];

                        if (target.Value.CreationTime < seed.CreationTime)
                        {
                            target.CreationTime = seed.CreationTime;
                            target.Comment = seed.Comment;
                            target.Value = seed;
                        }
                    }
                    else
                    {
                        var searchItem = new SearchListViewItem();

                        searchItem.Name = seed.Name;
                        searchItem.Signature = MessageConverter.ToSignatureString(seed.Certificate);
                        searchItem.Keywords = seed.Keywords.Where(n => n != null || n.Value != null).Select(m => m.Value);
                        searchItem.CreationTime = seed.CreationTime;
                        searchItem.Length = seed.Length;
                        searchItem.Comment = seed.Comment;
                        searchItem.Value = seed;
                        searchItem.State = item.Value;

                        searchItems.Add(seed, searchItem);
                    }

                    if (!stateDic.ContainsKey(seed))
                    {
                        stateDic.Add(seed, item.Value);
                    }
                    else
                    {
                        stateDic[seed] |= item.Value;
                    }
                }

                foreach (var item in searchItems)
                {
                    item.Value.State = stateDic[item.Key];
                }

                sw.Stop();
                Debug.WriteLine("Search {0}", sw.ElapsedMilliseconds);

                Random random = new Random();
                _searchingCache = searchItems.Values.OrderBy(n => random.Next()).Take(1000000).ToList();

                _updateStopwatch.Restart();

                return _searchingCache;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return new SearchListViewItem[0];
        }

        class SeedReferenceEqualityComparer : IEqualityComparer<Seed>
        {
            public bool Equals(Seed x, Seed y)
            {
                if ((x == null) != (y == null)) return false;
                if (x == null && y == null) return true;

                return object.ReferenceEquals(x, y);
            }

            public int GetHashCode(Seed obj)
            {
                if (obj == null) return 0;
                else if (obj.Key == null) return 0;
                else return obj.Key.GetHashCode();
            }
        }

        class CacheSeedEqualityComparer : IEqualityComparer<Seed>
        {
            Dictionary<Seed, byte[]> _seedHash;

            public CacheSeedEqualityComparer(Dictionary<Seed, byte[]> seedHash)
            {
                _seedHash = seedHash;
            }

            public bool Equals(Seed x, Seed y)
            {
                if (object.ReferenceEquals(x, y)) return true;

                var xh = _seedHash[x];
                var yh = _seedHash[y];

                if (xh != null && yh != null)
                {
                    if (xh.Length != yh.Length) return false;

                    for (int i = 0; i < xh.Length; i++) if (xh[i] != yh[i]) return false;
                }

                return true;
            }

            public int GetHashCode(Seed obj)
            {
                if (obj == null) return 0;
                if (obj.Key == null) return 0;
                else return obj.Key.GetHashCode();
            }
        }

        private void Update()
        {
            Settings.Instance.SearchControl_SearchTreeItem = _searchTreeViewItem.Value;

            _searchTreeView_SelectedItemChanged(this, null);
        }

        private void _searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
                if (selectSearchTreeViewItem == null) return;
                if (_searchTextBox.Text == "") return;

                var searchTreeItem = new SearchTreeItem();
                searchTreeItem.SearchItem = new SearchItem();
                searchTreeItem.SearchItem.Name = _searchTextBox.Text;
                searchTreeItem.SearchItem.SearchNameCollection.Add(new SearchContains<string>() { Contains = true, Value = _searchTextBox.Text });

                selectSearchTreeViewItem.Value.Items.Add(searchTreeItem);

                selectSearchTreeViewItem.Update();

                _searchTextBox.Text = "";
            }
        }

        #region _searchListView

        private void _searchListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_searchListView.GetCurrentIndex(e.GetPosition) == -1)
            {
                _searchListView.SelectedItems.Clear();
            }
        }

        private void _searchListView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_searchListView.GetCurrentIndex(e.GetPosition) < 0) return;

            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                _amoebaManager.Download(item.Value, 0);
            }

            _recache = true;
        }

        private void _searchListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_refresh)
            {
                _searchListViewCopyMenuItem.IsEnabled = false;
                _searchListViewCopyInfoMenuItem.IsEnabled = false;
                _searchListViewDownloadHistoryDeleteMenuItem.IsEnabled = false;
                _searchListViewUploadHistoryDeleteMenuItem.IsEnabled = false;
                _searchListViewFilterNameMenuItem.IsEnabled = false;
                _searchListViewFilterSignatureMenuItem.IsEnabled = false;
                _searchListViewFilterSeedMenuItem.IsEnabled = false;
                _searchListViewDownloadMenuItem.IsEnabled = false;

                return;
            }

            var selectItems = _searchListView.SelectedItems;
            if (selectItems == null) return;

            _searchListViewCopyMenuItem.IsEnabled = (selectItems.Count > 0);
            _searchListViewCopyInfoMenuItem.IsEnabled = (selectItems.Count > 0);
            _searchListViewFilterNameMenuItem.IsEnabled = (selectItems.Count > 0);
            _searchListViewFilterSignatureMenuItem.IsEnabled = (selectItems.Count > 0);
            _searchListViewFilterSeedMenuItem.IsEnabled = (selectItems.Count > 0);
            _searchListViewDownloadMenuItem.IsEnabled = (selectItems.Count > 0);
            _searchListViewDownloadHistoryDeleteMenuItem.IsEnabled = selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Downloaded));
            _searchListViewUploadHistoryDeleteMenuItem.IsEnabled = selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Uploaded));
        }

        private void _searchListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var sb = new StringBuilder();

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                sb.AppendLine(AmoebaConverter.ToSeedString(item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _searchListViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var item = selectSearchListViewItems.Cast<SearchListViewItem>().FirstOrDefault();
            if (item == null) return;

            try
            {
                Clipboard.SetText(MessageConverter.ToInfoMessage(item.Value));
            }
            catch (Exception)
            {

            }
        }

        private void _searchListViewDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                _amoebaManager.Download(item.Value, 0);
            }

            _recache = true;
        }

        private void _searchListViewFilterNameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            foreach (var listItem in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (string.IsNullOrWhiteSpace(listItem.Name)) continue;

                var item = new SearchContains<string>()
                {
                    Contains = false,
                    Value = listItem.Name,
                };

                if (selectSearchTreeViewItem.Value.SearchItem.SearchNameCollection.Contains(item)) continue;
                selectSearchTreeViewItem.Value.SearchItem.SearchNameCollection.Add(item);
            }

            this.Update();
        }

        private void _searchListViewFilterSignatureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            foreach (var listItem in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (string.IsNullOrWhiteSpace(listItem.Signature)) continue;

                var item = new SearchContains<string>()
                {
                    Contains = false,
                    Value = listItem.Signature,
                };

                if (selectSearchTreeViewItem.Value.SearchItem.SearchSignatureCollection.Contains(item)) continue;
                selectSearchTreeViewItem.Value.SearchItem.SearchSignatureCollection.Add(item);
            }

            this.Update();
        }

        private void _searchListViewFilterKeywordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            foreach (var listItem in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                foreach (var keyword in listItem.Value.Keywords)
                {
                    if (string.IsNullOrWhiteSpace(keyword.Value)) continue;

                    var item = new SearchContains<string>()
                    {
                        Contains = false,
                        Value = keyword.Value,
                    };

                    if (selectSearchTreeViewItem.Value.SearchItem.SearchKeywordCollection.Contains(item)) continue;
                    selectSearchTreeViewItem.Value.SearchItem.SearchKeywordCollection.Add(item);
                }
            }

            this.Update();
        }

        private void _searchListViewFilterSeedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            foreach (var listitem in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (listitem.Value == null) continue;

                var item = new SearchContains<Seed>()
                {
                    Contains = false,
                    Value = listitem.Value
                };

                if (selectSearchTreeViewItem.Value.SearchItem.SearchSeedCollection.Contains(item)) continue;
                selectSearchTreeViewItem.Value.SearchItem.SearchSeedCollection.Add(item);
            }

            this.Update();
        }

        private void _searchListViewDownloadHistoryDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            SeedHashEqualityComparer comparer = new SeedHashEqualityComparer();

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Value == null || !item.State.HasFlag(SearchState.Downloaded)) continue;

                foreach (var seed in _amoebaManager.DownloadedSeeds.ToArray())
                {
                    if (comparer.Equals(item.Value, seed))
                    {
                        _amoebaManager.DownloadedSeeds.Remove(seed);
                    }
                }
            }

            _recache = true;
        }

        private void _searchListViewUploadHistoryDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            SeedHashEqualityComparer comparer = new SeedHashEqualityComparer();

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Value == null || !item.State.HasFlag(SearchState.Uploaded)) continue;

                foreach (var seed in _amoebaManager.UploadedSeeds.ToArray())
                {
                    if (comparer.Equals(item.Value, seed))
                    {
                        _amoebaManager.UploadedSeeds.Remove(seed);
                    }
                }
            }

            _recache = true;
        }

        #endregion

        #region _searchTreeView

        private Point _startPoint;

        private void _searchTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released)
            {
                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (e.Source.GetType() == typeof(SearchTreeViewItem))
                    {
                        if (_searchTreeViewItem == _searchTreeView.SelectedItem) return;

                        DataObject data = new DataObject("item", _searchTreeView.SelectedItem);
                        DragDrop.DoDragDrop(_searchTreeView, data, DragDropEffects.Move);
                    }
                }
            }
        }

        private void _searchTreeView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("item"))
            {
                var s = e.Data.GetData("item") as SearchTreeViewItem;
                var t = _searchTreeView.GetCurrentItem(e.GetPosition) as SearchTreeViewItem;
                if (t == null || s == t
                    || t.Value.Items.Any(n => object.ReferenceEquals(n, s.Value))) return;

                if (_searchTreeViewItem.GetLineage(t).OfType<SearchTreeViewItem>().Any(n => object.ReferenceEquals(n, s))) return;

                t.IsSelected = true;
                t.IsExpanded = true;

                var list = _searchTreeViewItem.GetLineage(s).OfType<SearchTreeViewItem>().ToList();
                list[list.Count - 2].Value.Items.Remove(s.Value);
                list[list.Count - 2].Update();

                t.Value.Items.Add(s.Value);
                t.Update();

                this.Update();
            }
        }

        private void _searchTreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _searchTreeView.GetCurrentItem(e.GetPosition) as SearchTreeViewItem;
            if (item == null) return;

            item.IsSelected = true;
        }

        private void _searchTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);

            _searchTreeView_SelectedItemChanged(null, null);
        }

        private void _searchTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
            _refresh = true;
        }

        private static void Filter(ref HashSet<SearchListViewItem> searchItems, SearchTreeItem searchTreeItem)
        {
            searchItems.IntersectWith(searchItems.ToArray().Where(searchItem =>
            {
                var x = searchTreeItem.SearchItem.SearchState;
                var y = searchItem.State;

                if (x.HasFlag(SearchState.Searching) && y.HasFlag(SearchState.Searching))
                {
                    return false;
                }
                if (x.HasFlag(SearchState.Uploading) && y.HasFlag(SearchState.Uploading))
                {
                    return false;
                }
                if (x.HasFlag(SearchState.Downloading) && y.HasFlag(SearchState.Downloading))
                {
                    return false;
                }
                if (x.HasFlag(SearchState.Uploaded) && y.HasFlag(SearchState.Uploaded))
                {
                    return false;
                }
                if (x.HasFlag(SearchState.Downloaded) && y.HasFlag(SearchState.Downloaded))
                {
                    return false;
                }

                return true;
            }));

            DateTime now = DateTime.UtcNow;

            searchItems.IntersectWith(searchItems.ToArray().Where(n =>
            {
                if (n.Value.Length == 0) return false;
                if (string.IsNullOrWhiteSpace(n.Value.Name)) return false;
                if (n.Value.Rank == 0) return false;
                if (n.Value.Keywords.Count == 0) return false;
                if ((now - n.Value.CreationTime) > new TimeSpan(3, 0, 0, 0)) return false;

                return true;
            }));

            searchItems.IntersectWith(searchItems.ToArray().Where(searchItem =>
            {
                bool flag;

                if (searchTreeItem.SearchItem.SearchLengthRangeCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchLengthRangeCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return searchContains.Value.Verify(searchItem.Value.Length);

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchCreationTimeRangeCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchCreationTimeRangeCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return searchContains.Value.Verify(searchItem.Value.CreationTime);

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchKeywordCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchKeywordCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return searchItem.Keywords.Contains(searchContains.Value);

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchSignatureCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchSignatureCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return searchItem.Signature == searchContains.Value;

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchNameCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchNameCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains)
                        {
                            return searchContains.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                .All(n => searchItem.Name.ToLower().Contains(n.ToLower()));
                        }

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchNameRegexCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchNameRegexCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return searchContains.Value.IsMatch(searchItem.Name);

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchSeedCollection.Any(n => n.Contains == true))
                {
                    SeedHashEqualityComparer comparer = new SeedHashEqualityComparer();

                    flag = searchTreeItem.SearchItem.SearchSeedCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return comparer.Equals(searchItem.Value, searchContains.Value);

                        return false;
                    });
                    if (!flag) return false;
                }

                return true;
            }));

            searchItems.ExceptWith(searchItems.ToArray().Where(searchItem =>
            {
                bool flag;

                if (searchTreeItem.SearchItem.SearchLengthRangeCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchLengthRangeCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return searchContains.Value.Verify(searchItem.Value.Length);

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchCreationTimeRangeCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchCreationTimeRangeCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return searchContains.Value.Verify(searchItem.Value.CreationTime);

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchKeywordCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchKeywordCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return searchItem.Keywords.Contains(searchContains.Value);

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchSignatureCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchSignatureCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return searchItem.Signature == searchContains.Value;

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchNameCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchNameCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains)
                        {
                            return searchContains.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                .All(n => searchItem.Name.Contains(n));
                        }

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchNameRegexCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchNameRegexCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return searchContains.Value.IsMatch(searchItem.Name);

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchSeedCollection.Any(n => n.Contains == false))
                {
                    SeedHashEqualityComparer comparer = new SeedHashEqualityComparer();

                    flag = searchTreeItem.SearchItem.SearchSeedCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return comparer.Equals(searchItem.Value, searchContains.Value);

                        return false;
                    });
                    if (flag) return true;
                }

                return false;
            }));
        }

        private void _searchTreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            _searchTreeViewDeleteMenuItem.IsEnabled = !(selectSearchTreeViewItem == _searchTreeViewItem);
            _searchTreeViewCutContextMenuItem.IsEnabled = !(selectSearchTreeViewItem == _searchTreeViewItem);

            {
                var searchTreeItems = Clipboard.GetSearchTreeItems();

                _searchTreeViewPasteContextMenuItem.IsEnabled = (searchTreeItems.Count() > 0) ? true : false;
            }
        }

        private void _searchTreeViewAddMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            var searchTreeItem = new SearchTreeItem();
            searchTreeItem.SearchItem = new SearchItem();

            var searchItem = searchTreeItem.SearchItem;
            SearchItemEditWindow window = new SearchItemEditWindow(ref searchItem);

            if (true == window.ShowDialog())
            {
                selectSearchTreeViewItem.IsExpanded = true;
                selectSearchTreeViewItem.Value.Items.Add(searchTreeItem);

                selectSearchTreeViewItem.Update();
            }

            this.Update();
        }

        private void _searchTreeViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            var searchItem = selectSearchTreeViewItem.Value.SearchItem;
            SearchItemEditWindow window = new SearchItemEditWindow(ref searchItem);
            window.ShowDialog();

            selectSearchTreeViewItem.Update();

            this.Update();
        }

        private void _searchTreeViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            if (selectSearchTreeViewItem == _searchTreeViewItem) return;

            var list = _searchTreeViewItem.GetLineage(selectSearchTreeViewItem).OfType<SearchTreeViewItem>().ToList();

            list[list.Count - 2].IsSelected = true;

            list[list.Count - 2].Value.Items.Remove(selectSearchTreeViewItem.Value);
            list[list.Count - 2].Update();

            this.Update();
        }

        private void _searchTreeViewCutContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            Clipboard.SetSearchTreeItems(new List<SearchTreeItem>() { selectSearchTreeViewItem.Value.DeepClone() });

            var list = _searchTreeViewItem.GetLineage(selectSearchTreeViewItem).OfType<SearchTreeViewItem>().ToList();

            list[list.Count - 2].IsSelected = true;

            list[list.Count - 2].Value.Items.Remove(selectSearchTreeViewItem.Value);
            list[list.Count - 2].Update();

            this.Update();
        }

        private void _searchTreeViewCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            Clipboard.SetSearchTreeItems(new List<SearchTreeItem>() { selectSearchTreeViewItem.Value.DeepClone() });
        }

        private void _searchTreeViewPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            foreach (var searchTreeitem in Clipboard.GetSearchTreeItems().Select(n => n.DeepClone()))
            {
                selectSearchTreeViewItem.Value.Items.Add(searchTreeitem);
            }

            selectSearchTreeViewItem.Update();

            this.Update();
        }

        #endregion

        #region Sort

        private void Sort()
        {
            this.GridViewColumnHeaderClickedHandler(null, null);
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                _searchListView.SelectedIndex = -1;

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

                Sort(headerClicked, direction);

                Settings.Instance.SearchControl_LastHeaderClicked = headerClicked;
                Settings.Instance.SearchControl_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.SearchControl_LastHeaderClicked != null)
                {
                    Sort(Settings.Instance.SearchControl_LastHeaderClicked, Settings.Instance.SearchControl_ListSortDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _searchListView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.SearchControl_Name)
            {
                _searchListView.Items.SortDescriptions.Add(new SortDescription("Name", direction));
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Signature)
            {
                _searchListView.Items.SortDescriptions.Add(new SortDescription("Signature", direction));
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Length)
            {
                _searchListView.Items.SortDescriptions.Add(new SortDescription("Length", direction));
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Keywords)
            {
                var view = (ListCollectionView)CollectionViewSource.GetDefaultView(_searchListView.ItemsSource);
                view.CustomSort = (IComparer)new KeywordsIComparer(direction);
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_CreationTime)
            {
                _searchListView.Items.SortDescriptions.Add(new SortDescription("CreationTime", direction));
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Comment)
            {
                _searchListView.Items.SortDescriptions.Add(new SortDescription("Comment", direction));
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_State)
            {
                _searchListView.Items.SortDescriptions.Add(new SortDescription("State", direction));
            }
        }

        private class KeywordsIComparer : IComparer
        {
            private ListSortDirection _direction;
            private int flag = 0;

            public KeywordsIComparer(ListSortDirection direction)
            {
                _direction = direction;

                flag = (_direction == ListSortDirection.Ascending) ? 1 : -1;
            }

            public int Compare(object x, object y)
            {
                var xi = x as SearchListViewItem;
                var yi = y as SearchListViewItem;

                int c = Collection.Compare<string>(xi.Keywords, yi.Keywords);
                if (c != 0) return flag * c;
                return flag * xi.GetHashCode().CompareTo(yi.GetHashCode());
            }
        }

        #endregion

        private class SearchListViewItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(string info)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
            }

            public string Name { get; set; }
            public string Signature { get; set; }
            public SearchState State{ get; set; }
            public IEnumerable<string> Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public long Length { get; set; }
            public string Comment { get; set; }
            public Seed Value { get; set; }

            public override int GetHashCode()
            {
                if (this.Value == null) return 0;
                else return this.Value.GetHashCode();
            }
        }

        class SeedEqualityComparer : IEqualityComparer<Seed>
        {
            public bool Equals(Seed x, Seed y)
            {
                if (x == null && y == null) return true;
                if ((x == null) != (y == null)) return false;
                if (object.ReferenceEquals(x, y)) return true;

                if (x.Length != y.Length
                    || ((x.Keywords == null) != (y.Keywords == null))
                    //|| x.CreationTime != y.CreationTime
                    || x.Name != y.Name
                    //|| x.Comment != y.Comment
                    || x.Rank != y.Rank

                    || x.Key != y.Key

                    || x.CompressionAlgorithm != y.CompressionAlgorithm

                    || x.CryptoAlgorithm != y.CryptoAlgorithm
                    || ((x.CryptoKey == null) != (y.CryptoKey == null))

                    || x.Certificate != y.Certificate)
                {
                    return false;
                }

                if (x.Keywords != null && y.Keywords != null)
                {
                    if (!Collection.Equals(x.Keywords, y.Keywords)) return false;
                }

                if (x.CryptoKey != null && y.CryptoKey != null)
                {
                    if (!Collection.Equals(x.CryptoKey, y.CryptoKey)) return false;
                }

                return true;
            }

            public int GetHashCode(Seed obj)
            {
                if (obj == null) return 0;
                else if (obj.Name == null) return 0;
                else return obj.Name.GetHashCode();
            }
        }

        class SeedHashEqualityComparer : IEqualityComparer<Seed>
        {
            public bool Equals(Seed x, Seed y)
            {
                if (x == null && y == null) return true;
                if ((x == null) != (y == null)) return false;
                if (object.ReferenceEquals(x, y)) return true;

                if (//x.Length != y.Length
                    //|| ((x.Keywords == null) != (y.Keywords == null))
                    //|| x.CreationTime != y.CreationTime
                    //|| x.Name != y.Name
                    //|| x.Comment != y.Comment
                    x.Rank != y.Rank

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
    }

    class SearchTreeViewItem : TreeViewItem
    {
        private int _hit;
        private SearchTreeItem _value;

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

            base.IsExpanded = true;
        }

        public SearchTreeViewItem(SearchTreeItem searchTreeItem)
            : this()
        {
            this.Value = searchTreeItem;

            base.IsExpanded = true;
        }

        public void Update()
        {
            base.Header = string.Format("{0} ({1})", _value.SearchItem.Name, _hit);

            List<SearchTreeViewItem> list = new List<SearchTreeViewItem>();

            foreach (var item in this.Value.Items)
            {
                list.Add(new SearchTreeViewItem(item));
            }

            foreach (var item in this.Items.OfType<SearchTreeViewItem>().ToArray())
            {
                if (!list.Any(n => object.ReferenceEquals(n.Value.SearchItem, item.Value.SearchItem)))
                {
                    this.Items.Remove(item);
                }
            }

            foreach (var item in list)
            {
                if (!this.Items.OfType<SearchTreeViewItem>().Any(n => object.ReferenceEquals(n.Value.SearchItem, item.Value.SearchItem)))
                {
                    this.Items.Add(item);
                }
            }

            this.Items.SortDescriptions.Clear();
            this.Items.SortDescriptions.Add(new SortDescription("Value.SearchItem.Name", ListSortDirection.Ascending));
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
    class SearchTreeItem : IDeepCloneable<SearchTreeItem>
    {
        private SearchItem _searchItem;
        private List<SearchTreeItem> _items;

        [DataMember(Name = "SearchItem")]
        public SearchItem SearchItem
        {
            get
            {
                return _searchItem;
            }
            set
            {
                _searchItem = value;
            }
        }

        [DataMember(Name = "Items")]
        public List<SearchTreeItem> Items
        {
            get
            {
                if (_items == null)
                    _items = new List<SearchTreeItem>();

                return _items;
            }
        }

        #region IDeepClone<SearchTreeItem> メンバ

        public SearchTreeItem DeepClone()
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

        #endregion
    }

    [DataContract(Name = "SearchItem", Namespace = "http://Amoeba/Windows")]
    class SearchItem : IEquatable<SearchItem>, IDeepCloneable<SearchItem>
    {
        private string _name;
        private List<SearchContains<string>> _searchNameCollection;
        private List<SearchContains<SearchRegex>> _searchNameRegexCollection;
        private List<SearchContains<string>> _searchSignatureCollection;
        private List<SearchContains<string>> _searchKeywordCollection;
        private List<SearchContains<SearchRange<DateTime>>> _searchCreationTimeRangeCollection;
        private List<SearchContains<SearchRange<long>>> _searchLengthRangeCollection;
        private List<SearchContains<Seed>> _searchSeedCollection;
        private SearchState _searchState = 0;

        public SearchItem()
        {

        }

        public static bool operator ==(SearchItem x, SearchItem y)
        {
            if ((object)x == null)
            {
                if ((object)y == null) return true;

                return ((SearchItem)y).Equals((SearchItem)x);
            }
            else
            {
                return ((SearchItem)x).Equals((SearchItem)y);
            }
        }

        public static bool operator !=(SearchItem x, SearchItem y)
        {
            return !(x == y);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchItem)) return false;

            return this.Equals((SearchItem)obj);
        }

        public bool Equals(SearchItem other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.Name != other.Name
                || this.SearchState != other.SearchState)
            {
                return false;
            }

            if (this.SearchNameCollection != null && other.SearchNameCollection != null)
            {
                if (!Collection.Equals(this.SearchNameCollection, other.SearchNameCollection)) return false;
            }

            if (this.SearchNameRegexCollection != null && other.SearchNameRegexCollection != null)
            {
                if (!Collection.Equals(this.SearchNameRegexCollection, other.SearchNameRegexCollection)) return false;
            }

            if (this.SearchSignatureCollection != null && other.SearchSignatureCollection != null)
            {
                if (!Collection.Equals(this.SearchSignatureCollection, other.SearchSignatureCollection)) return false;
            }

            if (this.SearchKeywordCollection != null && other.SearchKeywordCollection != null)
            {
                if (!Collection.Equals(this.SearchKeywordCollection, other.SearchKeywordCollection)) return false;
            }

            if (this.SearchCreationTimeRangeCollection != null && other.SearchCreationTimeRangeCollection != null)
            {
                if (!Collection.Equals(this.SearchCreationTimeRangeCollection, other.SearchCreationTimeRangeCollection)) return false;
            }

            if (this.SearchLengthRangeCollection != null && other.SearchLengthRangeCollection != null)
            {
                if (!Collection.Equals(this.SearchLengthRangeCollection, other.SearchLengthRangeCollection)) return false;
            }

            if (this.SearchSeedCollection != null && other.SearchSeedCollection != null)
            {
                if (!Collection.Equals(this.SearchSeedCollection, other.SearchSeedCollection)) return false;
            }

            return true;
        }

        [DataMember(Name = "Name")]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        [DataMember(Name = "SearchNameCollection")]
        public List<SearchContains<string>> SearchNameCollection
        {
            get
            {
                if (_searchNameCollection == null)
                    _searchNameCollection = new List<SearchContains<string>>();

                return _searchNameCollection;
            }
        }

        [DataMember(Name = "SearchNameRegexCollection")]
        public List<SearchContains<SearchRegex>> SearchNameRegexCollection
        {
            get
            {
                if (_searchNameRegexCollection == null)
                    _searchNameRegexCollection = new List<SearchContains<SearchRegex>>();

                return _searchNameRegexCollection;
            }
        }

        [DataMember(Name = "SearchSignatureCollection")]
        public List<SearchContains<string>> SearchSignatureCollection
        {
            get
            {
                if (_searchSignatureCollection == null)
                    _searchSignatureCollection = new List<SearchContains<string>>();

                return _searchSignatureCollection;
            }
        }

        [DataMember(Name = "SearchKeywordCollection")]
        public List<SearchContains<string>> SearchKeywordCollection
        {
            get
            {
                if (_searchKeywordCollection == null)
                    _searchKeywordCollection = new List<SearchContains<string>>();

                return _searchKeywordCollection;
            }
        }

        [DataMember(Name = "SearchCreationTimeRangeCollection")]
        public List<SearchContains<SearchRange<DateTime>>> SearchCreationTimeRangeCollection
        {
            get
            {
                if (_searchCreationTimeRangeCollection == null)
                    _searchCreationTimeRangeCollection = new List<SearchContains<SearchRange<DateTime>>>();

                return _searchCreationTimeRangeCollection;
            }
        }

        [DataMember(Name = "SearchLengthRangeCollection")]
        public List<SearchContains<SearchRange<long>>> SearchLengthRangeCollection
        {
            get
            {
                if (_searchLengthRangeCollection == null)
                    _searchLengthRangeCollection = new List<SearchContains<SearchRange<long>>>();

                return _searchLengthRangeCollection;
            }
        }

        [DataMember(Name = "SearchSeedCollection")]
        public List<SearchContains<Seed>> SearchSeedCollection
        {
            get
            {
                if (_searchSeedCollection == null)
                    _searchSeedCollection = new List<SearchContains<Seed>>();

                return _searchSeedCollection;
            }
        }

        [DataMember(Name = "SearchState")]
        public SearchState SearchState
        {
            get
            {
                return _searchState;
            }
            set
            {
                _searchState = value;
            }
        }

        public override string ToString()
        {
            return string.Format("Name = {0}", this.Name);
        }

        #region IDeepClone<SearchItem> メンバ

        public SearchItem DeepClone()
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

        #endregion
    }

    [Flags]
    [DataContract(Name = "SearchState", Namespace = "http://Amoeba/Windows")]
    enum SearchState
    {
        [EnumMember(Value = "Searching")]
        Searching = 0x1,

        [EnumMember(Value = "Uploading")]
        Uploading = 0x2,

        [EnumMember(Value = "Uploaded")]
        Uploaded = 0x4,

        [EnumMember(Value = "Downloading")]
        Downloading = 0x8,

        [EnumMember(Value = "Downloaded")]
        Downloaded = 0x10,
    }

    [DataContract(Name = "SearchContains", Namespace = "http://Amoeba/Windows")]
    class SearchContains<T> : IEquatable<SearchContains<T>>, IDeepCloneable<SearchContains<T>>
    {
        [DataMember(Name = "Contains")]
        public bool Contains { get; set; }

        [DataMember(Name = "Value")]
        public T Value { get; set; }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
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

        #region IDeepClone<SearchContains<T>> メンバ

        public SearchContains<T> DeepClone()
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

        #endregion
    }

    [DataContract(Name = "SearchRegex", Namespace = "http://Amoeba/Windows")]
    class SearchRegex : IEquatable<SearchRegex>, IDeepCloneable<SearchRegex>
    {
        private string _value;
        private bool _isIgnoreCase;

        private Regex _regex;

        [DataMember(Name = "Value")]
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                this.RegexUpdate();
            }
        }

        [DataMember(Name = "IsIgnoreCase")]
        public bool IsIgnoreCase
        {
            get
            {
                return _isIgnoreCase;
            }
            set
            {
                _isIgnoreCase = value;

                this.RegexUpdate();
            }
        }

        private void RegexUpdate()
        {
            var o = RegexOptions.Compiled | RegexOptions.Singleline;
            if (_isIgnoreCase) o |= RegexOptions.IgnoreCase;

            try
            {
                if (_value != null) _regex = new Regex(_value, o);
                else _regex = null;
            }
            catch (Exception)
            {
                _regex = null;
            }
        }

        public bool IsMatch(string value)
        {
            if (_regex == null) return false;

            return _regex.IsMatch(value);
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
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

        #region IDeepClone<SearchRegex> メンバ

        public SearchRegex DeepClone()
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
        #endregion
    }

    [DataContract(Name = "SearchRange", Namespace = "http://Amoeba/Windows")]
    class SearchRange<T> : IEquatable<SearchRange<T>>, IDeepCloneable<SearchRange<T>>
        where T : IComparable
    {
        T _max;
        T _min;

        [DataMember(Name = "Max")]
        public T Max
        {
            get
            {
                return _max;
            }
            set
            {
                _max = value;
                _max = (_max.CompareTo(_min) < 0) ? _min : _max;
            }
        }

        [DataMember(Name = "Min")]
        public T Min
        {
            get
            {
                return _min;
            }
            set
            {
                _min = value;
                _min = (_min.CompareTo(_max) > 0) ? _max : _min;
            }
        }

        public bool Verify(T value)
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

        public override int GetHashCode()
        {
            return this.Min.GetHashCode();
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
            return string.Format("Max = {0}, Min = {1}", this.Max, this.Min);
        }

        #region IDeepClone<SearchRange<T>> メンバ

        public SearchRange<T> DeepClone()
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

        #endregion
    }
}
