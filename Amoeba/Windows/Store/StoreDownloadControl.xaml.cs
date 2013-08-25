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
    /// Interaction logic for StoreDownloadControl.xaml
    /// </summary>
    partial class StoreDownloadControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private volatile bool _refresh = false;
        private volatile bool _cacheUpdate = false;
        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private StoreCategorizeTreeViewItem _treeViewItem;
        private Dictionary<Seed, SearchState> _seedsDictionary = new Dictionary<Seed, SearchState>(new SeedHashEqualityComparer());

        private Thread _searchThread;
        private Thread _cacheThread;

        public StoreDownloadControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            _treeViewItem = new StoreCategorizeTreeViewItem(Settings.Instance.StoreDownloadControl_StoreCategorizeTreeItem);

            InitializeComponent();

            _treeView.Items.Add(_treeViewItem);

            foreach (var path in Settings.Instance.StoreDownloadControl_ExpandedPath.ToArray())
            {
                if (path.Count == 0 || _treeViewItem.Value.Name != path[0]) goto End;

                TreeViewItem treeViewItem = _treeViewItem;

                foreach (var name in path.Skip(1))
                {
                    treeViewItem = treeViewItem.Items.OfType<TreeViewItem>().FirstOrDefault(n =>
                    {
                        if (n is StoreCategorizeTreeViewItem) return ((StoreCategorizeTreeViewItem)n).Value.Name == name;
                        else if (n is StoreTreeViewItem) return ((StoreTreeViewItem)n).Value.Signature == name;
                        else if (n is BoxTreeViewItem) return ((BoxTreeViewItem)n).Value.Name == name;

                        return false;
                    });

                    if (treeViewItem == null) goto End;
                }

                treeViewItem.IsExpanded = true;
                continue;

            End: ;

                Settings.Instance.StoreDownloadControl_ExpandedPath.Remove(path);
            }

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                this.Update_Title();
            };

            _searchThread = new Thread(new ThreadStart(this.Search));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "StoreDownloadControl_SearchThread";
            _searchThread.Start();

            _cacheThread = new Thread(new ThreadStart(this.Cache));
            _cacheThread.Priority = ThreadPriority.Highest;
            _cacheThread.IsBackground = true;
            _cacheThread.Name = "StoreDownloadControl_CacheThread";
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
                        selectTreeViewItem = (TreeViewItem)_treeView.SelectedItem;
                    }));

                    if (selectTreeViewItem is StoreCategorizeTreeViewItem)
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (selectTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            _listView.Items.Clear();

                            this.Update_Title();
                        }));
                    }
                    else if (selectTreeViewItem is StoreTreeViewItem && selectTreeViewItem is BoxTreeViewItem)
                    {
                        BoxCollection boxes = new BoxCollection();
                        SeedCollection seeds = new SeedCollection();

                        if (selectTreeViewItem is StoreCategorizeTreeViewItem)
                        {
                            var storeTreeViewItem = (StoreTreeViewItem)selectTreeViewItem;

                            boxes.AddRange(storeTreeViewItem.Value.Boxes);
                        }
                        else if (selectTreeViewItem is BoxTreeViewItem)
                        {
                            var boxTreeViewItem = (BoxTreeViewItem)selectTreeViewItem;

                            boxes.AddRange(boxTreeViewItem.Value.Boxes);
                            seeds.AddRange(boxTreeViewItem.Value.Seeds);
                        }

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

                        foreach (var box in boxes)
                        {
                            var text = (box.Name ?? "").ToLower();
                            if (words != null && !words.All(n => text.Contains(n))) continue;

                            var boxesListViewItem = new BoxListViewItem();
                            boxesListViewItem.Index = newList.Count;
                            boxesListViewItem.Name = box.Name;
                            if (box.Certificate != null) boxesListViewItem.Signature = box.Certificate.ToString();
                            boxesListViewItem.CreationTime = box.CreationTime;
                            boxesListViewItem.Length = StoreDownloadControl.GetBoxLength(box);
                            boxesListViewItem.Comment = box.Comment;
                            boxesListViewItem.Value = box;

                            newList.Add(boxesListViewItem);
                        }

                        foreach (var seed in seeds)
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
                            if (seed.Key != null && seed.Key.Hash != null) seedListViewItem.Id = NetworkConverter.ToHexString(seed.Key.Hash);

                            SearchState state;

                            if (_seedsDictionary.TryGetValue(seed, out state))
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

                            this.Update_Title();
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
                    foreach (var seed in _amoebaManager.CacheSeeds)
                    {
                        _seedsDictionary[seed] = SearchState.Cache;
                    }

                    foreach (var seed in _amoebaManager.ShareSeeds)
                    {
                        if (!_seedsDictionary.ContainsKey(seed))
                        {
                            _seedsDictionary[seed] = SearchState.Share;
                        }
                        else
                        {
                            _seedsDictionary[seed] |= SearchState.Share;
                        }
                    }

                    foreach (var information in _amoebaManager.UploadingInformation)
                    {
                        if (information.Contains("Seed") && ((UploadState)information["State"]) != UploadState.Completed)
                        {
                            var seed = (Seed)information["Seed"];

                            if (!_seedsDictionary.ContainsKey(seed))
                            {
                                _seedsDictionary[seed] = SearchState.Uploading;
                            }
                            else
                            {
                                _seedsDictionary[seed] |= SearchState.Uploading;
                            }
                        }
                    }

                    foreach (var information in _amoebaManager.DownloadingInformation)
                    {
                        if (information.Contains("Seed") && ((DownloadState)information["State"]) != DownloadState.Completed)
                        {
                            var seed = (Seed)information["Seed"];

                            if (!_seedsDictionary.ContainsKey(seed))
                            {
                                _seedsDictionary[seed] = SearchState.Downloading;
                            }
                            else
                            {
                                _seedsDictionary[seed] |= SearchState.Downloading;
                            }
                        }
                    }

                    foreach (var seed in _amoebaManager.UploadedSeeds)
                    {
                        if (!_seedsDictionary.ContainsKey(seed))
                        {
                            _seedsDictionary[seed] = SearchState.Uploaded;
                        }
                        else
                        {
                            _seedsDictionary[seed] |= SearchState.Uploaded;
                        }
                    }

                    foreach (var seed in _amoebaManager.DownloadedSeeds)
                    {
                        if (!_seedsDictionary.ContainsKey(seed))
                        {
                            _seedsDictionary[seed] = SearchState.Downloaded;
                        }
                        else
                        {
                            _seedsDictionary[seed] |= SearchState.Downloaded;
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        var storeTreeViewItems = new List<StoreTreeViewItem>();

                        {
                            var categorizeStoreTreeViewItems = new List<StoreCategorizeTreeViewItem>();
                            categorizeStoreTreeViewItems.Add(_treeViewItem);

                            for (int i = 0; i < categorizeStoreTreeViewItems.Count; i++)
                            {
                                categorizeStoreTreeViewItems.AddRange(categorizeStoreTreeViewItems[i].Items.OfType<StoreCategorizeTreeViewItem>());
                                storeTreeViewItems.AddRange(categorizeStoreTreeViewItems[i].Items.OfType<StoreTreeViewItem>());
                            }
                        }

                        bool isUpdate = false;

                        foreach (var storeTreeViewItem in storeTreeViewItems)
                        {
                            var store = _amoebaManager.GetStore(storeTreeViewItem.Value.Signature);
                            if (store == null || Collection.Equals(storeTreeViewItem.Value.Boxes, store.Boxes)) continue;

                            StoreTreeItem storeTreeItem = new StoreTreeItem();
                            storeTreeItem.Signature = storeTreeViewItem.Value.Signature;
                            storeTreeItem.Boxes.AddRange(store.Boxes);
                            storeTreeItem.IsUpdated = true;

                            storeTreeViewItem.Value = storeTreeItem;
                            storeTreeViewItem.Update();

                            isUpdate = true;
                        }

                        if (_cacheUpdate || isUpdate)
                        {
                            _cacheUpdate = false;

                            this.Update();
                        }
                    }));

                    do
                    {
                        _autoResetEvent.WaitOne(1000 * 60 * 3);
                    } while (StoreControl.SelectTab != TabType.Store_Upload);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private static bool CheckBoxDigitalSignature(ref Box box)
        {
            bool flag = true;
            var seedList = new List<Seed>();
            var boxList = new List<Box>();
            boxList.Add(box);

            for (int i = 0; i < boxList.Count; i++)
            {
                boxList.AddRange(boxList[i].Boxes);
                seedList.AddRange(boxList[i].Seeds);
            }

            foreach (var item in seedList.Reverse<Seed>())
            {
                if (!item.VerifyCertificate())
                {
                    flag = false;

                    item.CreateCertificate(null);
                }
            }

            foreach (var item in boxList.Reverse<Box>())
            {
                if (!item.VerifyCertificate())
                {
                    flag = false;

                    item.CreateCertificate(null);
                }
            }

            return flag;
        }

        private bool DigitalSignatureRelease(IEnumerable<BoxTreeViewItem> treeViewItems)
        {
            var targetList = new List<BoxTreeViewItem>();
            StringBuilder builder = new StringBuilder();

            foreach (var item in treeViewItems)
            {
                if (item.Value.Certificate != null)
                {
                    targetList.Add(item);
                    builder.AppendLine(string.Format("\"{0}\"", item.Value.Name));
                }
            }

            if (targetList.Count == 0) return true;

            if (MessageBox.Show(
                    _mainWindow,
                    string.Format("{0}\r\n{1}", builder.ToString(), LanguagesManager.Instance.StoreDownloadControl_DigitalSignatureAnnulled_Message),
                    "StoreDownload",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                foreach (var item in targetList)
                {
                    item.Value.CreateCertificate(null);
                    item.Update();
                }

                return true;
            }
            else
            {
                return false;
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
                length += StoreDownloadControl.GetBoxLength(item);
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

        private void Update()
        {
            Settings.Instance.StoreDownloadControl_StoreCategorizeTreeItem = _treeViewItem.Value;

            _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
            _refresh = true;
        }

        private void Update_Cache()
        {
            _cacheUpdate = true;
            _autoResetEvent.Set();
        }

        private void Update_Title()
        {
            if (_refresh || MainWindow.SelectTab != TabType.Store_Download) return;

            if (_treeView.SelectedItem is StoreCategorizeTreeViewItem)
            {
                var selectTreeViewItem = (StoreCategorizeTreeViewItem)_treeView.SelectedItem;

                _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectTreeViewItem.Value.Name);
            }
            else if (_treeView.SelectedItem is StoreTreeViewItem)
            {
                var selectTreeViewItem = (StoreTreeViewItem)_treeView.SelectedItem;

                _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectTreeViewItem.Value.Signature);
            }
            else if (_treeView.SelectedItem is BoxTreeViewItem)
            {
                var selectTreeViewItem = (BoxTreeViewItem)_treeView.SelectedItem;

                _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectTreeViewItem.Value.Name);
            }
        }

        #region DragAndDrop

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

                    DataObject data = new DataObject("TreeViewItem", _treeView.SelectedItem);
                    DragDrop.DoDragDrop(_grid, data, DragDropEffects.Move);
                }
            }
        }

        private void _grid_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("TreeViewItem"))
            {
                var sourceItem = (TreeViewItem)e.Data.GetData("TreeViewItem");

                if (sourceItem is StoreCategorizeTreeViewItem)
                {
                    var destinationItem = this.GetDropDestination(e.GetPosition);

                    if (destinationItem is StoreCategorizeTreeViewItem)
                    {
                        var s = (StoreCategorizeTreeViewItem)sourceItem;
                        var d = (StoreCategorizeTreeViewItem)destinationItem;

                        if (d.Value.Children.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetAncestors(d).Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = s.Parent;

                        if (parentItem is StoreCategorizeTreeViewItem)
                        {
                            var p = (StoreCategorizeTreeViewItem)parentItem;

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
                else if (sourceItem is StoreTreeViewItem)
                {
                    var destinationItem = this.GetDropDestination(e.GetPosition);

                    if (destinationItem is StoreCategorizeTreeViewItem)
                    {
                        var s = (StoreTreeViewItem)sourceItem;
                        var d = (StoreCategorizeTreeViewItem)destinationItem;

                        if (d.Value.StoreTreeItems.Any(n => object.ReferenceEquals(n, s.Value))) return;

                        var parentItem = s.Parent;

                        if (parentItem is StoreCategorizeTreeViewItem)
                        {
                            var p = (StoreCategorizeTreeViewItem)parentItem;

                            var tItems = p.Value.Children.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            p.Value.Children.Clear();
                            p.Value.Children.AddRange(tItems);

                            p.Update();
                        }

                        d.IsSelected = true;
                        d.Value.StoreTreeItems.Add(s.Value);
                        d.Update();
                    }
                }
            }

            this.Update();
        }

        private TreeViewItem GetDropDestination(GetPositionDelegate getPosition)
        {
            return (TreeViewItem)_treeView.GetCurrentItem(getPosition);
        }

        #endregion

        #region _treeView

        private void _treeView_Expanded(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            Route path = new Route();

            foreach (var item in _treeView.GetAncestors(treeViewItem))
            {
                if (item is StoreCategorizeTreeViewItem) path.Add(((StoreCategorizeTreeViewItem)item).Value.Name);
                else if (item is StoreTreeViewItem) path.Add(((StoreTreeViewItem)item).Value.Signature);
                else if (item is BoxTreeViewItem) path.Add(((BoxTreeViewItem)item).Value.Name);
            }

            Settings.Instance.StoreDownloadControl_ExpandedPath.Add(path);
        }

        private void _treeView_Collapsed(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            Route path = new Route();

            foreach (var item in _treeView.GetAncestors(treeViewItem))
            {
                if (item is StoreCategorizeTreeViewItem) path.Add(((StoreCategorizeTreeViewItem)item).Value.Name);
                else if (item is StoreTreeViewItem) path.Add(((StoreTreeViewItem)item).Value.Signature);
                else if (item is BoxTreeViewItem) path.Add(((BoxTreeViewItem)item).Value.Name);
            }

            Settings.Instance.StoreDownloadControl_ExpandedPath.Remove(path);
        }

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

        private void _treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _storeCategorizeTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = sender as StoreCategorizeTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            _startPoint = new Point(-1, -1);

            MenuItem storeCategorizeTreeViewItemDeleteMenuItem = contextMenu.GetMenuItem("_storeCategorizeTreeViewItemDeleteMenuItem");
            MenuItem storeCategorizeTreeViewItemCutMenuItem = contextMenu.GetMenuItem("_storeCategorizeTreeViewItemCutMenuItem");
            MenuItem storeCategorizeTreeViewItemPasteMenuItem = contextMenu.GetMenuItem("_storeCategorizeTreeViewItemPasteMenuItem");
            MenuItem storeCategorizeTreeViewItemUploadMenuItem = contextMenu.GetMenuItem("_storeCategorizeTreeViewItemUploadMenuItem");

            storeCategorizeTreeViewItemDeleteMenuItem.IsEnabled = (selectTreeViewItem != _treeViewItem);
            storeCategorizeTreeViewItemCutMenuItem.IsEnabled = (selectTreeViewItem != _treeViewItem);
            storeCategorizeTreeViewItemPasteMenuItem.IsEnabled = Clipboard.ContainsStoreCategorizeTreeItems() || Clipboard.ContainsStoreTreeItems();
        }

        private void _storeCategorizeTreeViewItemNewStoreMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            SignatureWindow window = new SignatureWindow();
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.StoreTreeItems.Add(new StoreTreeItem() { Signature = window.DigitalSignature.ToString() });

                selectTreeViewItem.Update();
            }

            this.Update();

        }

        private void _storeCategorizeTreeViewItemNewCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            string name;

            if (!selectTreeViewItem.Value.Children.Any(n => n.Name == "New Category"))
            {
                name = "New Category";
            }
            else
            {
                int i = 1;
                while (selectTreeViewItem.Value.Children.Any(n => n.Name == "New Category_" + i)) i++;

                name = "New Category_" + i;
            }

            StoreCategorizeTreeItemEditWindow window = new StoreCategorizeTreeItemEditWindow();
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.Children.Add(new StoreCategorizeTreeItem() { Name = name });

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _storeCategorizeTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            StoreCategorizeTreeItemEditWindow window = new StoreCategorizeTreeItemEditWindow(selectTreeViewItem.Value.Name);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.Name = window.Name;
                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _storeCategorizeTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreCategorizeTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parentItem = selectTreeViewItem.Parent;

            if (parentItem is StoreCategorizeTreeViewItem)
            {
                var p = (StoreCategorizeTreeViewItem)parentItem;

                p.Value.Children.Remove(selectTreeViewItem.Value);
                p.IsSelected = true;

                p.Update();
            }

            this.Update();
        }

        private void _storeCategorizeTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreCategorizeTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            Clipboard.SetStoreCategorizeTreeItems(new StoreCategorizeTreeItem[] { selectTreeViewItem.Value });

            var parentItem = selectTreeViewItem.Parent;

            if (parentItem is StoreCategorizeTreeViewItem)
            {
                var p = (StoreCategorizeTreeViewItem)parentItem;

                p.Value.Children.Remove(selectTreeViewItem.Value);
                p.IsSelected = true;

                p.Update();
            }

            this.Update();
        }

        private void _storeCategorizeTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreCategorizeTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            Clipboard.SetStoreCategorizeTreeItems(new StoreCategorizeTreeItem[] { selectTreeViewItem.Value });
        }

        private void _storeCategorizeTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.Children.AddRange(Clipboard.GetStoreCategorizeTreeItems());
            selectTreeViewItem.Value.StoreTreeItems.AddRange(Clipboard.GetStoreTreeItems());

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _storeCategorizeTreeViewItemUploadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            var storeTreeItems = new List<StoreTreeItem>();

            {
                var categorizeStoreTreeItems = new List<StoreCategorizeTreeItem>();
                categorizeStoreTreeItems.Add(selectTreeViewItem.Value);

                for (int i = 0; i < categorizeStoreTreeItems.Count; i++)
                {
                    categorizeStoreTreeItems.AddRange(categorizeStoreTreeItems[i].Children);
                    storeTreeItems.AddRange(categorizeStoreTreeItems[i].StoreTreeItems);
                }
            }

            if (!storeTreeItems.Any(n => n.Boxes.Count != 0)) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Upload_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            foreach (var storeTreeItem in storeTreeItems)
            {
                var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == storeTreeItem.Signature);
                if (digitalSignature == null) return;

                Store store = new Store();
                store.Boxes.AddRange(storeTreeItem.Boxes);

                _amoebaManager.Upload(store, digitalSignature);
            }
        }

        private void _storeTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = sender as StoreTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            _startPoint = new Point(-1, -1);
        }

        private void _storeTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            SignatureWindow window = new SignatureWindow(selectTreeViewItem.Value.Signature);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.Signature = window.DigitalSignature.ToString();
                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _storeTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parentItem = selectTreeViewItem.Parent;

            if (parentItem is StoreCategorizeTreeViewItem)
            {
                var p = (StoreCategorizeTreeViewItem)parentItem;

                p.Value.StoreTreeItems.Remove(selectTreeViewItem.Value);
                p.IsSelected = true;

                p.Update();
            }

            this.Update();
        }

        private void _storeTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetStoreTreeItems(new StoreTreeItem[] { selectTreeViewItem.Value });

            var parentItem = selectTreeViewItem.Parent;

            if (parentItem is StoreCategorizeTreeViewItem)
            {
                var p = (StoreCategorizeTreeViewItem)parentItem;

                p.Value.StoreTreeItems.Remove(selectTreeViewItem.Value);
                p.IsSelected = true;

                p.Update();
            }

            this.Update();
        }

        private void _storeTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetStoreTreeItems(new StoreTreeItem[] { selectTreeViewItem.Value });
        }

        private void _storeTreeViewItemExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.RestoreDirectory = true;
                dialog.FileName = "Store - " + Signature.GetSignatureNickname(selectTreeViewItem.Value.Signature);
                dialog.DefaultExt = ".box";
                dialog.Filter = "Box (*.box)|*.box";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileName = dialog.FileName;

                    var box = new Box();
                    box.Name = "Store - " + Signature.GetSignatureNickname(selectTreeViewItem.Value.Signature);
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

        private void _boxTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = sender as BoxTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            _startPoint = new Point(-1, -1);
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
                _startPoint = new Point(-1, -1);

                return;
            }

            _startPoint = e.GetPosition(null);

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
            var posithonIndex = _listView.GetCurrentIndex(e.GetPosition);

            if (posithonIndex != -1)
            {
                var selectItem = _treeView.SelectedItem;

                if (selectItem is StoreTreeViewItem)
                {
                    var listViewItem = _listView.Items[posithonIndex];

                    if (listViewItem is BoxListViewItem)
                    {
                        var selectTreeViewItem = (StoreTreeViewItem)selectItem;
                        var boxListViewItem = (BoxListViewItem)listViewItem;

                        var item = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().FirstOrDefault(n => object.ReferenceEquals(n.Value, boxListViewItem.Value));

                        selectTreeViewItem.IsExpanded = true;
                        item.IsSelected = true;
                    }
                }
                else if (selectItem is BoxTreeViewItem)
                {
                    var listViewItem = _listView.Items[posithonIndex];

                    if (listViewItem is BoxListViewItem)
                    {
                        var selectTreeViewItem = (BoxTreeViewItem)selectItem;
                        var boxListViewItem = (BoxListViewItem)listViewItem;

                        var item = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().FirstOrDefault(n => object.ReferenceEquals(n.Value, boxListViewItem.Value));

                        selectTreeViewItem.IsExpanded = true;
                        item.IsSelected = true;
                    }
                    else if (listViewItem is SeedListViewItem)
                    {
                        var selectTreeViewItem = (BoxTreeViewItem)selectItem;
                        var seedListViewItem = (SeedListViewItem)selectItem;

                        string baseDirectory = "";

                        {
                            List<string> path = new List<string>();

                            foreach (var item in _treeView.GetAncestors(selectTreeViewItem))
                            {
                                if (item is StoreCategorizeTreeViewItem) path.Add(((StoreCategorizeTreeViewItem)item).Value.Name);
                                else if (item is StoreTreeViewItem) path.Add(((StoreTreeViewItem)item).Value.Signature);
                                else if (item is BoxTreeViewItem) path.Add(((BoxTreeViewItem)item).Value.Name);
                            }

                            baseDirectory = System.IO.Path.Combine(path.ToArray());
                        }

                        var seed = seedListViewItem.Value;

                        _amoebaManager.Download(seed.DeepClone(), baseDirectory, 3);

                        this.Update_Cache();
                    }
                }
            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _startPoint = new Point(-1, -1);

            if (_refresh || _treeView.SelectedItem is StoreCategorizeTreeViewItem)
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
            var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;
            if (selectTreeViewItem == null) return;

            string baseDirectory = "";

            {
                List<string> path = new List<string>();

                foreach (var item in _treeView.GetAncestors(selectTreeViewItem))
                {
                    if (item is BoxTreeViewItem) path.Add(((BoxTreeViewItem)item).Value.Name);
                }

                baseDirectory = System.IO.Path.Combine(path.ToArray());
            }

            foreach (var seed in _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value))
            {
                _amoebaManager.Download(seed.DeepClone(), baseDirectory, 3);
            }

            foreach (var box in _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value))
            {
                this.BoxDownload(baseDirectory, box);
            }

            this.Update_Cache();
        }

        private void BoxDownload(string baseDirectory, Box rootBox)
        {
            baseDirectory = System.IO.Path.Combine(baseDirectory, rootBox.Name);

            foreach (var seed in rootBox.Seeds)
            {
                _amoebaManager.Download(seed.DeepClone(), baseDirectory, 3);
            }

            foreach (var box in rootBox.Boxes)
            {
                this.BoxDownload(baseDirectory, box);
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

                if (headerClicked != Settings.Instance.StoreDownloadControl_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.StoreDownloadControl_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                Settings.Instance.StoreDownloadControl_LastHeaderClicked = headerClicked;
                Settings.Instance.StoreDownloadControl_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.StoreDownloadControl_LastHeaderClicked != null)
                {
                    Sort(Settings.Instance.StoreDownloadControl_LastHeaderClicked, Settings.Instance.StoreDownloadControl_ListSortDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listView.Items.SortDescriptions.Clear();
            _listView.Items.SortDescriptions.Add(new SortDescription("Type", direction));

            if (sortBy == LanguagesManager.Instance.StoreDownloadControl_Name)
            {

            }
            else if (sortBy == LanguagesManager.Instance.StoreDownloadControl_Signature)
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
            else if (sortBy == LanguagesManager.Instance.StoreDownloadControl_CreationTime)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("CreationTime", direction));
            }
            else if (sortBy == LanguagesManager.Instance.StoreDownloadControl_Comment)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Comment", direction));
            }
            else if (sortBy == LanguagesManager.Instance.StoreDownloadControl_State)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("State", direction));
            }
            else if (sortBy == LanguagesManager.Instance.StoreDownloadControl_Id)
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
            public string Id { get { return null; } }

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
            public string Id { get; set; }
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
                    || this.Id != other.Id
                    || this.Value != other.Value
                    || this.State != other.State)
                {
                    return false;
                }

                return true;
            }
        }

        private void Execute_New(object sender, ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is StoreCategorizeTreeViewItem)
            {
                _storeCategorizeTreeViewItemNewCategoryMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is StoreTreeViewItem)
            {

            }
            else if (_treeView.SelectedItem is BoxTreeViewItem)
            {

            }
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is StoreCategorizeTreeViewItem)
            {
                _storeCategorizeTreeViewItemDeleteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is StoreTreeViewItem)
            {
                _storeTreeViewItemDeleteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is BoxTreeViewItem)
            {

            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is StoreCategorizeTreeViewItem)
            {
                _storeCategorizeTreeViewItemCopyMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is StoreTreeViewItem)
            {
                _storeTreeViewItemCopyMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is BoxTreeViewItem)
            {
                _boxTreeViewItemCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is StoreCategorizeTreeViewItem)
            {
                _storeCategorizeTreeViewItemCutMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is StoreTreeViewItem)
            {
                _storeTreeViewItemCutMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is BoxTreeViewItem)
            {

            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is StoreCategorizeTreeViewItem)
            {
                _storeCategorizeTreeViewItemPasteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is StoreTreeViewItem)
            {

            }
            else if (_treeView.SelectedItem is BoxTreeViewItem)
            {

            }
        }

        private void Execute_Search(object sender, ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(24);
            _searchTextBox.Focus();
        }
    }
}
