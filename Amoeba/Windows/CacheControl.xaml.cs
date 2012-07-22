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
    /// CacheControl.xaml の相互作用ロジック
    /// </summary>
    partial class CacheControl : UserControl
    {
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private Thread _searchThread = null;
        private volatile bool _refresh = false;
        private volatile bool _recache = false;

        private volatile List<SearchListViewItem> _searchingCache = new List<SearchListViewItem>();
        private Stopwatch _updateStopwatch = new Stopwatch();

        public CacheControl(MainWindow mainWindow, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _treeViewItem.Value = Settings.Instance.CacheControl_SearchTreeItem;

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

            _searchThread = new Thread(new ThreadStart(() =>
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
                            CacheControl.Filter(ref newList, searchTreeViewItem.Value);

                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                            {
                                searchTreeViewItem.Hit = newList.Count;
                                searchTreeViewItem.Update();
                            }), null);
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
                catch (Exception)
                {

                }
            }));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "SearchThread";
            _searchThread.Start();
        }

        private static void Filter(ref HashSet<SearchListViewItem> searchItems, SearchTreeItem searchTreeItem)
        {
            searchItems.IntersectWith(searchItems.ToArray().Where(searchItem =>
            {
                var x = searchTreeItem.SearchItem.SearchState;
                var y = searchItem.State;

                if (x.HasFlag(SearchState.Cache) && y.HasFlag(SearchState.Cache))
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
                        if (searchContains.Contains) return searchItem.Value.Keywords.Any(n => !string.IsNullOrWhiteSpace(n) && n == searchContains.Value);

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchSignatureCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchSignatureCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains)
                        {
                            if (searchContains.Value == "Anonymous")
                            {
                                return searchItem.Signature == null;
                            }
                            else
                            {
                                return searchItem.Signature == searchContains.Value;
                            }
                        }

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
                                .All(n => searchItem.Value.Name.ToLower().Contains(n.ToLower()));
                        }

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchNameRegexCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchNameRegexCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return searchContains.Value.IsMatch(searchItem.Value.Name);

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
                        if (!searchContains.Contains) return searchItem.Value.Keywords.Any(n => !string.IsNullOrWhiteSpace(n) && n == searchContains.Value);

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchSignatureCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchSignatureCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains)
                        {
                            if (searchContains.Value == "Anonymous")
                            {
                                return searchItem.Signature == null;
                            }
                            else
                            {
                                return searchItem.Signature == searchContains.Value;
                            }
                        }

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
                                .All(n => searchItem.Value.Name.Contains(n));
                        }

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchNameRegexCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchNameRegexCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return searchContains.Value.IsMatch(searchItem.Value.Name);

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

                HashSet<Seed> seeds = new HashSet<Seed>();
                Dictionary<Seed, SearchState> seedsDictionary = new Dictionary<Seed, SearchState>(new SeedHashEqualityComparer());

                {
                    if (!Settings.Instance.Global_SearchFilterSettings_State.HasFlag(SearchState.Cache))
                    {
                        foreach (var seed in _amoebaManager.Seeds)
                        {
                            seeds.Add(seed);
                            seedsDictionary[seed] = SearchState.Cache;
                        }
                    }

                    if (!Settings.Instance.Global_SearchFilterSettings_State.HasFlag(SearchState.Uploading))
                    {
                        foreach (var information in _amoebaManager.UploadingInformation)
                        {
                            if (information.Contains("Seed") && ((UploadState)information["State"]) != UploadState.Completed)
                            {
                                var seed = (Seed)information["Seed"];
                                seeds.Add(seed);

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
                                seeds.Add(seed);

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
                            seeds.Add(seed);

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
                            seeds.Add(seed);

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

                List<SearchListViewItem> searchItems = new List<SearchListViewItem>();

                foreach (var seed in seeds)
                {
                    var searchItem = new SearchListViewItem();

                    searchItem.Name = seed.Name;
                    searchItem.Signature = MessageConverter.ToSignatureString(seed.Certificate);
                    searchItem.Keywords = string.Join(", ", seed.Keywords.Where(n => !string.IsNullOrWhiteSpace(n)));
                    searchItem.CreationTime = seed.CreationTime;
                    searchItem.Length = seed.Length;
                    searchItem.Comment = seed.Comment;
                    searchItem.Value = seed;
                    searchItem.State = seedsDictionary[seed];

                    searchItems.Add(searchItem);
                }

                sw.Stop();
                Debug.WriteLine("Search {0}", sw.ElapsedMilliseconds);

                Random random = new Random();
                _searchingCache = searchItems.OrderBy(n => random.Next()).Take(1000000).ToList();

                _updateStopwatch.Restart();

                return _searchingCache;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return new SearchListViewItem[0];
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
                searchTreeItem.SearchItem.Name = _textBox.Text;
                searchTreeItem.SearchItem.SearchNameCollection.Add(new SearchContains<string>()
                {
                    Contains = true,
                    Value = _textBox.Text
                });

                selectTreeViewItem.Value.Items.Add(searchTreeItem);

                selectTreeViewItem.Update();

                _textBox.Text = "";
            }
        }

        #region _treeView

        private Point _startPoint = new Point(-1, -1);

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

        private void _treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _treeView.GetCurrentItem(e.GetPosition) as SearchTreeViewItem;
            if (item == null)
            {
                _startPoint = new Point(-1, -1);

                return;
            }

            _startPoint = e.GetPosition(null);

            if (item.IsSelected == true)
                _treeView_SelectedItemChanged(null, null);
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

        private void _treeViewAddMenuItem_Click(object sender, RoutedEventArgs e)
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
            if (selectTreeViewItem == null) return;

            if (selectTreeViewItem == _treeViewItem) return;

            var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<SearchTreeViewItem>().ToList();

            list[list.Count - 2].IsSelected = true;

            list[list.Count - 2].Value.Items.Remove(selectTreeViewItem.Value);
            list[list.Count - 2].Update();

            this.Update();
        }

        private void _treeViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

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
            Clipboard.SetSearchTreeItems(new SearchTreeItem[0]);

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

            BoxEditWindow window = new BoxEditWindow(ref box);
            window.Owner = _mainWindow;
            window.ShowDialog();

            if (window.DialogResult != true) return; 

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
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
                _listViewDeleteMenuItem.IsEnabled = false;
                _listViewDownloadHistoryDeleteMenuItem.IsEnabled = false;
                _listViewUploadHistoryDeleteMenuItem.IsEnabled = false;
                _listViewFilterNameMenuItem.IsEnabled = false;
                _listViewFilterSignatureMenuItem.IsEnabled = false;
                _listViewFilterKeywordMenuItem.IsEnabled = false;
                _listViewFilterSeedMenuItem.IsEnabled = false;
                _listViewDownloadMenuItem.IsEnabled = false;

                return;
            }

            var selectItems = _listView.SelectedItems;

            _listViewEditMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewCopyInfoMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewFilterNameMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewFilterSignatureMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewFilterKeywordMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewFilterSeedMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewDownloadMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            if (!_listViewDeleteMenuItem_IsEnabled) _listViewDeleteMenuItem.IsEnabled = false;
            else _listViewDeleteMenuItem.IsEnabled = selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Cache));
            if (!_listViewDownloadHistoryDeleteMenuItem_IsEnabled) _listViewDownloadHistoryDeleteMenuItem.IsEnabled = false;
            else _listViewDownloadHistoryDeleteMenuItem.IsEnabled = selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Downloaded));
            if (!_listViewUploadHistoryDeleteMenuItem_IsEnabled) _listViewUploadHistoryDeleteMenuItem.IsEnabled = false;
            else _listViewUploadHistoryDeleteMenuItem.IsEnabled = selectItems.OfType<SearchListViewItem>().Any(n => n.State.HasFlag(SearchState.Uploaded));
        }

        private void _listViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems.OfType<SearchListViewItem>();
            if (selectSearchListViewItems == null) return;

            var selectSeeds = new HashSet<Seed>(selectSearchListViewItems.Select(n => n.Value));
            if (selectSeeds == null) return;

            IList<Seed> seeds = new List<Seed>();

            {
                if (!Settings.Instance.Global_SearchFilterSettings_State.HasFlag(SearchState.Cache))
                {
                    foreach (var seed in _amoebaManager.Seeds)
                    {
                        if (selectSeeds.Contains(seed)) seeds.Add(seed);
                    }
                }

                if (!Settings.Instance.Global_SearchFilterSettings_State.HasFlag(SearchState.Uploading))
                {
                    foreach (var information in _amoebaManager.UploadingInformation)
                    {
                        if (information.Contains("Seed") && ((UploadState)information["State"]) != UploadState.Completed)
                        {
                            var seed = (Seed)information["Seed"];
                            if (selectSeeds.Contains(seed)) seeds.Add(seed);
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
                            if (selectSeeds.Contains(seed)) seeds.Add(seed);
                        }
                    }
                }

                if (!Settings.Instance.Global_SearchFilterSettings_State.HasFlag(SearchState.Uploaded))
                {
                    foreach (var seed in _amoebaManager.UploadedSeeds)
                    {
                        if (selectSeeds.Contains(seed)) seeds.Add(seed);
                    }
                }

                if (!Settings.Instance.Global_SearchFilterSettings_State.HasFlag(SearchState.Downloaded))
                {
                    foreach (var seed in _amoebaManager.DownloadedSeeds)
                    {
                        if (selectSeeds.Contains(seed)) seeds.Add(seed);
                    }
                }
            }

            SeedEditWindow window = new SeedEditWindow(ref seeds, _amoebaManager);
            window.Owner = _mainWindow;
            window.ShowDialog();

            _recache = true;

            this.Update();
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

            _listViewDeleteMenuItem_IsEnabled = false;

            var list = new HashSet<Seed>(new SeedHashEqualityComparer());

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Value == null || !item.State.HasFlag(SearchState.Cache)) continue;

                list.Add(item.Value);
            }

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                foreach (var item in list)
                {
                    _amoebaManager.RemoveSeed(item);
                }

                foreach (var seed in _amoebaManager.Seeds.ToArray())
                {
                    if (list.Contains(seed))
                    {
                        _amoebaManager.RemoveSeed(seed);
                    }
                }

                _recache = true;

                _listViewDeleteMenuItem_IsEnabled = true;

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
            }));
        }

        volatile bool _listViewDownloadHistoryDeleteMenuItem_IsEnabled = true;

        private void _listViewDownloadHistoryDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            _listViewDownloadHistoryDeleteMenuItem_IsEnabled = false;

            var list = new HashSet<Seed>(new SeedHashEqualityComparer());

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Value == null || !item.State.HasFlag(SearchState.Downloaded)) continue;

                list.Add(item.Value);
            }

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                foreach (var item in list)
                {
                    _amoebaManager.DownloadedSeeds.Remove(item);
                }

                foreach (var seed in _amoebaManager.DownloadedSeeds.ToArray())
                {
                    if (list.Contains(seed))
                    {
                        _amoebaManager.DownloadedSeeds.Remove(seed);
                    }
                }

                _recache = true;

                _listViewDownloadHistoryDeleteMenuItem_IsEnabled = true;

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
            }));
        }

        volatile bool _listViewUploadHistoryDeleteMenuItem_IsEnabled = true;

        private void _listViewUploadHistoryDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _listView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            _listViewUploadHistoryDeleteMenuItem_IsEnabled = false;

            var list = new HashSet<Seed>(new SeedHashEqualityComparer());

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Value == null || !item.State.HasFlag(SearchState.Uploaded)) continue;

                list.Add(item.Value);
            }

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                foreach (var item in list)
                {
                    _amoebaManager.UploadedSeeds.Remove(item);
                }

                foreach (var seed in _amoebaManager.UploadedSeeds.ToArray())
                {
                    if (list.Contains(seed))
                    {
                        _amoebaManager.UploadedSeeds.Remove(seed);
                    }
                }

                _recache = true;

                _listViewUploadHistoryDeleteMenuItem_IsEnabled = true;

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
        }

        #endregion

        private class SearchListViewItem
        {
            public string Name { get; set; }
            public string Signature { get; set; }
            public SearchState State { get; set; }
            public string Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public long Length { get; set; }
            public string Comment { get; set; }
            public Seed Value { get; set; }

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
                    || this.State != other.State
                    || this.Value != other.Value)
                {
                    return false;
                }

                return true;
            }
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

            base.IsExpanded = true;
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

            base.IsExpanded = true;
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

        public void Update()
        {
            base.Header = string.Format("{0} ({1})", _value.SearchItem.Name, _hit);

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

                return x.Value.GetHashCode().CompareTo(y.Value.GetHashCode());
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

        #region IDeepClone<SearchTreeItem>

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

        #region IDeepClone<SearchItem>

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
        [EnumMember(Value = "Cache")]
        Cache = 0x1,

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

        #region IDeepClone<SearchContains<T>>

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

        #region IDeepClone<SearchRegex>

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
                _min = (_min.CompareTo(_max) > 0) ? _max : _min;
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
                _max = (_max.CompareTo(_min) < 0) ? _min : _max;
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

        #region IDeepClone<SearchRange<T>>

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
