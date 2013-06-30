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
    /// Interaction logic for StoreControl.xaml
    /// </summary>
    partial class StoreControl : UserControl
    {
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private volatile bool _refresh = false;

        private Thread _searchThread;

        private ObservableCollection<StoreTreeViewItem> _treeViewItemCollection = new ObservableCollection<StoreTreeViewItem>();

        public StoreControl(MainWindow mainWindow, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            InitializeComponent();

            foreach (var item in Settings.Instance.StoreControl_StoreTreeItems)
            {
                _treeViewItemCollection.Add(new StoreTreeViewItem(item));
            }

            _treeView.ItemsSource = _treeViewItemCollection;

            {
                foreach (var path in Settings.Instance.StoreControl_ExpandedPath.ToArray())
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

                    Settings.Instance.StoreControl_ExpandedPath.Remove(path);
                }
            }

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                if (App.SelectTab == TabItemType.Store && !_refresh)
                {
                    if (_treeView.SelectedItem == null)
                    {
                        _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
                    }
                    else if (_treeView.SelectedItem is StoreTreeViewItem)
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
            _searchThread.Name = "StoreControl_SearchThread";
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
                            boxesListViewItem.Length = StoreControl.GetBoxLength(box);
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

                            if (App.SelectTab == TabItemType.Store)
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
                            boxesListViewItem.Length = StoreControl.GetBoxLength(box);
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

                            if (App.SelectTab == TabItemType.Store)
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

        private bool DigitalSignatureRelease(IEnumerable<Box> boxTreeViewItemCollectiion)
        {
            List<Box> boxList = new List<Box>();
            StringBuilder builder = new StringBuilder();

            foreach (var item in boxTreeViewItemCollectiion)
            {
                if (item.Certificate != null)
                {
                    boxList.Add(item);
                    builder.AppendLine(string.Format("\"{0}\"", item.Name));
                }
            }

            if (boxList.Count == 0) return true;

            if (MessageBox.Show(
                    _mainWindow,
                    string.Format("{0}\r\n{1}", builder.ToString(), LanguagesManager.Instance.StoreControl_DigitalSignatureAnnulled_Message),
                    "Store",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                var items = new List<BoxTreeViewItem>();

                foreach (var storeTreeViewItem in _treeViewItemCollection)
                {
                    items.AddRange(storeTreeViewItem.Items.OfType<BoxTreeViewItem>());
                }

                for (int i = 0; i < items.Count; i++)
                {
                    foreach (BoxTreeViewItem item in items[i].Items)
                    {
                        items.Add(item);
                    }
                }

                foreach (var item in boxList)
                {
                    var t = items.FirstOrDefault(n => object.ReferenceEquals(n.Value, item));

                    item.CreateCertificate(null);
                    t.Update();
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
                length += StoreControl.GetBoxLength(item);
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

            Settings.Instance.StoreControl_StoreTreeItems = _treeViewItemCollection.Select(n => n.Value).ToLockedList();

            _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
            _refresh = true;
        }

        #region Grid

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
                    if (!(_treeView.SelectedItem is BoxTreeViewItem)) return;

                    DataObject data = new DataObject("BoxTreeViewItem", _treeView.SelectedItem);
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
                    if (!_refresh)
                    {
                        if (_listView.SelectedItems.Count == 0) return;

                        DataObject data = new DataObject("ListViewItem", _listView.SelectedItems);
                        DragDrop.DoDragDrop(_grid, data, DragDropEffects.Move);
                    }
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
                var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;
                if (selectTreeViewItem == null) return;

                var posithonIndex = _listView.GetCurrentIndex(e.GetPosition);

                if (posithonIndex != -1)
                {
                    var tempListViewItem = _listView.Items[posithonIndex] as BoxListViewItem;
                    var tempBoxTreeViewItem = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().FirstOrDefault(n => object.ReferenceEquals(n.Value, tempListViewItem.Value));

                    if (tempBoxTreeViewItem != null)
                    {
                        selectTreeViewItem = tempBoxTreeViewItem;
                    }
                }
                else
                {
                    var tempTreeViewItem = _treeView.GetCurrentItem(e.GetPosition) as TreeViewItem;
                    if (tempTreeViewItem != null) selectTreeViewItem = tempTreeViewItem;
                }

                foreach (string filePath in ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(item => File.Exists(item)))
                {
                    if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                    using (FileStream stream = new FileStream(filePath, FileMode.Open))
                    {
                        try
                        {
                            var box = AmoebaConverter.FromBoxStream(stream);
                            bool flag = false;

                            if (!StoreControl.CheckBoxDigitalSignature(ref box))
                            {
                                if (MessageBox.Show(
                                    _mainWindow,
                                    string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.BoxControl_DigitalSignatureError_Message),
                                    "Store",
                                    MessageBoxButton.OKCancel,
                                    MessageBoxImage.Asterisk) == MessageBoxResult.OK)
                                {
                                    flag = true;
                                }
                            }
                            else
                            {
                                flag = true;
                            }

                            if (flag)
                            {
                                if (selectTreeViewItem is StoreTreeViewItem)
                                {
                                    var selectStoreTreeViewItem = (StoreTreeViewItem)selectTreeViewItem;

                                    selectStoreTreeViewItem.Value.Boxes.Add(box);
                                    selectStoreTreeViewItem.Update();
                                }
                                else if (selectTreeViewItem is BoxTreeViewItem)
                                {
                                    var selectBoxTreeViewItem = (BoxTreeViewItem)selectTreeViewItem;

                                    selectBoxTreeViewItem.Value.Boxes.Add(box);
                                    selectBoxTreeViewItem.Update();
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }
            else
            {
                if (e.Data.GetDataPresent("BoxTreeViewItem"))
                {
                    var s = (BoxTreeViewItem)e.Data.GetData("BoxTreeViewItem");
                    var currentItem = _treeView.GetCurrentItem(e.GetPosition);

                    if (currentItem is StoreTreeViewItem)
                    {
                        var t = (StoreTreeViewItem)currentItem;
                        if (t.Value.Boxes.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetLineage(t).Any(n => object.ReferenceEquals(n, s))) return;

                        var list = _treeView.GetLineage(s).OfType<TreeViewItem>().ToList();

                        if (!this.DigitalSignatureRelease(_treeView.GetLineage(t).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
                        if (!this.DigitalSignatureRelease(_treeView.GetLineage(s).OfType<BoxTreeViewItem>().Where(n => n != s).Select(n => n.Value))) return;

                        t.IsSelected = true;

                        if (list[list.Count - 2] is StoreTreeViewItem)
                        {
                            var target = (StoreTreeViewItem)list[list.Count - 2];

                            var tboxes = target.Value.Boxes.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            target.Value.Boxes.Clear();
                            target.Value.Boxes.AddRange(tboxes);

                            target.Update();
                        }
                        else if (list[list.Count - 2] is BoxTreeViewItem)
                        {
                            var target = (BoxTreeViewItem)list[list.Count - 2];

                            var tboxes = target.Value.Boxes.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            target.Value.Boxes.Clear();
                            target.Value.Boxes.AddRange(tboxes);
                            target.Value.CreationTime = DateTime.UtcNow;

                            target.Update();
                        }

                        t.Value.Boxes.Add(s.Value);
                        t.Update();
                    }
                    else if (currentItem is BoxTreeViewItem)
                    {
                        var t = (BoxTreeViewItem)currentItem;
                        if (s == t
                            || t.Value.Boxes.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetLineage(t).Any(n => object.ReferenceEquals(n, s))) return;

                        var list = _treeView.GetLineage(s).OfType<TreeViewItem>().ToList();

                        if (!this.DigitalSignatureRelease(_treeView.GetLineage(t).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
                        if (!this.DigitalSignatureRelease(_treeView.GetLineage(s).OfType<BoxTreeViewItem>().Where(n => n != s).Select(n => n.Value))) return;

                        t.IsSelected = true;

                        if (list[list.Count - 2] is StoreTreeViewItem)
                        {
                            var target = (StoreTreeViewItem)list[list.Count - 2];

                            var tboxes = target.Value.Boxes.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            target.Value.Boxes.Clear();
                            target.Value.Boxes.AddRange(tboxes);

                            target.Update();
                        }
                        else if (list[list.Count - 2] is BoxTreeViewItem)
                        {
                            var target = (BoxTreeViewItem)list[list.Count - 2];

                            var tboxes = target.Value.Boxes.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            target.Value.Boxes.Clear();
                            target.Value.Boxes.AddRange(tboxes);
                            target.Value.CreationTime = DateTime.UtcNow;

                            target.Update();
                        }

                        t.Value.Boxes.Add(s.Value);
                        t.Value.CreationTime = DateTime.UtcNow;
                        t.Update();
                    }
                }
                else if (e.Data.GetDataPresent("ListViewItem"))
                {
                    var boxes = ((IList)e.Data.GetData("ListViewItem")).OfType<BoxListViewItem>().Select(n => n.Value).ToList();
                    var seeds = ((IList)e.Data.GetData("ListViewItem")).OfType<SeedListViewItem>().Select(n => n.Value).ToList();

                    if (e.Source.GetType() == typeof(ListViewEx))
                    {
                        if (_treeView.SelectedItem is StoreTreeViewItem)
                        {
                            var selectTreeViewItem = (StoreTreeViewItem)_treeView.SelectedItem;
                            if (selectTreeViewItem == null) return;

                            int index = _listView.GetCurrentIndex(e.GetPosition);
                            if (index == -1) return;

                            var tl = _listView.Items[index] as BoxListViewItem;
                            if (tl == null) return;

                            var t = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                            boxes = boxes.Where(n => !object.ReferenceEquals(n, t.Value)).ToList();

                            if (boxes.Count == 0 && seeds.Count == 0) return;

                            if (!this.DigitalSignatureRelease(new Box[] { t.Value })) return;
                            if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                            var tboxes = selectTreeViewItem.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                            selectTreeViewItem.Value.Boxes.Clear();
                            selectTreeViewItem.Value.Boxes.AddRange(tboxes);

                            t.Value.Boxes.AddRange(boxes);
                            t.Value.CreationTime = DateTime.UtcNow;

                            selectTreeViewItem.Update();
                            t.Update();
                        }
                        else if (_treeView.SelectedItem is BoxTreeViewItem)
                        {
                            var selectTreeViewItem = (BoxTreeViewItem)_treeView.SelectedItem;
                            if (selectTreeViewItem == null) return;

                            int index = _listView.GetCurrentIndex(e.GetPosition);
                            if (index == -1) return;

                            var tl = _listView.Items[index] as BoxListViewItem;
                            if (tl == null) return;

                            var t = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                            boxes = boxes.Where(n => !object.ReferenceEquals(n, t.Value)).ToList();

                            if (boxes.Count == 0 && seeds.Count == 0) return;

                            if (!this.DigitalSignatureRelease(new Box[] { t.Value })) return;
                            if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                            var tboxes = selectTreeViewItem.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                            selectTreeViewItem.Value.Boxes.Clear();
                            selectTreeViewItem.Value.Boxes.AddRange(tboxes);
                            var tseeds = selectTreeViewItem.Value.Seeds.Where(n => !seeds.Any(m => object.ReferenceEquals(n, m))).ToArray();
                            selectTreeViewItem.Value.Seeds.Clear();
                            selectTreeViewItem.Value.Seeds.AddRange(tseeds);
                            selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                            t.Value.Boxes.AddRange(boxes);
                            t.Value.Seeds.AddRange(seeds);
                            t.Value.CreationTime = DateTime.UtcNow;

                            selectTreeViewItem.Update();
                            t.Update();
                        }
                    }
                    else
                    {
                        var currentItem = _treeView.GetCurrentItem(e.GetPosition);

                        if (currentItem is StoreTreeViewItem)
                        {
                            var t = (StoreTreeViewItem)currentItem;

                            if (_treeView.SelectedItem is StoreTreeViewItem)
                            {
                                var selectTreeViewItem = (StoreTreeViewItem)_treeView.SelectedItem;
                                if (selectTreeViewItem == null) return;

                                if (t.Value.Boxes.Any(n => boxes.Any(m => object.ReferenceEquals(n, m)))) return;

                                boxes = boxes.Where(n => !object.ReferenceEquals(n, t.Value)).ToList();

                                if (boxes.Count == 0) return;

                                foreach (var box in boxes)
                                {
                                    if (_treeView.GetLineage(t).OfType<BoxTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, box))) return;
                                }

                                if (!this.DigitalSignatureRelease(_treeView.GetLineage(t).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
                                if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                                t.IsSelected = true;

                                var tboxes = selectTreeViewItem.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                                selectTreeViewItem.Value.Boxes.Clear();
                                selectTreeViewItem.Value.Boxes.AddRange(tboxes);

                                t.Value.Boxes.AddRange(boxes);

                                selectTreeViewItem.Update();
                                t.Update();
                            }
                            else if (_treeView.SelectedItem is BoxTreeViewItem)
                            {
                                var selectTreeViewItem = (BoxTreeViewItem)_treeView.SelectedItem;
                                if (selectTreeViewItem == null) return;

                                if (t.Value.Boxes.Any(n => boxes.Any(m => object.ReferenceEquals(n, m)))) return;

                                boxes = boxes.Where(n => !object.ReferenceEquals(n, t.Value)).ToList();

                                if (boxes.Count == 0 && seeds.Count == 0) return;

                                foreach (var box in boxes)
                                {
                                    if (_treeView.GetLineage(t).OfType<BoxTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, box))) return;
                                }

                                if (!this.DigitalSignatureRelease(_treeView.GetLineage(t).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
                                if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                                t.IsSelected = true;

                                var tboxes = selectTreeViewItem.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                                selectTreeViewItem.Value.Boxes.Clear();
                                selectTreeViewItem.Value.Boxes.AddRange(tboxes);
                                var tseeds = selectTreeViewItem.Value.Seeds.Where(n => !seeds.Any(m => object.ReferenceEquals(n, m))).ToArray();

                                t.Value.Boxes.AddRange(boxes);

                                selectTreeViewItem.Update();
                                t.Update();
                            }
                        }
                        else if (currentItem is BoxTreeViewItem)
                        {
                            var t = (BoxTreeViewItem)currentItem;

                            if (_treeView.SelectedItem is StoreTreeViewItem)
                            {
                                var selectTreeViewItem = (StoreTreeViewItem)_treeView.SelectedItem;
                                if (selectTreeViewItem == null) return;

                                if (t.Value.Boxes.Any(n => boxes.Any(m => object.ReferenceEquals(n, m)))) return;

                                boxes = boxes.Where(n => !object.ReferenceEquals(n, t.Value)).ToList();

                                if (boxes.Count == 0 && seeds.Count == 0) return;

                                foreach (var box in boxes)
                                {
                                    if (_treeView.GetLineage(t).OfType<BoxTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, box))) return;
                                }

                                if (!this.DigitalSignatureRelease(_treeView.GetLineage(t).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
                                if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                                t.IsSelected = true;

                                var tboxes = selectTreeViewItem.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                                selectTreeViewItem.Value.Boxes.Clear();
                                selectTreeViewItem.Value.Boxes.AddRange(tboxes);

                                t.Value.Boxes.AddRange(boxes);
                                t.Value.CreationTime = DateTime.UtcNow;

                                selectTreeViewItem.Update();
                                t.Update();
                            }
                            else if (_treeView.SelectedItem is BoxTreeViewItem)
                            {
                                var selectTreeViewItem = (BoxTreeViewItem)_treeView.SelectedItem;
                                if (selectTreeViewItem == null) return;

                                if (t.Value.Boxes.Any(n => boxes.Any(m => object.ReferenceEquals(n, m)))
                                    || t.Value.Seeds.Any(n => seeds.Any(m => object.ReferenceEquals(n, m)))) return;

                                boxes = boxes.Where(n => !object.ReferenceEquals(n, t.Value)).ToList();

                                if (boxes.Count == 0 && seeds.Count == 0) return;

                                foreach (var box in boxes)
                                {
                                    if (_treeView.GetLineage(t).OfType<BoxTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, box))) return;
                                }

                                if (!this.DigitalSignatureRelease(_treeView.GetLineage(t).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
                                if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                                t.IsSelected = true;

                                var tboxes = selectTreeViewItem.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                                selectTreeViewItem.Value.Boxes.Clear();
                                selectTreeViewItem.Value.Boxes.AddRange(tboxes);
                                var tseeds = selectTreeViewItem.Value.Seeds.Where(n => !seeds.Any(m => object.ReferenceEquals(n, m))).ToArray();
                                selectTreeViewItem.Value.Seeds.Clear();
                                selectTreeViewItem.Value.Seeds.AddRange(tseeds);
                                selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                                t.Value.Boxes.AddRange(boxes);
                                t.Value.Seeds.AddRange(seeds);
                                t.Value.CreationTime = DateTime.UtcNow;

                                selectTreeViewItem.Update();
                                t.Update();
                            }
                        }
                    }
                }
            }

            this.Update();
        }

        #endregion

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

            Settings.Instance.StoreControl_ExpandedPath.Add(path);
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

            Settings.Instance.StoreControl_ExpandedPath.Remove(path);
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

        private void _treeViewNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SignatureWindow window = new SignatureWindow();
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                if (_treeViewItemCollection.Any(n => n.Value.UploadSignature == window.DigitalSignature.ToString())) return;

                var storeInfo = new StoreInfo();
                storeInfo.UploadSignature = window.DigitalSignature.ToString();

                _treeViewItemCollection.Add(new StoreTreeViewItem(storeInfo));
            }

            this.Update();
        }

        private void _storeTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = sender as StoreTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            {
                var storeTreeViewItemPasteMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(n => n.Name == "_storeTreeViewItemPasteMenuItem");
                if (storeTreeViewItemPasteMenuItem == null) return;

                var boxes = Clipboard.GetBoxes();

                storeTreeViewItemPasteMenuItem.IsEnabled = boxes.Count() > 0 ? true : false;
            }

            {
                var storeTreeViewItemUploadMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(n => n.Name == "_storeTreeViewItemUploadMenuItem");
                if (storeTreeViewItemUploadMenuItem == null) return;

                storeTreeViewItemUploadMenuItem.IsEnabled = selectTreeViewItem.Value.Boxes.Count > 0 ? true : false;
            }
        }

        private void _storeTreeViewItemNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

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

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _storeTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            SignatureWindow window = new SignatureWindow(selectTreeViewItem.Value.UploadSignature);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                if (_treeViewItemCollection.Any(n => n.Value.UploadSignature == window.DigitalSignature.ToString())) return;

                selectTreeViewItem.Value.UploadSignature = window.DigitalSignature.ToString();
                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _storeTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Section", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _treeViewItemCollection.Remove(selectTreeViewItem);

            this.Update();
        }

        private void _storeTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetText(selectTreeViewItem.Value.UploadSignature);
        }

        private void _storeTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            selectTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes());

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _storeTreeViewItemImportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

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

                                if (!StoreControl.CheckBoxDigitalSignature(ref box))
                                {
                                    if (MessageBox.Show(
                                            _mainWindow,
                                            string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.BoxControl_DigitalSignatureError_Message),
                                            "Store",
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

                    selectTreeViewItem.Update();
                    this.Update();
                }
            }
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

        private void _storeTreeViewItemUploadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as StoreTreeViewItem;
            if (selectTreeViewItem == null) return;

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == selectTreeViewItem.Value.UploadSignature);
            if (digitalSignature == null) return;

            if (selectTreeViewItem.Value.Boxes.Count == 0) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Upload_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            Store store = new Store();
            store.Boxes.AddRange(selectTreeViewItem.Value.Boxes);

            _amoebaManager.Upload(store, digitalSignature);
        }

        private void _boxTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = sender as BoxTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            {
                var boxTreeViewItemPasteMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(n => n.Name == "_boxTreeViewItemPasteMenuItem");
                if (boxTreeViewItemPasteMenuItem == null) return;

                var boxes = Clipboard.GetBoxes();
                var Seeds = Clipboard.GetSeeds();

                boxTreeViewItemPasteMenuItem.IsEnabled = (boxes.Count() + Seeds.Count()) > 0 ? true : false;
            }
        }

        private void _boxTreeViewItemNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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

        private void _boxTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).Where(n => n != selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var box = selectTreeViewItem.Value;
            BoxEditWindow window = new BoxEditWindow(box);
            window.Owner = _mainWindow;
            window.ShowDialog();

            selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _boxTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            var list = _treeView.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();
            if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).Where(n => n != selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            if (list[list.Count - 2] is StoreTreeViewItem)
            {
                var target = (StoreTreeViewItem)list[list.Count - 2];

                target.Value.Boxes.Remove(selectTreeViewItem.Value);

                target.Update();
            }
            else if (list[list.Count - 2] is BoxTreeViewItem)
            {
                var target = (BoxTreeViewItem)list[list.Count - 2];

                target.Value.Boxes.Remove(selectTreeViewItem.Value);
                target.Value.CreationTime = DateTime.UtcNow;

                target.Update();
            }

            this.Update();
        }

        private void _boxTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            var list = _treeView.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();
            if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).Where(n => n != selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            Clipboard.SetBoxes(new Box[] { selectTreeViewItem.Value });

            if (list[list.Count - 2] is StoreTreeViewItem)
            {
                var target = (StoreTreeViewItem)list[list.Count - 2];

                target.Value.Boxes.Remove(selectTreeViewItem.Value);

                target.Update();
            }
            else if (list[list.Count - 2] is BoxTreeViewItem)
            {
                var target = (BoxTreeViewItem)list[list.Count - 2];

                target.Value.Boxes.Remove(selectTreeViewItem.Value);
                target.Value.CreationTime = DateTime.UtcNow;

                target.Update();
            }

            this.Update();
        }

        private void _boxTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetBoxes(new Box[] { selectTreeViewItem.Value });
        }

        private void _boxTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            selectTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes());
            selectTreeViewItem.Value.Seeds.AddRange(Clipboard.GetSeeds());
            selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _boxTreeViewItemImportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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

                                if (!StoreControl.CheckBoxDigitalSignature(ref box))
                                {
                                    if (MessageBox.Show(
                                            _mainWindow,
                                            string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.BoxControl_DigitalSignatureError_Message),
                                            "Store",
                                            MessageBoxButton.OKCancel,
                                            MessageBoxImage.Asterisk) == MessageBoxResult.OK)
                                    {
                                        selectTreeViewItem.Value.Boxes.Add(box);
                                    }
                                }
                                else
                                {
                                    selectTreeViewItem.Value.Boxes.Add(box);
                                    selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }

                    selectTreeViewItem.Update();
                    this.Update();
                }
            }
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
                    string baseDirectory = System.IO.Path.Combine("Store", StoreControl.GetNormalizedPath(Signature.GetSignatureNickname(selectTreeViewItem.Value.UploadSignature)));

                    foreach (var item in _treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().ToList().Select(n => n.Value))
                    {
                        baseDirectory = System.IO.Path.Combine(baseDirectory, StoreControl.GetNormalizedPath(item.Name));
                    }

                    var path = System.IO.Path.Combine(baseDirectory, StoreControl.GetNormalizedPath(seed.Name));

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
                    string baseDirectory = System.IO.Path.Combine("Store", StoreControl.GetNormalizedPath(Signature.GetSignatureNickname(storeTreeViewItem.Value.UploadSignature)));

                    foreach (var item in _treeView.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().ToList().Select(n => n.Value))
                    {
                        baseDirectory = System.IO.Path.Combine(baseDirectory, StoreControl.GetNormalizedPath(item.Name));
                    }

                    var path = System.IO.Path.Combine(baseDirectory, StoreControl.GetNormalizedPath(seed.Name));

                    _amoebaManager.Download(seed.DeepClone(), baseDirectory, 3);
                }
            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _startPoint = new Point(-1, -1);

            if (_refresh || _treeView.SelectedItem == null)
            {
                _listViewNewMenuItem.IsEnabled = false;
                _listViewEditMenuItem.IsEnabled = false;
                _listViewDeleteMenuItem.IsEnabled = false;
                _listViewCutMenuItem.IsEnabled = false;
                _listViewCopyMenuItem.IsEnabled = false;
                _listViewCopyInfoMenuItem.IsEnabled = false;
                _listViewDownloadMenuItem.IsEnabled = false;
                _listViewPasteMenuItem.IsEnabled = false;

                return;
            }

            var selectItems = _listView.SelectedItems;

            _listViewNewMenuItem.IsEnabled = true;
            _listViewEditMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewCopyInfoMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewDownloadMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;
                if (selectTreeViewItem == null) return;

                if (_listView.SelectedItem is BoxListViewItem)
                {
                    var tempListViewItem = _listView.SelectedItem as BoxListViewItem;
                    var tempBoxTreeViewItem = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tempListViewItem.Value));

                    if (tempBoxTreeViewItem != null)
                    {
                        selectTreeViewItem = tempBoxTreeViewItem;
                    }
                }

                if (selectTreeViewItem is StoreTreeViewItem)
                {
                    var boxes = Clipboard.GetBoxes();

                    _listViewPasteMenuItem.IsEnabled = boxes.Count() > 0 ? true : false;
                }
                else if (selectTreeViewItem is BoxTreeViewItem)
                {
                    var seeds = Clipboard.GetSeeds();
                    var boxes = Clipboard.GetBoxes();

                    _listViewPasteMenuItem.IsEnabled = (seeds.Count() + boxes.Count()) > 0 ? true : false;
                }
            }
        }

        private void _listViewNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;
            if (selectTreeViewItem == null) return;

            if (_listView.SelectedItem is BoxListViewItem)
            {
                var tempListViewItem = _listView.SelectedItem as BoxListViewItem;
                var tempBoxTreeViewItem = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tempListViewItem.Value));

                if (tempBoxTreeViewItem != null)
                {
                    selectTreeViewItem = tempBoxTreeViewItem;
                }
            }

            if (selectTreeViewItem is StoreTreeViewItem)
            {
                var selectStoreTreeViewItem = (StoreTreeViewItem)selectTreeViewItem;

                Box box;

                if (!selectStoreTreeViewItem.Value.Boxes.Any(n => n.Name == "New box"))
                {
                    box = new Box() { Name = "New box", CreationTime = DateTime.UtcNow };
                }
                else
                {
                    int i = 1;
                    while (selectStoreTreeViewItem.Value.Boxes.Any(n => n.Name == "New box_" + i)) i++;

                    box = new Box() { Name = "New box_" + i, CreationTime = DateTime.UtcNow };
                }

                BoxEditWindow window = new BoxEditWindow(box);
                window.Owner = _mainWindow;

                if (window.ShowDialog() == true)
                {
                    selectStoreTreeViewItem.Value.Boxes.Add(box);

                    selectStoreTreeViewItem.Update();
                }
            }
            else if (selectTreeViewItem is BoxTreeViewItem)
            {
                var selectBoxTreeViewItem = (BoxTreeViewItem)selectTreeViewItem;

                if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                Box box;

                if (!selectBoxTreeViewItem.Value.Boxes.Any(n => n.Name == "New box"))
                {
                    box = new Box() { Name = "New box", CreationTime = DateTime.UtcNow };
                }
                else
                {
                    int i = 1;
                    while (selectBoxTreeViewItem.Value.Boxes.Any(n => n.Name == "New box_" + i)) i++;

                    box = new Box() { Name = "New box_" + i, CreationTime = DateTime.UtcNow };
                }

                BoxEditWindow window = new BoxEditWindow(box);
                window.Owner = _mainWindow;

                if (window.ShowDialog() == true)
                {
                    selectBoxTreeViewItem.Value.Boxes.Add(box);

                    selectBoxTreeViewItem.Update();
                }
            }

            this.Update();
        }

        private void _listViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;
            if (selectTreeViewItem == null) return;

            if (selectTreeViewItem is StoreTreeViewItem)
            {
                var selectStoreTreeViewItem = (StoreTreeViewItem)_treeView.SelectedItem;

                if (_listView.SelectedItem is BoxListViewItem)
                {
                    var selectBoxListViewItems = _listView.SelectedItems.OfType<BoxListViewItem>();
                    if (selectBoxListViewItems == null) return;

                    if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectStoreTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                    var editBoxs = (IList<Box>)selectBoxListViewItems.Select(n => n.Value.DeepClone()).ToList();
                    if (editBoxs == null) return;

                    BoxEditWindow window = new BoxEditWindow(editBoxs.ToArray());
                    window.Owner = _mainWindow;

                    if (window.ShowDialog() == true)
                    {
                        foreach (var item in selectBoxListViewItems)
                        {
                            selectStoreTreeViewItem.Value.Boxes.Remove(item.Value);
                        }

                        foreach (var seed in editBoxs)
                        {
                            selectStoreTreeViewItem.Value.Boxes.Add(seed);
                        }

                        selectStoreTreeViewItem.Update();
                    }
                }
            }
            else if (selectTreeViewItem is BoxTreeViewItem)
            {
                var selectBoxTreeViewItem = (BoxTreeViewItem)_treeView.SelectedItem;

                if (_listView.SelectedItem is BoxListViewItem)
                {
                    var selectBoxListViewItems = _listView.SelectedItems.OfType<BoxListViewItem>();
                    if (selectBoxListViewItems == null) return;

                    if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                    var editBoxs = (IList<Box>)selectBoxListViewItems.Select(n => n.Value.DeepClone()).ToList();
                    if (editBoxs == null) return;

                    BoxEditWindow window = new BoxEditWindow(editBoxs.ToArray());
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

                    if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                    var editSeeds = (IList<Seed>)selectSeedListViewItems.Select(n => n.Value.DeepClone()).ToList();
                    if (editSeeds == null) return;

                    SeedEditWindow window = new SeedEditWindow(editSeeds.ToArray());
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

            if (selectTreeViewItem is StoreTreeViewItem)
            {
                var selectStoreTreeViewItem = (StoreTreeViewItem)_treeView.SelectedItem;

                if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

                var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);

                foreach (var item in boxes)
                {
                    selectStoreTreeViewItem.Value.Boxes.Remove(item);
                }

                selectStoreTreeViewItem.Update();
            }
            else if (selectTreeViewItem is BoxTreeViewItem)
            {
                var selectBoxTreeViewItem = (BoxTreeViewItem)_treeView.SelectedItem;

                if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
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

            if (selectTreeViewItem is StoreTreeViewItem)
            {
                var selectStoreTreeViewItem = (StoreTreeViewItem)_treeView.SelectedItem;

                var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);

                Clipboard.SetBoxes(boxes);

                foreach (var item in boxes)
                {
                    selectStoreTreeViewItem.Value.Boxes.Remove(item);
                }

                selectStoreTreeViewItem.Update();
            }
            else if (selectTreeViewItem is BoxTreeViewItem)
            {
                var selectBoxTreeViewItem = (BoxTreeViewItem)_treeView.SelectedItem;

                if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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
            var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;
            if (selectTreeViewItem == null) return;

            if (_listView.SelectedItem is BoxListViewItem)
            {
                var tempListViewItem = _listView.SelectedItem as BoxListViewItem;
                var tempBoxTreeViewItem = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tempListViewItem.Value));

                if (tempBoxTreeViewItem != null)
                {
                    selectTreeViewItem = tempBoxTreeViewItem;
                }
            }

            if (selectTreeViewItem is StoreTreeViewItem)
            {
                var selectStoreTreeViewItem = (StoreTreeViewItem)selectTreeViewItem;

                selectStoreTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes());

                selectStoreTreeViewItem.Update();
            }
            else if (selectTreeViewItem is BoxTreeViewItem)
            {
                var selectBoxTreeViewItem = (BoxTreeViewItem)selectTreeViewItem;

                if (!this.DigitalSignatureRelease(_treeView.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                selectBoxTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes());
                selectBoxTreeViewItem.Value.Seeds.AddRange(Clipboard.GetSeeds());
                selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                selectBoxTreeViewItem.Update();
            }

            this.Update();
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
                string baseDirectory = System.IO.Path.Combine("Store", StoreControl.GetNormalizedPath(Signature.GetSignatureNickname(storeTreeViewItem.Value.UploadSignature)));

                foreach (var item in StoreControl.GetBoxLineage(storeTreeViewItem.Value, seed))
                {
                    baseDirectory = System.IO.Path.Combine(baseDirectory, StoreControl.GetNormalizedPath(item.Name));
                }

                var path = System.IO.Path.Combine(baseDirectory, StoreControl.GetNormalizedPath(seed.Name));

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

                if (headerClicked != Settings.Instance.StoreControl_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.StoreControl_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                Settings.Instance.StoreControl_LastHeaderClicked = headerClicked;
                Settings.Instance.StoreControl_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.StoreControl_LastHeaderClicked != null)
                {
                    Sort(Settings.Instance.StoreControl_LastHeaderClicked, Settings.Instance.StoreControl_ListSortDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listView.Items.SortDescriptions.Clear();
            _listView.Items.SortDescriptions.Add(new SortDescription("Type", direction));

            if (sortBy == LanguagesManager.Instance.StoreControl_Name)
            {

            }
            else if (sortBy == LanguagesManager.Instance.StoreControl_Signature)
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
            else if (sortBy == LanguagesManager.Instance.StoreControl_CreationTime)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("CreationTime", direction));
            }
            else if (sortBy == LanguagesManager.Instance.StoreControl_Comment)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Comment", direction));
            }
            else if (sortBy == LanguagesManager.Instance.StoreControl_State)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("State", direction));
            }
            else if (sortBy == LanguagesManager.Instance.StoreControl_Hash)
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

        private void Execute_New(object sender, ExecutedRoutedEventArgs e)
        {
            _treeViewNewMenuItem_Click(null, null);
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                if (_treeView.SelectedItem is StoreTreeViewItem)
                {
                    _storeTreeViewItemDeleteMenuItem_Click(null, null);
                }
                else if (_treeView.SelectedItem is BoxTreeViewItem)
                {
                    _boxTreeViewItemDeleteMenuItem_Click(null, null);
                }
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

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                if (_treeView.SelectedItem is StoreTreeViewItem)
                {

                }
                else if (_treeView.SelectedItem is BoxTreeViewItem)
                {
                    _boxTreeViewItemCutMenuItem_Click(null, null);
                }
            }
            else
            {
                _listViewCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is StoreTreeViewItem)
            {
                _storeTreeViewItemPasteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is BoxTreeViewItem)
            {
                _boxTreeViewItemPasteMenuItem_Click(null, null);
            }
        }

        private void Execute_Search(object sender, ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(24);
            _searchTextBox.Focus();
        }
    }

    class StoreTreeViewItem : TreeViewItem
    {
        private StoreInfo _value;
        private ObservableCollection<BoxTreeViewItem> _listViewItemCollection = new ObservableCollection<BoxTreeViewItem>();
        private TextBlock _header = new TextBlock();

        public StoreTreeViewItem(StoreInfo storeTreeItem)
        {
            base.Header = _header;
            this.Value = storeTreeItem;

            this.ItemsSource = _listViewItemCollection;

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
            _header.Text = this.Value.UploadSignature;

            List<BoxTreeViewItem> list = new List<BoxTreeViewItem>();

            foreach (var item in _value.Boxes)
            {
                list.Add(new BoxTreeViewItem(item));
            }

            foreach (var item in _listViewItemCollection.OfType<BoxTreeViewItem>().ToArray())
            {
                if (!list.Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in list)
            {
                if (!_listViewItemCollection.OfType<BoxTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    _listViewItemCollection.Add(item);
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.OfType<BoxTreeViewItem>().ToList();

            list.Sort((x, y) =>
            {
                int c = x.Value.Name.CompareTo(y.Value.Name);
                if (c != 0) return c;
                c = (x.Value.Certificate == null).CompareTo(y.Value.Certificate == null);
                if (c != 0) return c;
                if (x.Value.Certificate != null && x.Value.Certificate != null)
                {
                    c = Collection.Compare(x.Value.Certificate.PublicKey, y.Value.Certificate.PublicKey);
                    if (c != 0) return c;
                }
                c = y.Value.Seeds.Count.CompareTo(x.Value.Seeds.Count);
                if (c != 0) return c;
                c = y.Value.Boxes.Count.CompareTo(x.Value.Boxes.Count);
                if (c != 0) return c;

                return x.GetHashCode().CompareTo(y.GetHashCode());
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _listViewItemCollection.IndexOf(list[i]);

                if (i != o) _listViewItemCollection.Move(o, i);
            }

            foreach (var item in this.Items.OfType<BoxTreeViewItem>())
            {
                item.Sort();
            }
        }

        public StoreInfo Value
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
    }

    [DataContract(Name = "StoreInfo", Namespace = "http://Amoeba/Windows")]
    class StoreInfo : IDeepCloneable<StoreInfo>, IThisLock
    {
        private string _uploadSignature = null;
        private BoxCollection _boxes = null;
        private bool _isUpdated;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "UploadSignature")]
        public string UploadSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _uploadSignature;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _uploadSignature = value;
                }
            }
        }

        [DataMember(Name = "Boxes")]
        public BoxCollection Boxes
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_boxes == null)
                        _boxes = new BoxCollection();

                    return _boxes;
                }
            }
        }

        [DataMember(Name = "IsUpdated")]
        public bool IsUpdated
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isUpdated;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isUpdated = value;
                }
            }
        }

        #region IDeepClone<StoreTreeItem>

        public StoreInfo DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(StoreInfo));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (StoreInfo)ds.ReadObject(textDictionaryReader);
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
