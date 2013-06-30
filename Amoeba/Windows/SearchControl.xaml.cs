using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using Amoeba.Properties;
using Library;
using Library.Collections;
using Library.Io;
using Library.Net;
using Library.Net.Amoeba;
using Library.Security;

namespace Amoeba.Windows
{
    /// <summary>
    /// Interaction logic for SearchControl.xaml
    /// </summary>
    partial class SearchControl : UserControl
    {
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private volatile bool _refresh = false;

        private Thread _searchThread;
        private Thread _cacheThread = null;

        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private ObservableCollection<StoreTreeViewItem> _treeViewItemCollection = new ObservableCollection<StoreTreeViewItem>();

        public SearchControl(MainWindow mainWindow, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            InitializeComponent();

            foreach (var item in Settings.Instance.SearchControl_StoreTreeItems)
            {
                _treeViewItemCollection.Add(new StoreTreeViewItem(item));
            }

            _treeView.ItemsSource = _treeViewItemCollection;

            {
                foreach (var path in Settings.Instance.SearchControl_ExpandedPath.ToArray())
                {
                    if (path.Count == 0) goto End;

                    TreeViewItem treeViewItem = _treeViewItemCollection.FirstOrDefault(n => n.Value.UploadSignature == path[0]);
                    if (treeViewItem == null) goto End;

                    foreach (var name in path.Skip(1))
                    {
                        treeViewItem = treeViewItem.Items.OfType<BoxTreeViewItem>().FirstOrDefault(n => n.Value.Name == name);
                        if (treeViewItem == null) goto End;
                    }

                    treeViewItem.IsExpanded = true;
                    continue;

                End: ;

                    Settings.Instance.SearchControl_ExpandedPath.Remove(path);
                }
            }

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                if (App.SelectTab == TabItemType.Search && !_refresh)
                {
                    if (_treeView.SelectedItem is StoreTreeViewItem)
                    {
                        var selectStoreTreeViewItem = (StoreTreeViewItem)_treeView.SelectedItem;
                        _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectStoreTreeViewItem.Value.UploadSignature);
                    }
                    else if (_treeView.SelectedItem is BoxTreeViewItem)
                    {
                        var selectBoxTreeViewItem = (BoxTreeViewItem)_treeView.SelectedItem;
                        _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectBoxTreeViewItem.Value.Name);
                    }
                }
            };

            _searchThread = new Thread(new ThreadStart(this.Search));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "SearchControl_SearchThread";
            _searchThread.Start();

            _cacheThread = new Thread(new ThreadStart(this.Cache));
            _cacheThread.Priority = ThreadPriority.Highest;
            _cacheThread.IsBackground = true;
            _cacheThread.Name = "SearchControl_CacheThread";
            _cacheThread.Start();

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

                    TreeViewItem selectTreeViewItem = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;
                    }));

                    if (selectTreeViewItem == null)
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            _refresh = false;

                            _listView.Items.Clear();

                            if (App.SelectTab == TabItemType.Search)
                                _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
                        }));

                        continue;
                    }

                    if (selectTreeViewItem is StoreTreeViewItem)
                    {
                        StoreTreeViewItem selectStoreTreeViewItem = (StoreTreeViewItem)selectTreeViewItem;

                        selectStoreTreeViewItem.Value.IsUpdated = false;

                        HashSet<object> newList = new HashSet<object>(new ReferenceEqualityComparer());
                        HashSet<object> oldList = new HashSet<object>(new ReferenceEqualityComparer());

                        string[] words = null;

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            oldList.UnionWith(_listView.Items.OfType<object>());

                            var searchText = _searchTextBox.Text;

                            if (!string.IsNullOrWhiteSpace(searchText))
                            {
                                words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                            }
                        }));

                        foreach (var box in selectStoreTreeViewItem.Value.Boxes)
                        {
                            var text = (box.Name ?? "").ToLower();
                            if (words != null && !words.All(n => text.Contains(n))) continue;

                            var boxesListViewItem = new BoxListViewItem();
                            boxesListViewItem.Index = newList.Count;
                            boxesListViewItem.Name = box.Name;
                            if (box.Certificate != null) boxesListViewItem.Signature = box.Certificate.ToString();
                            boxesListViewItem.CreationTime = box.CreationTime;
                            boxesListViewItem.Length = SearchControl.GetBoxLength(box);
                            boxesListViewItem.Comment = box.Comment;
                            boxesListViewItem.Value = box;

                            newList.Add(boxesListViewItem);
                        }

                        var removeList = new List<object>();
                        var addList = new List<object>();

                        foreach (var item in oldList)
                        {
                            if (!newList.Contains(item)) removeList.Add(item);
                        }

                        foreach (var item in newList)
                        {
                            if (!oldList.Contains(item)) addList.Add(item);
                        }

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (selectStoreTreeViewItem != _treeView.SelectedItem) return;
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

                            if (App.SelectTab == TabItemType.Search)
                                _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectStoreTreeViewItem.Value.UploadSignature);
                        }));
                    }
                    else if (selectTreeViewItem is BoxTreeViewItem)
                    {
                        BoxTreeViewItem selectBoxTreeViewItem = (BoxTreeViewItem)selectTreeViewItem;

                        HashSet<object> newList = new HashSet<object>(new ReferenceEqualityComparer());
                        HashSet<object> oldList = new HashSet<object>(new ReferenceEqualityComparer());

                        string[] words = new string[] { };

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            oldList.UnionWith(_listView.Items.OfType<object>());

                            var searchText = _searchTextBox.Text;

                            if (!string.IsNullOrWhiteSpace(searchText))
                            {
                                words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                            }
                        }));

                        foreach (var box in selectBoxTreeViewItem.Value.Boxes)
                        {
                            var text = (box.Name ?? "").ToLower();
                            if (words != null && !words.All(n => text.Contains(n))) continue;

                            var boxesListViewItem = new BoxListViewItem();
                            boxesListViewItem.Index = newList.Count;
                            boxesListViewItem.Name = box.Name;
                            if (box.Certificate != null) boxesListViewItem.Signature = box.Certificate.ToString();
                            boxesListViewItem.CreationTime = box.CreationTime;
                            boxesListViewItem.Length = SearchControl.GetBoxLength(box);
                            boxesListViewItem.Comment = box.Comment;
                            boxesListViewItem.Value = box;

                            newList.Add(boxesListViewItem);
                        }

                        Dictionary<Seed, SearchState> seedsDictionary = new Dictionary<Seed, SearchState>(new SeedHashEqualityComparer());

                        {
                            foreach (var seed in _amoebaManager.CacheSeeds)
                            {
                                seedsDictionary[seed] = SearchState.Cache;
                            }

                            foreach (var seed in _amoebaManager.ShareSeeds)
                            {
                                if (!seedsDictionary.ContainsKey(seed))
                                {
                                    seedsDictionary[seed] = SearchState.Share;
                                }
                                else
                                {
                                    seedsDictionary[seed] |= SearchState.Share;
                                }
                            }

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

                        foreach (var seed in selectBoxTreeViewItem.Value.Seeds)
                        {
                            var text = (seed.Name ?? "").ToLower();
                            if (words != null && !words.All(n => text.Contains(n))) continue;

                            var seedListViewItem = new SeedListViewItem();
                            seedListViewItem.Index = newList.Count;
                            seedListViewItem.Name = seed.Name;
                            if (seed.Certificate != null) seedListViewItem.Signature = seed.Certificate.ToString();
                            seedListViewItem.Keywords = string.Join(", ", seed.Keywords.Where(n => !string.IsNullOrWhiteSpace(n)));
                            seedListViewItem.CreationTime = seed.CreationTime;
                            seedListViewItem.Length = seed.Length;
                            seedListViewItem.Comment = seed.Comment;
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

                                seedListViewItem.Hash = NetworkConverter.ToHexString(Sha512.ComputeHash(stream));
                            }

                            SearchState state;

                            if (seedsDictionary.TryGetValue(seed, out state))
                            {
                                seedListViewItem.State = state;
                            }

                            seedListViewItem.Value = seed;

                            newList.Add(seedListViewItem);
                        }

                        var removeList = new List<object>();
                        var addList = new List<object>();

                        foreach (var item in oldList)
                        {
                            if (!newList.Contains(item)) removeList.Add(item);
                        }

                        foreach (var item in newList)
                        {
                            if (!oldList.Contains(item)) addList.Add(item);
                        }

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (selectBoxTreeViewItem != _treeView.SelectedItem) return;
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

                            if (App.SelectTab == TabItemType.Search)
                                _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectBoxTreeViewItem.Value.Name);
                        }));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void Cache()
        {
            try
            {
                for (; ; )
                {
                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        var signatures = _amoebaManager.Signatures;

                        foreach (var item in _treeViewItemCollection.ToArray())
                        {
                            if (signatures.Contains(item.Value.UploadSignature)) continue;

                            _treeViewItemCollection.Remove(item);
                        }

                        foreach (var signature in signatures)
                        {
                            if (_treeViewItemCollection.Any(n => n.Value.UploadSignature == signature)) continue;

                            StoreInfo storeInfo = new StoreInfo();
                            storeInfo.UploadSignature = signature;
                            _treeViewItemCollection.Add(new StoreTreeViewItem(storeInfo));
                        }

                        foreach (var storeTreeViewItem in _treeViewItemCollection)
                        {
                            var store = _amoebaManager.GetStore(storeTreeViewItem.Value.UploadSignature);
                            if (store == null || Collection.Equals(storeTreeViewItem.Value.Boxes, store.Boxes)) continue;

                            StoreInfo storeInfo = new StoreInfo();
                            storeInfo.UploadSignature = storeTreeViewItem.Value.UploadSignature;
                            storeInfo.Boxes.AddRange(store.Boxes);
                            storeInfo.IsUpdated = true;

                            storeTreeViewItem.Value = storeInfo;
                            storeTreeViewItem.Update();
                        }

                        this.Update();
                    }));

                    _autoResetEvent.WaitOne(1000 * 60 * 3);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private static long GetBoxLength(Box box)
        {
            long length = 0;

            foreach (var item in box.Seeds)
            {
                length += item.Length;
            }

            foreach (var item in box.Boxes)
            {
                length += SearchControl.GetBoxLength(item);
            }

            return length;
        }

        private static string GetNormalizedPath(string path)
        {
            string filePath = path;

            foreach (char ic in System.IO.Path.GetInvalidFileNameChars())
            {
                filePath = filePath.Replace(ic.ToString(), "-");
            }
            foreach (char ic in System.IO.Path.GetInvalidPathChars())
            {
                filePath = filePath.Replace(ic.ToString(), "-");
            }

            return filePath;
        }

        private static IEnumerable<Box> GetBoxLineage(StoreInfo parentItem, Seed childItem)
        {
            var list = new List<Box>();
            list.AddRange(parentItem.Boxes);

            for (int i = 0; i < list.Count; i++)
            {
                foreach (var item in list[i].Boxes)
                {
                    list.Add(item);
                }
            }

            var targetList = new List<Box>();

            try
            {
                targetList.Add(list.First(n => n.Seeds.Any(m => object.ReferenceEquals(m, childItem))));

                for (; ; )
                {
                    var item = targetList.Last();
                    if (parentItem.Boxes.Contains(item)) break;

                    targetList.Add(list.First(n => n.Boxes.Any(m => object.ReferenceEquals(m, item))));
                }
            }
            catch (Exception)
            {

            }

            targetList.Reverse();

            return targetList;
        }

        private void Update()
        {
            {
                var list = _treeViewItemCollection.ToList();

                list.Sort((x, y) =>
                {
                    var vx = x.Value;
                    var vy = y.Value;

                    int c = vx.UploadSignature.CompareTo(vy.UploadSignature);
                    if (c != 0) return c;
                    c = vx.Boxes.Count.CompareTo(vy.Boxes.Count);
                    if (c != 0) return c;
                    c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                    if (c != 0) return c;

                    return 0;
                });

                for (int i = 0; i < list.Count; i++)
                {
                    var o = _treeViewItemCollection.IndexOf(list[i]);

                    if (i != o) _treeViewItemCollection.Move(o, i);
                }
            }

            foreach (var item in _treeViewItemCollection)
            {
                item.Sort();
            }

            this.Update_TreeView_Color();

            Settings.Instance.SearchControl_StoreTreeItems = _treeViewItemCollection.Select(n => n.Value).ToLockedList();

            _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
            _refresh = true;
        }

        private void Update_Cache()
        {
            _autoResetEvent.Set();
        }

        private void Update_TreeView_Color()
        {
            var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;

            {
                var items = new List<TreeViewItem>();
                items.AddRange(_treeViewItemCollection.OfType<TreeViewItem>());

                var hitItems = new HashSet<TreeViewItem>();

                foreach (var item in items.OfType<StoreTreeViewItem>().Where(n => n.Value.IsUpdated))
                {
                    hitItems.UnionWith(_treeView.GetLineage(item));
                }

                foreach (var item in items)
                {
                    var textBlock = (TextBlock)item.Header;

                    if (hitItems.Contains(item))
                    {
                        textBlock.FontWeight = FontWeights.ExtraBlack;

                        if (selectTreeViewItem != item)
                        {
                            textBlock.Foreground = new SolidColorBrush(Settings.Instance.Color_Tree_Hit);
                        }
                        else
                        {
                            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00));
                        }
                    }
                    else
                    {
                        textBlock.FontWeight = FontWeights.Normal;

                        if (selectTreeViewItem != item)
                        {
                            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                        }
                        else
                        {
                            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00));
                        }
                    }
                }
            }
        }

        #region _treeView

        private void _treeView_Expanded(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            NameCollection path = new NameCollection();

            foreach (var item in _treeView.GetLineage(treeViewItem))
            {
                if (item is StoreTreeViewItem) path.Add(((StoreTreeViewItem)item).Value.UploadSignature);
                else if (item is BoxTreeViewItem) path.Add(((BoxTreeViewItem)item).Value.Name);
            }

            Settings.Instance.SearchControl_ExpandedPath.Add(path);
        }

        private void _treeView_Collapsed(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            NameCollection path = new NameCollection();

            foreach (var item in _treeView.GetLineage(treeViewItem))
            {
                if (item is StoreTreeViewItem) path.Add(((StoreTreeViewItem)item).Value.UploadSignature);
                else if (item is BoxTreeViewItem) path.Add(((BoxTreeViewItem)item).Value.Name);
            }

            Settings.Instance.SearchControl_ExpandedPath.Remove(path);
        }

        private void _treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _treeView.GetCurrentItem(e.GetPosition) as TreeViewItem;
            if (item == null)
            {
                return;
            }

            Point lposition = e.GetPosition(_treeView);

            if ((_treeView.ActualWidth - lposition.X) < 15
                || (_treeView.ActualHeight - lposition.Y) < 15)
            {
                return;
            }

            if (item.IsSelected == true)
            {
                _treeView_SelectedItemChanged(null, null);
            }
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.Update();
        }

        private void _treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _treeViewPasteMenuItem.IsEnabled = Clipboard.GetText().Split('\r', '\n').Any(n => Signature.HasSignature(n));
        }

        private void _treeViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var signature in Clipboard.GetText().Split('\r', '\n').Where(n => Signature.HasSignature(n)))
            {
                if (_amoebaManager.Signatures.Contains(signature)) continue;

                _amoebaManager.Signatures.Add(signature);
            }

            this.Update_Cache();
        }

        private void _storeTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _storeTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetText(selectTreeViewItem.Value.UploadSignature);
        }

        private void _storeTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            _amoebaManager.Signatures.Remove(selectTreeViewItem.Value.UploadSignature);

            this.Update_Cache();
        }

        private void _storeTreeViewItemExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.RestoreDirectory = true;
                dialog.FileName = "Store - " + Signature.GetSignatureNickname(selectTreeViewItem.Value.UploadSignature);
                dialog.DefaultExt = ".box";
                dialog.Filter = "Box (*.box)|*.box";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileName = dialog.FileName;

                    var box = new Box();
                    box.Name = "Store - " + Signature.GetSignatureNickname(selectTreeViewItem.Value.UploadSignature);
                    box.Boxes.AddRange(selectTreeViewItem.Value.Boxes);
                    box.CreationTime = DateTime.UtcNow;

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

        private void _storeTreeViewItemResetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            _amoebaManager.ResetStore(selectTreeViewItem.Value.UploadSignature);
            selectTreeViewItem.Value.Boxes.Clear();
            selectTreeViewItem.Update();

            this.Update_Cache();
        }

        private void _boxTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _boxTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetBoxes(new Box[] { selectTreeViewItem.Value });
        }

        private void _boxTreeViewItemExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.RestoreDirectory = true;
                dialog.FileName = selectTreeViewItem.Value.Name;
                dialog.DefaultExt = ".box";
                dialog.Filter = "Box (*.box)|*.box";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileName = dialog.FileName;

                    using (FileStream stream = new FileStream(fileName, FileMode.Create))
                    using (Stream directoryStream = AmoebaConverter.ToBoxStream(selectTreeViewItem.Value))
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

        private void _listView_PreviewDragOver(object sender, DragEventArgs e)
        {
            Point position = MouseUtilities.GetMousePosition(_listView);

            if (position.Y < 50)
            {
                var peer = ItemsControlAutomationPeer.CreatePeerForElement(_listView);
                var scrollProvider = peer.GetPattern(PatternInterface.Scroll) as IScrollProvider;

                try
                {
                    scrollProvider.Scroll(System.Windows.Automation.ScrollAmount.NoAmount, System.Windows.Automation.ScrollAmount.SmallDecrement);
                }
                catch (Exception)
                {

                }
            }
            else if ((_listView.ActualHeight - position.Y) < 50)
            {
                var peer = ItemsControlAutomationPeer.CreatePeerForElement(_listView);
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

        private void _listView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point lposition = e.GetPosition(_listView);

            if (_listView.GetCurrentIndex(e.GetPosition) < 0
                || lposition.Y < 25
                || (_listView.ActualWidth - lposition.X) < 15
                || (_listView.ActualHeight - lposition.Y) < 15)
            {
                return;
            }

            {
                var posithonIndex = _listView.GetCurrentIndex(e.GetPosition);

                if (_listView.SelectionMode != System.Windows.Controls.SelectionMode.Single)
                {
                    if (!System.Windows.Input.Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
                        && !System.Windows.Input.Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        var posithonItem = _listView.Items[posithonIndex];

                        if (_listView.SelectedItems.Cast<object>().Any(n => object.ReferenceEquals(n, posithonItem)))
                        {
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        private void _listView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            {
                var posithonIndex = _listView.GetCurrentIndex(e.GetPosition);

                if (posithonIndex != -1 && _listView.SelectionMode != System.Windows.Controls.SelectionMode.Single)
                {
                    if (!System.Windows.Input.Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
                        && !System.Windows.Input.Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        var posithonItem = _listView.Items[posithonIndex];

                        _listView.SelectedItems.Clear();
                        _listView.SelectedItems.Add(posithonItem);
                    }
                }
            }
        }

        private void _listView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_listView.GetCurrentIndex(e.GetPosition) < 0) return;

            if (_treeView.SelectedItem is StoreTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
                if (selectTreeViewItem == null) return;

                if (_listView.SelectedItem is BoxListViewItem)
                {
                    var selectBoxListViewItem = _listView.SelectedItem as BoxListViewItem;
                    if (selectBoxListViewItem == null) return;

                    var selectBox = selectBoxListViewItem.Value;
                    var item = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().FirstOrDefault(n => object.ReferenceEquals(n.Value, selectBox));

                    try
                    {
                        selectTreeViewItem.IsExpanded = true;
                        item.IsSelected = true;
                    }
                    catch (Exception)
                    {

                    }
                }
                else if (_listView.SelectedItem is SeedListViewItem)
                {
                    var selectSeedListViewItem = _listView.SelectedItem as SeedListViewItem;
                    if (selectSeedListViewItem == null) return;

                    var seed = selectSeedListViewItem.Value;
                    string baseDirectory = System.IO.Path.Combine("Search", SearchControl.GetNormalizedPath(Signature.GetSignatureNickname(selectTreeViewItem.Value.UploadSignature)));

                    foreach (var item in _treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().ToList().Select(n => n.Value))
                    {
                        baseDirectory = System.IO.Path.Combine(baseDirectory, SearchControl.GetNormalizedPath(item.Name));
                    }

                    var path = System.IO.Path.Combine(baseDirectory, SearchControl.GetNormalizedPath(seed.Name));

                    _amoebaManager.Download(seed.DeepClone(), baseDirectory, 3);
                }
            }
            else if (_treeView.SelectedItem is BoxTreeViewItem)
            {
                var storeTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<StoreTreeViewItem>().FirstOrDefault() as StoreTreeViewItem;
                if (storeTreeViewItem == null) return;

                var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
                if (selectTreeViewItem == null) return;

                if (_listView.SelectedItem is BoxListViewItem)
                {
                    var selectBoxListViewItem = _listView.SelectedItem as BoxListViewItem;
                    if (selectBoxListViewItem == null) return;

                    var selectBox = selectBoxListViewItem.Value;
                    var item = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().FirstOrDefault(n => object.ReferenceEquals(n.Value, selectBox));

                    try
                    {
                        selectTreeViewItem.IsExpanded = true;
                        item.IsSelected = true;
                    }
                    catch (Exception)
                    {

                    }
                }
                else if (_listView.SelectedItem is SeedListViewItem)
                {
                    var selectSeedListViewItem = _listView.SelectedItem as SeedListViewItem;
                    if (selectSeedListViewItem == null) return;

                    var seed = selectSeedListViewItem.Value;
                    string baseDirectory = System.IO.Path.Combine("Search", SearchControl.GetNormalizedPath(Signature.GetSignatureNickname(storeTreeViewItem.Value.UploadSignature)));

                    foreach (var item in _treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().ToList().Select(n => n.Value))
                    {
                        baseDirectory = System.IO.Path.Combine(baseDirectory, SearchControl.GetNormalizedPath(item.Name));
                    }

                    var path = System.IO.Path.Combine(baseDirectory, SearchControl.GetNormalizedPath(seed.Name));

                    _amoebaManager.Download(seed.DeepClone(), baseDirectory, 3);
                }
            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_refresh || _treeView.SelectedItem == null)
            {
                _listViewCopyMenuItem.IsEnabled = false;
                _listViewCopyInfoMenuItem.IsEnabled = false;
                _listViewDownloadMenuItem.IsEnabled = false;

                return;
            }

            var selectItems = _listView.SelectedItems;

            _listViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewCopyInfoMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewDownloadMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

            Clipboard.SetBoxAndSeeds(boxes, seeds);
        }

        private void _listViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

            var sb = new StringBuilder();

            foreach (var box in boxes)
            {
                sb.AppendLine(MessageConverter.ToInfoMessage(box));
                sb.AppendLine();
            }

            foreach (var seed in seeds)
            {
                sb.AppendLine(AmoebaConverter.ToSeedString(seed));
                sb.AppendLine(MessageConverter.ToInfoMessage(seed));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        private void _listViewDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var storeTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<StoreTreeViewItem>().FirstOrDefault() as StoreTreeViewItem;
            if (storeTreeViewItem == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value).ToList();

            {
                var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value).ToList();

                for (int i = 0; i < boxes.Count; i++)
                {
                    boxes.AddRange(boxes[i].Boxes);
                }

                foreach (var box in boxes)
                {
                    seeds.AddRange(box.Seeds);
                }
            }

            foreach (var seed in seeds)
            {
                string baseDirectory = System.IO.Path.Combine("Search", SearchControl.GetNormalizedPath(Signature.GetSignatureNickname(storeTreeViewItem.Value.UploadSignature)));

                foreach (var item in SearchControl.GetBoxLineage(storeTreeViewItem.Value, seed))
                {
                    baseDirectory = System.IO.Path.Combine(baseDirectory, SearchControl.GetNormalizedPath(item.Name));
                }

                var path = System.IO.Path.Combine(baseDirectory, SearchControl.GetNormalizedPath(seed.Name));

                _amoebaManager.Download(seed.DeepClone(), baseDirectory, 3);
            }
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
            _listView.Items.SortDescriptions.Clear();
            _listView.Items.SortDescriptions.Add(new SortDescription("Type", direction));

            if (sortBy == LanguagesManager.Instance.SearchControl_Name)
            {

            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Signature)
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
            else if (sortBy == LanguagesManager.Instance.SearchControl_CreationTime)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("CreationTime", direction));
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Comment)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Comment", direction));
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_State)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("State", direction));
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Hash)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Hash", direction));
            }

            _listView.Items.SortDescriptions.Add(new SortDescription("Name", direction));
            _listView.Items.SortDescriptions.Add(new SortDescription("Index", direction));
        }

        #endregion

        private class BoxListViewItem
        {
            public int Type { get { return 0; } }
            public int Index { get; set; }
            public string Name { get; set; }
            public string Signature { get; set; }
            public string Keywords { get { return null; } }
            public DateTime CreationTime { get; set; }
            public long Length { get; set; }
            public string Comment { get; set; }
            public SearchState State { get; set; }
            public Box Value { get; set; }
            public string Hash { get { return null; } }

            public override int GetHashCode()
            {
                if (this.Name == null) return 0;
                else return this.Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (!(obj is BoxListViewItem)) return false;
                if (obj == null) return false;
                if (object.ReferenceEquals(this, obj)) return true;
                if (this.GetHashCode() != obj.GetHashCode()) return false;

                var other = (BoxListViewItem)obj;

                if (this.Index != other.Index
                    || this.Name != other.Name
                    || this.Signature != other.Signature
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

        private class SeedListViewItem
        {
            public int Type { get { return 1; } }
            public int Index { get; set; }
            public string Name { get; set; }
            public string Signature { get; set; }
            public string Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public long Length { get; set; }
            public string Comment { get; set; }
            public string Hash { get; set; }
            public Seed Value { get; set; }
            public SearchState State { get; set; }

            public override int GetHashCode()
            {
                if (this.Name == null) return 0;
                else return this.Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (!(obj is SeedListViewItem)) return false;
                if (obj == null) return false;
                if (object.ReferenceEquals(this, obj)) return true;
                if (this.GetHashCode() != obj.GetHashCode()) return false;

                var other = (SeedListViewItem)obj;

                if (this.Index != other.Index
                    || this.Name != other.Name
                    || this.Signature != other.Signature
                    || this.Keywords != other.Keywords
                    || this.CreationTime != other.CreationTime
                    || this.Length != other.Length
                    || this.Comment != other.Comment
                    || this.Hash != other.Hash
                    || this.Value != other.Value
                    || this.State != other.State)
                {
                    return false;
                }

                return true;
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                if (_treeView.SelectedItem is StoreTreeViewItem)
                {
                    _storeTreeViewItemCopyMenuItem_Click(null, null);
                }
                else if (_treeView.SelectedItem is BoxTreeViewItem)
                {
                    _boxTreeViewItemCopyMenuItem_Click(null, null);
                }
            }
            else
            {
                _listViewCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Search(object sender, ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(24);
            _searchTextBox.Focus();
        }
    }
}
