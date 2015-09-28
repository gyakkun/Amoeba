using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// LibraryControl.xaml の相互作用ロジック
    /// </summary>
    partial class LibraryControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private StoreControl _storeControl;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private volatile bool _refresh;
        private volatile bool _cacheUpdate;
        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private BoxTreeViewItem _treeViewItem;
        private ObservableCollectionEx<object> _listViewItemCollection = new ObservableCollectionEx<object>();
        private LockedHashDictionary<Seed, SearchState> _seedsDictionary = new LockedHashDictionary<Seed, SearchState>();

        private Thread _searchThread;
        private Thread _cacheThread;
        private Thread _watchThread;

        public LibraryControl(StoreControl storeControl, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _storeControl = storeControl;
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            _treeViewItem = new BoxTreeViewItem(Settings.Instance.LibraryControl_Box);

            InitializeComponent();

            _treeView.Items.Add(_treeViewItem);

            //try
            //{
            //    _treeViewItem.IsSelected = true;
            //}
            //catch (Exception)
            //{

            //}

            _listView.ItemsSource = _listViewItemCollection;

            foreach (var path in Settings.Instance.LibraryControl_ExpandedPath.ToArray())
            {
                if (path.Count == 0 || path[0] != _treeViewItem.Value.Name) goto End;
                TreeViewItem treeViewItem = _treeViewItem;

                foreach (var name in path.Skip(1))
                {
                    treeViewItem = treeViewItem.Items.OfType<BoxTreeViewItem>().FirstOrDefault(n => n.Value.Name == name);
                    if (treeViewItem == null) goto End;
                }

                treeViewItem.IsExpanded = true;
                continue;

            End: ;

                Settings.Instance.LibraryControl_ExpandedPath.Remove(path);
            }

            SelectionChangedEventHandler selectionChanged = (object sender, SelectionChangedEventArgs e) =>
            {
                if (e.OriginalSource != _mainWindow._tabControl && e.OriginalSource != _storeControl._tabControl) return;

                if (_mainWindow.SelectedTab == MainWindowTabType.Store && _storeControl.SelectedTab == StoreControlTabType.Library)
                {
                    if (!_refresh) this.Update_Title();
                    _autoResetEvent.Set();
                }
            };

            _mainWindow._tabControl.SelectionChanged += selectionChanged;
            _storeControl._tabControl.SelectionChanged += selectionChanged;

            _searchThread = new Thread(this.Search);
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "LibraryControl_SearchThread";
            _searchThread.Start();

            _cacheThread = new Thread(this.Cache);
            _cacheThread.Priority = ThreadPriority.Highest;
            _cacheThread.IsBackground = true;
            _cacheThread.Name = "LibraryControl_CacheThread";
            _cacheThread.Start();

            _watchThread = new Thread(this.Watch);
            _watchThread.Priority = ThreadPriority.Highest;
            _watchThread.IsBackground = true;
            _watchThread.Name = "LibraryControl_WatchThread";
            _watchThread.Start();

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

                    BoxTreeViewItem tempTreeViewItem = null;

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        tempTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
                        _listView.ContextMenu.IsOpen = false;
                    }));

                    if (tempTreeViewItem == null) continue;

                    HashSet<object> newList = new HashSet<object>(new ReferenceEqualityComparer());

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

                    foreach (var box in tempTreeViewItem.Value.Boxes)
                    {
                        if (words != null && words.Length != 0)
                        {
                            var text = (box.Name ?? "").ToLower();
                            if (!words.All(n => text.Contains(n))) continue;
                        }

                        var boxesListViewItem = new BoxListViewItem();
                        boxesListViewItem.Index = newList.Count;
                        boxesListViewItem.Name = box.Name;
                        if (box.Certificate != null) boxesListViewItem.Signature = box.Certificate.ToString();
                        boxesListViewItem.Length = BoxUtilities.GetBoxLength(box);
                        boxesListViewItem.CreationTime = BoxUtilities.GetBoxCreationTime(box);
                        boxesListViewItem.Value = box;

                        newList.Add(boxesListViewItem);
                    }

                    foreach (var seed in tempTreeViewItem.Value.Seeds)
                    {
                        if (words != null && words.Length != 0)
                        {
                            var text = (seed.Name ?? "").ToLower();
                            if (!words.All(n => text.Contains(n))) continue;
                        }

                        var seedListViewItem = new SeedListViewItem();
                        seedListViewItem.Index = newList.Count;
                        seedListViewItem.Name = seed.Name;
                        if (seed.Certificate != null) seedListViewItem.Signature = seed.Certificate.ToString();
                        seedListViewItem.Length = seed.Length;
                        seedListViewItem.Keywords = string.Join(", ", seed.Keywords.Where(n => !string.IsNullOrWhiteSpace(n)));
                        seedListViewItem.CreationTime = seed.CreationTime;
                        seedListViewItem.Id = SeedUtilities.GetHash(seed);

                        SearchState state;

                        if (_seedsDictionary.TryGetValue(seed, out state))
                        {
                            seedListViewItem.State = state;
                        }

                        seedListViewItem.Value = seed;

                        newList.Add(seedListViewItem);
                    }

                    HashSet<object> oldList = new HashSet<object>(new ReferenceEqualityComparer());

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        oldList.UnionWith(_listViewItemCollection.OfType<object>());
                    }));

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

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        if (tempTreeViewItem != _treeView.SelectedItem) return;
                        _refresh = false;

                        _listView.SelectedItems.Clear();

                        bool sortFlag = false;

                        if (removeList.Count > 100)
                        {
                            sortFlag = true;

                            _listViewItemCollection.Clear();

                            foreach (var item in newList)
                            {
                                _listViewItemCollection.Add(item);
                            }
                        }
                        else
                        {
                            if (addList.Count != 0) sortFlag = true;
                            if (removeList.Count != 0) sortFlag = true;

                            foreach (var item in addList)
                            {
                                _listViewItemCollection.Add(item);
                            }

                            foreach (var item in removeList)
                            {
                                _listViewItemCollection.Remove(item);
                            }
                        }

                        if (sortFlag) this.Sort();

                        this.Update_Title();
                    }));
                }
            }
            catch (Exception)
            {

            }
        }

        private void Cache()
        {
            try
            {
                for (; ; )
                {
                    _autoResetEvent.WaitOne(1000 * 60 * 3);

                    while (_mainWindow.SelectedTab != MainWindowTabType.Store || _storeControl.SelectedTab != StoreControlTabType.Library)
                    {
                        Thread.Sleep(1000);
                    }

                    Dictionary<Seed, SearchState> seedsDictionary = new Dictionary<Seed, SearchState>();

                    {
                        foreach (var seed in _amoebaManager.CacheSeeds)
                        {
                            seedsDictionary[seed] = SearchState.Cache;
                        }

                        foreach (var information in _amoebaManager.UploadingInformation)
                        {
                            if (information.Contains("Seed") && ((UploadState)information["State"]) != UploadState.Completed)
                            {
                                var seed = (Seed)information["Seed"];
                                SearchState state;

                                if (seedsDictionary.TryGetValue(seed, out state))
                                {
                                    state |= SearchState.Uploading;
                                    seedsDictionary[seed] = state;
                                }
                                else
                                {
                                    seedsDictionary.Add(seed, SearchState.Uploading);
                                }
                            }
                        }

                        foreach (var information in _amoebaManager.DownloadingInformation)
                        {
                            if (information.Contains("Seed") && ((DownloadState)information["State"]) != DownloadState.Completed)
                            {
                                var seed = (Seed)information["Seed"];
                                SearchState state;

                                if (seedsDictionary.TryGetValue(seed, out state))
                                {
                                    state |= SearchState.Downloading;
                                    seedsDictionary[seed] = state;
                                }
                                else
                                {
                                    seedsDictionary.Add(seed, SearchState.Downloading);
                                }
                            }
                        }

                        foreach (var seed in _amoebaManager.UploadedSeeds)
                        {
                            SearchState state;

                            if (seedsDictionary.TryGetValue(seed, out state))
                            {
                                state |= SearchState.Uploaded;
                                seedsDictionary[seed] = state;
                            }
                            else
                            {
                                seedsDictionary.Add(seed, SearchState.Uploaded);
                            }
                        }

                        foreach (var seed in _amoebaManager.DownloadedSeeds)
                        {
                            SearchState state;

                            if (seedsDictionary.TryGetValue(seed, out state))
                            {
                                state |= SearchState.Downloaded;
                                seedsDictionary[seed] = state;
                            }
                            else
                            {
                                seedsDictionary.Add(seed, SearchState.Downloaded);
                            }
                        }
                    }

                    lock (_seedsDictionary.ThisLock)
                    {
                        _seedsDictionary.Clear();

                        foreach (var pair in seedsDictionary)
                        {
                            _seedsDictionary.Add(pair.Key, pair.Value);
                        }
                    }

                    if (_cacheUpdate)
                    {
                        _cacheUpdate = false;

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
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

        private void Watch()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(1000 * 3);

                    try
                    {
                        if (Directory.Exists(App.DirectoryPaths["Input"]))
                        {
                            this.OpenBox(App.DirectoryPaths["Input"]);
                        }
                    }
                    catch (Exception)
                    {

                    }

                    try
                    {
                        var directory = _amoebaManager.DownloadDirectory;

                        if (Directory.Exists(directory))
                        {
                            this.OpenBox(directory);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void OpenBox(string path)
        {
            foreach (var filePath in Directory.GetFiles(path, "*.box", SearchOption.TopDirectoryOnly))
            {
                if (!filePath.EndsWith(".box")) continue;

                try
                {
                    using (FileStream stream = new FileStream(filePath, FileMode.Open))
                    {
                        var box = AmoebaConverter.FromBoxStream(stream);
                        if (box == null) continue;

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            try
                            {
                                var treeViewItem = this.GetExtractToBox();
                                if (!this.DigitalSignatureRelease(_treeView.GetAncestors(treeViewItem).OfType<BoxTreeViewItem>())) return;

                                if (!LibraryControl.CheckBoxDigitalSignature(ref box))
                                {
                                    if (MessageBox.Show(
                                            _mainWindow,
                                            string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message),
                                            "Library",
                                            MessageBoxButton.OKCancel,
                                            MessageBoxImage.Asterisk) == MessageBoxResult.OK)
                                    {
                                        treeViewItem.Value.Boxes.Remove(box);
                                        treeViewItem.Value.Boxes.Add(box);
                                        treeViewItem.Value.CreationTime = DateTime.UtcNow;
                                    }
                                }
                                else
                                {
                                    treeViewItem.Value.Boxes.Remove(box);
                                    treeViewItem.Value.Boxes.Add(box);
                                    treeViewItem.Value.CreationTime = DateTime.UtcNow;
                                }

                                treeViewItem.Update();
                                this.Update();
                            }
                            catch (Exception)
                            {

                            }
                        }));
                    }
                }
                catch (IOException)
                {
                    continue;
                }
                catch (Exception)
                {

                }

                try
                {
                    File.Delete(filePath);
                }
                catch (Exception)
                {

                }
            }
        }

        private BoxTreeViewItem GetExtractToBox()
        {
            var paths = Settings.Instance.Global_BoxExtractTo_Path.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (paths.Count == 0 || _treeViewItem.Value.Name != paths[0]) return _treeViewItem;

            BoxTreeViewItem treeViewItem = _treeViewItem;

            for (int i = 1; i < paths.Count; i++)
            {
                treeViewItem = treeViewItem.Items.OfType<BoxTreeViewItem>().FirstOrDefault(n => n.Value.Name == paths[i]);
                if (treeViewItem == null) return _treeViewItem;
            }

            return treeViewItem;
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
                    string.Format("{0}\r\n{1}", builder.ToString(), LanguagesManager.Instance.LibraryControl_DigitalSignatureAnnulled_Message),
                    "Library",
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
            _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
            _refresh = true;
        }

        private void Update_Cache()
        {
            this.Update_Cache(true);
        }

        private void Update_Cache(bool update)
        {
            _cacheUpdate = update;
            _autoResetEvent.Set();
        }

        private void Update_Title()
        {
            if (_refresh) return;

            if (_mainWindow.SelectedTab == MainWindowTabType.Store && _storeControl.SelectedTab == StoreControlTabType.Library)
            {
                if (_treeView.SelectedItem is BoxTreeViewItem)
                {
                    var selectTreeViewItem = (BoxTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectTreeViewItem.Value.Name);
                }
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

        private void _listView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released)
            {
                if (_listView.ContextMenu.IsVisible) return;
                if (_startPoint.X == -1 && _startPoint.Y == -1) return;

                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance
                        || Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (_refresh || _listView.SelectedItems.Count == 0) return;

                    DataObject data = new DataObject("ListViewItems", _listView.SelectedItems);
                    DragDrop.DoDragDrop(_grid, data, DragDropEffects.Move);
                }
            }
        }

        private void _grid_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
                e.Handled = true;
            }
        }

        private void _grid_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(item => File.Exists(item)).ToList();

                var destinationItem = this.GetDropDestination(e.GetPosition);
                if (destinationItem == null) destinationItem = (TreeViewItem)_treeView.SelectedItem;

                if (destinationItem is BoxTreeViewItem)
                {
                    var d = (BoxTreeViewItem)destinationItem;

                    if (!this.DigitalSignatureRelease(_treeView.GetAncestors(d).OfType<BoxTreeViewItem>())) return;

                    foreach (string filePath in paths)
                    {
                        using (FileStream stream = new FileStream(filePath, FileMode.Open))
                        {
                            try
                            {
                                var box = AmoebaConverter.FromBoxStream(stream);
                                if (box == null) continue;

                                if (!LibraryControl.CheckBoxDigitalSignature(ref box))
                                {
                                    if (MessageBox.Show(
                                        _mainWindow,
                                        string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message),
                                        "Library",
                                        MessageBoxButton.OKCancel,
                                        MessageBoxImage.Asterisk) == MessageBoxResult.OK)
                                    {
                                        d.Value.Boxes.Add(box);
                                        d.Value.CreationTime = DateTime.UtcNow;
                                    }
                                }
                                else
                                {
                                    d.Value.Boxes.Add(box);
                                    d.Value.CreationTime = DateTime.UtcNow;
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }

                    d.Update();
                }
            }
            else
            {
                if (e.Data.GetDataPresent("TreeViewItem"))
                {
                    var sourceItem = (TreeViewItem)e.Data.GetData("TreeViewItem");

                    if (sourceItem is BoxTreeViewItem)
                    {
                        var destinationItem = this.GetDropDestination(e.GetPosition);

                        if (destinationItem is BoxTreeViewItem)
                        {
                            var s = (BoxTreeViewItem)sourceItem;
                            var d = (BoxTreeViewItem)destinationItem;

                            if (d.Value.Boxes.Any(n => object.ReferenceEquals(n, s.Value))) return;
                            if (_treeView.GetAncestors(d).Any(n => object.ReferenceEquals(n, s))) return;

                            if (!this.DigitalSignatureRelease(_treeView.GetAncestors(s).OfType<BoxTreeViewItem>().Where(n => n != s))) return;
                            if (!this.DigitalSignatureRelease(_treeView.GetAncestors(d).OfType<BoxTreeViewItem>())) return;

                            var parentItem = s.Parent;

                            if (parentItem is BoxTreeViewItem)
                            {
                                var p = (BoxTreeViewItem)parentItem;

                                var tItems = p.Value.Boxes.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                                p.Value.Boxes.Clear();
                                p.Value.Boxes.AddRange(tItems);
                                p.Value.CreationTime = DateTime.UtcNow;

                                p.Update();
                            }

                            d.Value.Boxes.Add(s.Value);
                            d.Value.CreationTime = DateTime.UtcNow;
                            d.IsSelected = true;

                            d.Update();
                        }
                    }
                }
                else if (e.Data.GetDataPresent("ListViewItems"))
                {
                    var boxes = ((IList)e.Data.GetData("ListViewItems")).OfType<BoxListViewItem>().Select(n => n.Value).ToList();
                    var seeds = ((IList)e.Data.GetData("ListViewItems")).OfType<SeedListViewItem>().Select(n => n.Value).ToList();
                    bool isListView = _listView.GetCurrentIndex(e.GetPosition) != -1;

                    var parentItem = (TreeViewItem)_treeView.SelectedItem;

                    if (parentItem is BoxTreeViewItem)
                    {
                        var destinationItem = this.GetDropDestination(e.GetPosition);

                        if (destinationItem is BoxTreeViewItem)
                        {
                            var p = (BoxTreeViewItem)parentItem;
                            var d = (BoxTreeViewItem)destinationItem;

                            if (p == d) return;

                            boxes = boxes.Where(n => !object.ReferenceEquals(n, d.Value)).ToList();

                            if (boxes.Count == 0 && seeds.Count == 0) return;

                            if (!this.DigitalSignatureRelease(_treeView.GetAncestors(p).OfType<BoxTreeViewItem>())) return;
                            if (!this.DigitalSignatureRelease(_treeView.GetAncestors(d).OfType<BoxTreeViewItem>())) return;

                            var tboxes = p.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                            p.Value.Boxes.Clear();
                            p.Value.Boxes.AddRange(tboxes);
                            var tseeds = p.Value.Seeds.Where(n => !seeds.Any(m => object.ReferenceEquals(n, m))).ToArray();
                            p.Value.Seeds.Clear();
                            p.Value.Seeds.AddRange(tseeds);
                            p.Value.CreationTime = DateTime.UtcNow;

                            p.Update();

                            d.Value.Boxes.AddRange(boxes);
                            d.Value.Seeds.AddRange(seeds);
                            d.Value.CreationTime = DateTime.UtcNow;
                            if (!isListView) d.IsSelected = true;

                            d.Update();
                        }
                    }
                }
            }

            this.Update();
        }

        private TreeViewItem GetDropDestination(GetPositionDelegate getPosition)
        {
            var posithonIndex = _listView.GetCurrentIndex(getPosition);

            if (posithonIndex != -1)
            {
                var selectItem = _treeView.SelectedItem;

                if (selectItem is BoxTreeViewItem)
                {
                    var listViewItem = _listView.Items[posithonIndex];

                    if (listViewItem is BoxListViewItem)
                    {
                        var selectTreeViewItem = (BoxTreeViewItem)selectItem;
                        var boxListViewItem = (BoxListViewItem)listViewItem;

                        return selectTreeViewItem.Items.OfType<BoxTreeViewItem>().FirstOrDefault(n => object.ReferenceEquals(n.Value, boxListViewItem.Value));
                    }
                }
            }
            else
            {
                return (TreeViewItem)_treeView.GetCurrentItem(getPosition);
            }

            return null;
        }

        private TreeViewItem GetSelectedItem()
        {
            var selectIndex = _listView.SelectedIndex;

            if (selectIndex != -1)
            {
                var selectItem = _treeView.SelectedItem;

                if (selectItem is BoxTreeViewItem)
                {
                    var listViewItem = _listView.Items[selectIndex];

                    if (listViewItem is BoxListViewItem)
                    {
                        var selectTreeViewItem = (BoxTreeViewItem)selectItem;
                        var boxListViewItem = (BoxListViewItem)listViewItem;

                        return selectTreeViewItem.Items.OfType<BoxTreeViewItem>().FirstOrDefault(n => object.ReferenceEquals(n.Value, boxListViewItem.Value));
                    }
                }
            }
            else
            {
                return (TreeViewItem)_treeView.SelectedItem;
            }

            return null;
        }

        #endregion

        #region _treeView

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

        private void _treeView_Expanded(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            Route path = new Route();

            foreach (var item in _treeView.GetAncestors(treeViewItem))
            {
                if (item is BoxTreeViewItem) path.Add(((BoxTreeViewItem)item).Value.Name);
            }

            Settings.Instance.LibraryControl_ExpandedPath.Add(path);
        }

        private void _treeView_Collapsed(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            Route path = new Route();

            foreach (var item in _treeView.GetAncestors(treeViewItem))
            {
                if (item is BoxTreeViewItem) path.Add(((BoxTreeViewItem)item).Value.Name);
            }

            Settings.Instance.LibraryControl_ExpandedPath.Remove(path);
        }

        private void _treeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = sender as BoxTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            _startPoint = new Point(-1, -1);

            MenuItem treeViewItemDeleteMenuItem = contextMenu.GetMenuItem("_treeViewItemDeleteMenuItem");
            MenuItem treeViewItemCutMenuItem = contextMenu.GetMenuItem("_treeViewItemCutMenuItem");
            MenuItem treeViewItemPasteMenuItem = contextMenu.GetMenuItem("_treeViewItemPasteMenuItem");

            treeViewItemDeleteMenuItem.IsEnabled = (selectTreeViewItem != _treeViewItem);
            treeViewItemCutMenuItem.IsEnabled = (selectTreeViewItem != _treeViewItem);
            treeViewItemPasteMenuItem.IsEnabled = Clipboard.ContainsBoxes() || Clipboard.ContainsSeeds();
        }

        private void _treeViewItemNewBoxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeView.GetAncestors(selectTreeViewItem).OfType<BoxTreeViewItem>())) return;

            Box box;

            if (!selectTreeViewItem.Value.Boxes.Any(n => n.Name == "New box"))
            {
                box = new Box() { Name = "New box", CreationTime = DateTime.UtcNow };
            }
            else
            {
                int i = 1;
                while (selectTreeViewItem.Value.Boxes.Any(n => n.Name == "New box_" + i)) i++;

                box = new Box() { Name = "New box_" + i, CreationTime = DateTime.UtcNow };
            }

            BoxEditWindow window = new BoxEditWindow(box);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.Boxes.Add(box);
                selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _treeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeView.GetAncestors(selectTreeViewItem).Where(n => n != selectTreeViewItem).OfType<BoxTreeViewItem>())) return;

            var box = selectTreeViewItem.Value;

            BoxEditWindow window = new BoxEditWindow(box);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _treeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            if (!this.DigitalSignatureRelease(_treeView.GetAncestors(selectTreeViewItem).Where(n => n != selectTreeViewItem).OfType<BoxTreeViewItem>())) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Library", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parentItem = selectTreeViewItem.Parent;

            if (parentItem is BoxTreeViewItem)
            {
                var p = (BoxTreeViewItem)parentItem;

                p.Value.Boxes.Remove(selectTreeViewItem.Value);
                p.Value.CreationTime = DateTime.UtcNow;

                p.Update();
            }

            this.Update();
        }

        private void _treeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            if (!this.DigitalSignatureRelease(_treeView.GetAncestors(selectTreeViewItem).Where(n => n != selectTreeViewItem).OfType<BoxTreeViewItem>())) return;

            Clipboard.SetBoxes(new Box[] { selectTreeViewItem.Value });

            var parentItem = selectTreeViewItem.Parent;

            if (parentItem is BoxTreeViewItem)
            {
                var p = (BoxTreeViewItem)parentItem;

                p.Value.Boxes.Remove(selectTreeViewItem.Value);
                p.Value.CreationTime = DateTime.UtcNow;

                p.Update();
            }

            this.Update();
        }

        private void _treeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetBoxes(new Box[] { selectTreeViewItem.Value });
        }

        private void _treeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeView.GetAncestors(selectTreeViewItem).OfType<BoxTreeViewItem>())) return;

            selectTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes());
            selectTreeViewItem.Value.Seeds.AddRange(Clipboard.GetSeeds());
            selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _treeViewItemImportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeView.GetAncestors(selectTreeViewItem).OfType<BoxTreeViewItem>())) return;

            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.RestoreDirectory = true;
                dialog.DefaultExt = ".box";
                dialog.Filter = "Box (*.box)|*.box";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (var filePath in dialog.FileNames)
                    {
                        try
                        {
                            using (FileStream stream = new FileStream(filePath, FileMode.Open))
                            {
                                var box = AmoebaConverter.FromBoxStream(stream);
                                if (box == null) continue;

                                if (!LibraryControl.CheckBoxDigitalSignature(ref box))
                                {
                                    if (MessageBox.Show(
                                            _mainWindow,
                                            string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message),
                                            "Library",
                                            MessageBoxButton.OKCancel,
                                            MessageBoxImage.Asterisk) == MessageBoxResult.OK)
                                    {
                                        selectTreeViewItem.Value.Boxes.Add(box);
                                    }
                                }
                                else
                                {
                                    selectTreeViewItem.Value.Boxes.Add(box);
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }

                    selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;
                    selectTreeViewItem.Update();
                    this.Update();
                }
            }
        }

        private void _treeViewItemExportMenuItem_Click(object sender, RoutedEventArgs e)
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

                    using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                    using (Stream boxStream = AmoebaConverter.ToBoxStream(selectTreeViewItem.Value))
                    {
                        byte[] buffer = null;

                        try
                        {
                            buffer = _bufferManager.TakeBuffer(1024 * 4);

                            int i = -1;

                            while ((i = boxStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, i);
                            }
                        }
                        finally
                        {
                            if (buffer != null)
                            {
                                _bufferManager.ReturnBuffer(buffer);
                            }
                        }
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

                if (selectItem is BoxTreeViewItem)
                {
                    var listViewItem = _listView.Items[posithonIndex];

                    if (listViewItem is BoxListViewItem)
                    {
                        var selectTreeViewItem = (BoxTreeViewItem)selectItem;
                        var boxListViewItem = (BoxListViewItem)listViewItem;

                        var item = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().FirstOrDefault(n => object.ReferenceEquals(n.Value, boxListViewItem.Value));

                        try
                        {
                            selectTreeViewItem.IsExpanded = true;
                            item.IsSelected = true;
                        }
                        catch (Exception)
                        {

                        }
                    }
                    else if (listViewItem is SeedListViewItem)
                    {
                        var selectTreeViewItem = (BoxTreeViewItem)selectItem;
                        var seedListViewItem = (SeedListViewItem)listViewItem;

                        string baseDirectory = "";

                        {
                            List<string> path = new List<string>();

                            foreach (var item in _treeView.GetAncestors(selectTreeViewItem))
                            {
                                if (item is BoxTreeViewItem) path.Add(((BoxTreeViewItem)item).Value.Name);
                            }

                            baseDirectory = System.IO.Path.Combine(path.ToArray());
                        }

                        var seed = seedListViewItem.Value;

                        ThreadPool.QueueUserWorkItem((object wstate) =>
                        {
                            Thread.CurrentThread.IsBackground = true;

                            try
                            {
                                _amoebaManager.Download(seed, baseDirectory, 3);

                                this.Update_Cache(false);
                            }
                            catch (Exception)
                            {

                            }
                        });
                    }
                }
            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _startPoint = new Point(-1, -1);

            if (_refresh || _treeView.SelectedItem == null)
            {
                _listViewContextMenu.IsEnabled = false;

                e.Handled = true;
            }
            else
            {
                _listViewContextMenu.IsEnabled = true;

                var selectItems = _listView.SelectedItems;

                _listViewNewBoxMenuItem.IsEnabled = true;
                _listViewEditMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _listViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _listViewCutMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _listViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _listViewCopyInfoMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
                _listViewDownloadMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

                // Paste
                {
                    var destinationItem = this.GetSelectedItem();

                    if (destinationItem is BoxTreeViewItem)
                    {
                        _listViewPasteMenuItem.IsEnabled = Clipboard.ContainsBoxes() || Clipboard.ContainsSeeds();
                    }
                }
            }
        }

        private void _listViewNewBoxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var destinationItem = this.GetSelectedItem();

            if (destinationItem is BoxTreeViewItem)
            {
                var d = (BoxTreeViewItem)destinationItem;

                if (!this.DigitalSignatureRelease(_treeView.GetAncestors(d).OfType<BoxTreeViewItem>())) return;

                Box box;

                if (!d.Value.Boxes.Any(n => n.Name == "New box"))
                {
                    box = new Box() { Name = "New box", CreationTime = DateTime.UtcNow };
                }
                else
                {
                    int i = 1;
                    while (d.Value.Boxes.Any(n => n.Name == "New box_" + i)) i++;

                    box = new Box() { Name = "New box_" + i, CreationTime = DateTime.UtcNow };
                }

                BoxEditWindow window = new BoxEditWindow(box);
                window.Owner = _mainWindow;

                if (window.ShowDialog() == true)
                {
                    d.Value.Boxes.Add(box);
                    d.Value.CreationTime = DateTime.UtcNow;

                    d.Update();
                }
            }

            this.Update();
        }

        private void _listViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;
            if (selectTreeViewItem == null) return;

            if (selectTreeViewItem is BoxTreeViewItem)
            {
                var selectBoxTreeViewItem = (BoxTreeViewItem)selectTreeViewItem;

                if (_listView.SelectedItem is BoxListViewItem)
                {
                    var selectBoxListViewItems = _listView.SelectedItems.OfType<BoxListViewItem>();
                    if (selectBoxListViewItems == null) return;

                    if (!this.DigitalSignatureRelease(_treeView.GetAncestors(selectBoxTreeViewItem).OfType<BoxTreeViewItem>())) return;

                    var editBoxs = selectBoxListViewItems.Select(n => n.Value.Clone()).ToList();
                    if (editBoxs == null) return;

                    BoxEditWindow window = new BoxEditWindow(editBoxs);
                    window.Owner = _mainWindow;

                    if (window.ShowDialog() == true)
                    {
                        foreach (var item in selectBoxListViewItems)
                        {
                            selectBoxTreeViewItem.Value.Boxes.Remove(item.Value);
                        }

                        foreach (var seed in editBoxs)
                        {
                            selectBoxTreeViewItem.Value.Boxes.Add(seed);
                        }

                        selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                        selectBoxTreeViewItem.Update();
                    }
                }
                else if (_listView.SelectedItem is SeedListViewItem)
                {
                    var selectSeedListViewItems = _listView.SelectedItems.OfType<SeedListViewItem>();
                    if (selectSeedListViewItems == null) return;

                    if (!this.DigitalSignatureRelease(_treeView.GetAncestors(selectBoxTreeViewItem).OfType<BoxTreeViewItem>())) return;

                    var editSeeds = selectSeedListViewItems.Select(n => n.Value.Clone()).ToList();
                    if (editSeeds == null) return;

                    SeedEditWindow window = new SeedEditWindow(editSeeds);
                    window.Owner = _mainWindow;

                    if (window.ShowDialog() == true)
                    {
                        foreach (var item in selectSeedListViewItems)
                        {
                            selectBoxTreeViewItem.Value.Seeds.Remove(item.Value);
                        }

                        foreach (var seed in editSeeds)
                        {
                            selectBoxTreeViewItem.Value.Seeds.Add(seed);
                        }

                        selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                        selectBoxTreeViewItem.Update();
                    }
                }
            }

            this.Update();
        }

        private void _listViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;

            var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;
            if (selectTreeViewItem == null) return;

            if (selectTreeViewItem is BoxTreeViewItem)
            {
                var selectBoxTreeViewItem = (BoxTreeViewItem)selectTreeViewItem;

                if (!this.DigitalSignatureRelease(_treeView.GetAncestors(selectBoxTreeViewItem).OfType<BoxTreeViewItem>())) return;
                if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

                var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
                var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

                foreach (var item in boxes)
                {
                    selectBoxTreeViewItem.Value.Boxes.Remove(item);
                }

                foreach (var item in seeds)
                {
                    selectBoxTreeViewItem.Value.Seeds.Remove(item);
                }

                selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                selectBoxTreeViewItem.Update();
            }

            this.Update();
        }

        private void _listViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;

            var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;
            if (selectTreeViewItem == null) return;

            if (selectTreeViewItem is BoxTreeViewItem)
            {
                var selectBoxTreeViewItem = (BoxTreeViewItem)selectTreeViewItem;

                if (!this.DigitalSignatureRelease(_treeView.GetAncestors(selectBoxTreeViewItem).OfType<BoxTreeViewItem>())) return;

                var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
                var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

                Clipboard.SetBoxAndSeeds(boxes, seeds);

                foreach (var item in boxes)
                {
                    selectBoxTreeViewItem.Value.Boxes.Remove(item);
                }

                foreach (var item in seeds)
                {
                    selectBoxTreeViewItem.Value.Seeds.Remove(item);
                }

                selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                selectBoxTreeViewItem.Update();
            }

            this.Update();
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

        private void _listViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var destinationItem = this.GetSelectedItem();

            if (destinationItem is BoxTreeViewItem)
            {
                var d = (BoxTreeViewItem)destinationItem;

                if (!this.DigitalSignatureRelease(_treeView.GetAncestors(d).OfType<BoxTreeViewItem>())) return;

                d.Value.Boxes.AddRange(Clipboard.GetBoxes());
                d.Value.Seeds.AddRange(Clipboard.GetSeeds());
                d.Value.CreationTime = DateTime.UtcNow;

                d.Update();
            }

            this.Update();
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

            var list = new List<KeyValuePair<Seed, string>>();

            {
                foreach (var seed in _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value))
                {
                    list.Add(new KeyValuePair<Seed, string>(seed.Clone(), baseDirectory));
                }

                foreach (var box in _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value))
                {
                    this.BoxDownload(box, baseDirectory, ref list);
                }
            }

            ThreadPool.QueueUserWorkItem((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var pair in list)
                    {
                        var seed = pair.Key;
                        var path = pair.Value;

                        _amoebaManager.Download(seed, path, 3);
                    }

                    this.Update_Cache(false);
                }
                catch (Exception)
                {

                }
            });
        }

        private void BoxDownload(Box currentBox, string baseDirectory, ref List<KeyValuePair<Seed, string>> list)
        {
            baseDirectory = System.IO.Path.Combine(baseDirectory, currentBox.Name);

            foreach (var seed in currentBox.Seeds)
            {
                list.Add(new KeyValuePair<Seed, string>(seed.Clone(), baseDirectory));
            }

            foreach (var box in currentBox.Boxes)
            {
                this.BoxDownload(box, baseDirectory, ref list);
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

                if (headerClicked != Settings.Instance.LibraryControl_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.LibraryControl_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                this.Sort(headerClicked, direction);

                Settings.Instance.LibraryControl_LastHeaderClicked = headerClicked;
                Settings.Instance.LibraryControl_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.LibraryControl_LastHeaderClicked != null)
                {
                    this.Sort(Settings.Instance.LibraryControl_LastHeaderClicked, Settings.Instance.LibraryControl_ListSortDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.LibraryControl_Id)
            {
                ListCollectionView view = (ListCollectionView)CollectionViewSource.GetDefaultView(_listView.ItemsSource);
                view.CustomSort = new ComparerListener<dynamic>((x, y) =>
                {
                    {
                        int c = x.Type.CompareTo(y.Type);
                        if (c != 0) return c;
                    }

                    {
                        if (x.Id != null && y.Id != null)
                        {
                            int c = Unsafe.Compare(x.Id, y.Id);
                            if (c != 0) return c;
                        }
                        else if (x.Id == null && y.Id != null)
                        {
                            return -1;
                        }
                        else if (x.Id != null && y.Id == null)
                        {
                            return 1;
                        }
                    }

                    {
                        int c = x.Name.CompareTo(y.Name);
                        if (c != 0) return c;
                        c = x.Index.CompareTo(y.Index);
                        if (c != 0) return c;
                    }

                    return 0;
                }, direction);

                view.Refresh();
            }
            else
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Type", direction));

                if (sortBy == LanguagesManager.Instance.LibraryControl_Name)
                {

                }
                else if (sortBy == LanguagesManager.Instance.LibraryControl_Signature)
                {
                    _listView.Items.SortDescriptions.Add(new SortDescription("Signature", direction));
                }
                else if (sortBy == LanguagesManager.Instance.SearchControl_Length)
                {
                    _listView.Items.SortDescriptions.Add(new SortDescription("Length", direction));
                }
                else if (sortBy == LanguagesManager.Instance.SearchControl_Keywords)
                {
                    _listView.Items.SortDescriptions.Add(new SortDescription("Keywords", direction));
                }
                else if (sortBy == LanguagesManager.Instance.LibraryControl_CreationTime)
                {
                    _listView.Items.SortDescriptions.Add(new SortDescription("CreationTime", direction));
                }
                else if (sortBy == LanguagesManager.Instance.LibraryControl_Comment)
                {
                    _listView.Items.SortDescriptions.Add(new SortDescription("Comment", direction));
                }
                else if (sortBy == LanguagesManager.Instance.LibraryControl_State)
                {
                    _listView.Items.SortDescriptions.Add(new SortDescription("State", direction));
                }

                _listView.Items.SortDescriptions.Add(new SortDescription("Name", direction));
                _listView.Items.SortDescriptions.Add(new SortDescription("Index", direction));
            }
        }

        private class ComparerListener<T> : IComparer<T>, IComparer
        {
            private Comparison<T> _comparison;
            private ListSortDirection _direction;

            public ComparerListener(Comparison<T> comparison, ListSortDirection direction)
            {
                _comparison = comparison;
                _direction = direction;
            }

            public int Compare(T x, T y)
            {
                if (_direction == ListSortDirection.Ascending) return _comparison(x, y);
                else if (_direction == ListSortDirection.Descending) return -1 * _comparison(x, y);

                return 0;
            }

            public int Compare(object x, object y)
            {
                return this.Compare((T)x, (T)y);
            }
        }

        #endregion

        private class BoxListViewItem : IEquatable<BoxListViewItem>
        {
            public int Type { get { return 0; } }
            public int Index { get; set; }
            public string Name { get; set; }
            public string Signature { get; set; }
            public long Length { get; set; }
            public string Keywords { get { return null; } }
            public DateTime CreationTime { get; set; }
            public SearchState State { get { return (SearchState)0; } }
            public byte[] Id { get { return null; } }
            public Box Value { get; set; }

            public override int GetHashCode()
            {
                if (this.Name == null) return 0;
                else return this.Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if ((object)obj == null || !(obj is BoxListViewItem)) return false;

                return this.Equals((BoxListViewItem)obj);
            }

            public bool Equals(BoxListViewItem other)
            {
                if ((object)other == null) return false;
                if (object.ReferenceEquals(this, other)) return true;

                if (this.Index != other.Index
                    || this.Name != other.Name
                    || this.Signature != other.Signature
                    || this.Length != other.Length
                    || this.CreationTime != other.CreationTime
                    || this.State != other.State
                    || this.Id != other.Id
                    || this.Value != other.Value)
                {
                    return false;
                }

                return true;
            }
        }

        private class SeedListViewItem : IEquatable<SeedListViewItem>
        {
            public int Type { get { return 1; } }
            public int Index { get; set; }
            public string Name { get; set; }
            public string Signature { get; set; }
            public long Length { get; set; }
            public string Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public SearchState State { get; set; }
            public byte[] Id { get; set; }
            public Seed Value { get; set; }

            public override int GetHashCode()
            {
                if (this.Name == null) return 0;
                else return this.Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if ((object)obj == null || !(obj is SeedListViewItem)) return false;

                return this.Equals((SeedListViewItem)obj);
            }

            public bool Equals(SeedListViewItem other)
            {
                if ((object)other == null) return false;
                if (object.ReferenceEquals(this, other)) return true;

                if (this.Index != other.Index
                    || this.Name != other.Name
                    || this.Signature != other.Signature
                    || this.Length != other.Length
                    || this.Keywords != other.Keywords
                    || this.CreationTime != other.CreationTime
                    || this.State != other.State
                    || this.Id != other.Id
                    || this.Value != other.Value)
                {
                    return false;
                }

                return true;
            }
        }

        private void Execute_New(object sender, ExecutedRoutedEventArgs e)
        {
            _treeViewItemNewBoxMenuItem_Click(null, null);
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
                _listViewCutMenuItem_Click(null, null);
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
