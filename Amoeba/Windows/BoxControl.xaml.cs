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
using Library.Io;
using Library.Net;
using Library.Net.Amoeba;
using Library.Security;

namespace Amoeba.Windows
{
    /// <summary>
    /// BoxControl.xaml の相互作用ロジック
    /// </summary>
    partial class BoxControl : UserControl
    {
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private volatile bool _refresh = false;

        private Thread _searchThread;
        private Thread _watchThread;

        public BoxControl(MainWindow mainWindow, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            InitializeComponent();

            _treeViewItem.Value = Settings.Instance.BoxControl_Box;
            _treeViewItem.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(_treeViewItem_PreviewMouseLeftButtonDown);

            try
            {
                _treeViewItem.IsSelected = true;
            }
            catch (Exception)
            {

            }

            {
                foreach (var path in Settings.Instance.BoxControl_ExpandedPath.ToArray())
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

                    Settings.Instance.BoxControl_ExpandedPath.Remove(path);
                }
            }

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;

                if (App.SelectTab == TabItemType.Box && !_refresh)
                    _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectTreeViewItem.Value.Name);
            };

            _searchThread = new Thread(new ThreadStart(this.Search));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "BoxControl_SearchThread";
            _searchThread.Start();

            _watchThread = new Thread(new ThreadStart(this.Watch));
            _watchThread.Priority = ThreadPriority.Highest;
            _watchThread.IsBackground = true;
            _watchThread.Name = "BoxControl_WatchThread";
            _watchThread.Start();

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

                    BoxTreeViewItem selectTreeViewItem = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
                    }));

                    if (selectTreeViewItem == null) continue;

                    HashSet<object> newList = new HashSet<object>(new ReferenceEqualityComparer());

                    string[] words = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        var searchText = _searchTextBox.Text;

                        if (!string.IsNullOrWhiteSpace(searchText))
                        {
                            words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                        }
                    }));

                    foreach (var box in selectTreeViewItem.Value.Boxes)
                    {
                        var text = (box.Name ?? "").ToLower();
                        if (words != null && !words.All(n => text.Contains(n))) continue;

                        var boxesListViewItem = new BoxListViewItem();
                        boxesListViewItem.Index = newList.Count;
                        boxesListViewItem.Name = box.Name;
                        if (box.Certificate != null) boxesListViewItem.Signature = box.Certificate.ToString();
                        boxesListViewItem.CreationTime = box.CreationTime;
                        boxesListViewItem.Length = BoxControl.GetBoxLength(box);
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

                    foreach (var seed in selectTreeViewItem.Value.Seeds)
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

                    HashSet<object> oldList = new HashSet<object>(new ReferenceEqualityComparer());

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        oldList.UnionWith(_listView.Items.OfType<object>());
                    }));

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

                        if (App.SelectTab == TabItemType.Box)
                            _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectTreeViewItem.Value.Name);
                    }));
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
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

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            try
                            {
                                var treeViewItem = this.GetExtractToBox();
                                if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(treeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                                if (!BoxControl.CheckBoxDigitalSignature(ref box))
                                {
                                    if (MessageBox.Show(
                                            _mainWindow,
                                            string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.BoxControl_DigitalSignatureError_Message),
                                            "Box",
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
                    string.Format("{0}\r\n{1}", builder.ToString(), LanguagesManager.Instance.BoxControl_DigitalSignatureAnnulled_Message),
                    "Box",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                var items = new List<BoxTreeViewItem>();
                items.Add(_treeViewItem);

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
                length += BoxControl.GetBoxLength(item);
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

        private static IEnumerable<Box> GetBoxLineage(Box parentItem, Seed childItem)
        {
            var list = new List<Box>();
            list.Add(parentItem);

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
                    if (item == parentItem) break;

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
            Settings.Instance.BoxControl_Box = _treeViewItem.Value;

            _treeView_SelectedItemChanged(this, null);
            _treeViewItem.Sort();
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
                    if (_treeViewItem == _treeView.SelectedItem) return;

                    DataObject data = new DataObject("item", _treeView.SelectedItem);
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

                        DataObject data = new DataObject("list", _listView.SelectedItems);
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
                var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
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
                    var tempBoxTreeViewItem = _treeView.GetCurrentItem(e.GetPosition) as BoxTreeViewItem;
                    if (tempBoxTreeViewItem != null) selectTreeViewItem = tempBoxTreeViewItem;
                }

                if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                foreach (string filePath in ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(item => File.Exists(item)))
                {
                    using (FileStream stream = new FileStream(filePath, FileMode.Open))
                    {
                        try
                        {
                            var box = AmoebaConverter.FromBoxStream(stream);
                            if (box == null) continue;

                            if (!BoxControl.CheckBoxDigitalSignature(ref box))
                            {
                                if (MessageBox.Show(
                                    _mainWindow,
                                    string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.BoxControl_DigitalSignatureError_Message),
                                    "Box",
                                    MessageBoxButton.OKCancel,
                                    MessageBoxImage.Asterisk) == MessageBoxResult.OK)
                                {
                                    selectTreeViewItem.Value.Boxes.Add(box);
                                    selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                selectTreeViewItem.Value.Boxes.Add(box);
                                selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                selectTreeViewItem.Update();
            }
            else
            {
                if (e.Data.GetDataPresent("item"))
                {
                    var s = e.Data.GetData("item") as BoxTreeViewItem;
                    var t = _treeView.GetCurrentItem(e.GetPosition) as BoxTreeViewItem;
                    if (t == null || s == t
                        || t.Value.Boxes.Any(n => object.ReferenceEquals(n, s.Value))) return;
                    if (_treeViewItem.GetLineage(t).OfType<BoxTreeViewItem>().Any(n => object.ReferenceEquals(n, s))) return;

                    if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(t).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
                    if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(s).OfType<BoxTreeViewItem>().Where(n => n != s).Select(n => n.Value))) return;

                    t.IsSelected = true;

                    var list = _treeViewItem.GetLineage(s).OfType<BoxTreeViewItem>().ToList();
                    var target = list[list.Count - 2];

                    var tboxes = target.Value.Boxes.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                    target.Value.Boxes.Clear();
                    target.Value.Boxes.AddRange(tboxes);
                    target.Value.CreationTime = DateTime.UtcNow;

                    t.Value.Boxes.Add(s.Value);
                    t.Value.CreationTime = DateTime.UtcNow;

                    list[list.Count - 2].Update();
                    t.Update();
                }
                else if (e.Data.GetDataPresent("list"))
                {
                    var boxes = ((IList)e.Data.GetData("list")).OfType<BoxListViewItem>().Select(n => n.Value).ToList();
                    var seeds = ((IList)e.Data.GetData("list")).OfType<SeedListViewItem>().Select(n => n.Value).ToList();

                    if (e.Source.GetType() == typeof(ListViewEx))
                    {
                        var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
                        if (selectTreeViewItem == null) return;

                        int index = _listView.GetCurrentIndex(e.GetPosition);
                        if (index == -1) return;

                        var tl = _listView.Items[index] as BoxListViewItem;
                        if (tl == null) return;

                        var t = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                        boxes = boxes.Where(n => !object.ReferenceEquals(n, t.Value)).ToList();

                        if (boxes.Count == 0 && seeds.Count == 0) return;

                        if (!this.DigitalSignatureRelease(new Box[] { t.Value })) return;
                        if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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
                    else
                    {
                        var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
                        if (selectTreeViewItem == null) return;

                        var t = _treeView.GetCurrentItem(e.GetPosition) as BoxTreeViewItem;
                        if (t == null
                            || t.Value.Boxes.Any(n => boxes.Any(m => object.ReferenceEquals(n, m)))
                            || t.Value.Seeds.Any(n => seeds.Any(m => object.ReferenceEquals(n, m)))) return;

                        boxes = boxes.Where(n => !object.ReferenceEquals(n, t.Value)).ToList();

                        if (boxes.Count == 0 && seeds.Count == 0) return;

                        foreach (var box in boxes)
                        {
                            if (_treeViewItem.GetLineage(t).OfType<BoxTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, box))) return;
                        }

                        if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(t).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
                        if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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

            this.Update();
        }

        #endregion

        #region _treeView

        private void _treeView_Expanded(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            Route path = new Route();

            foreach (var item in _treeView.GetLineage(treeViewItem))
            {
                if (item is BoxTreeViewItem) path.Add(((BoxTreeViewItem)item).Value.Name);
            }

            Settings.Instance.BoxControl_ExpandedPath.Add(path);
        }

        private void _treeView_Collapsed(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            Route path = new Route();

            foreach (var item in _treeView.GetLineage(treeViewItem))
            {
                if (item is BoxTreeViewItem) path.Add(((BoxTreeViewItem)item).Value.Name);
            }

            Settings.Instance.BoxControl_ExpandedPath.Remove(path);
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

        private void _treeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _treeView.GetCurrentItem(e.GetPosition) as BoxTreeViewItem;
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
            else
            {
                _startPoint = new Point(-1, -1);
            }
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
            _refresh = true;
        }

        private void _treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _startPoint = new Point(-1, -1);

            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            _treeViewCutMenuItem.IsEnabled = (selectTreeViewItem != _treeViewItem);
            _treeViewDeleteMenuItem.IsEnabled = (selectTreeViewItem != _treeViewItem);

            {
                var boxes = Clipboard.GetBoxes();
                var Seeds = Clipboard.GetSeeds();

                _treeViewPasteMenuItem.IsEnabled = (boxes.Count() + Seeds.Count()) > 0 ? true : false;
            }
        }

        private void _treeViewNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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

        private void _treeViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).Where(n => n != selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var box = selectTreeViewItem.Value;
            BoxEditWindow window = new BoxEditWindow(box);
            window.Owner = _mainWindow;
            window.ShowDialog();

            selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _treeViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).Where(n => n != selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Box", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().ToList();
            var target = list[list.Count - 2];

            target.Value.Boxes.Remove(selectTreeViewItem.Value);
            target.Value.CreationTime = DateTime.UtcNow;

            target.Update();

            this.Update();
        }

        private void _treeViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).Where(n => n != selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            Clipboard.SetBoxes(new Box[] { selectTreeViewItem.Value });

            var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().ToList();
            var target = list[list.Count - 2];

            target.Value.Boxes.Remove(selectTreeViewItem.Value);
            target.Value.CreationTime = DateTime.UtcNow;

            target.Update();

            this.Update();
        }

        private void _treeViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetBoxes(new Box[] { selectTreeViewItem.Value });
        }

        private void _treeViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            selectTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes());
            selectTreeViewItem.Value.Seeds.AddRange(Clipboard.GetSeeds());
            selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _treeViewImportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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

                                if (!BoxControl.CheckBoxDigitalSignature(ref box))
                                {
                                    if (MessageBox.Show(
                                            _mainWindow,
                                            string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.BoxControl_DigitalSignatureError_Message),
                                            "Box",
                                            MessageBoxButton.OKCancel,
                                            MessageBoxImage.Asterisk) == MessageBoxResult.OK)
                                    {
                                        selectTreeViewItem.Value.Boxes.Add(box);
                                        selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;
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

        private void _treeViewExportMenuItem_Click(object sender, RoutedEventArgs e)
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
                string baseDirectory = "";

                foreach (var item in _treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().ToList().Select(n => n.Value))
                {
                    baseDirectory = System.IO.Path.Combine(baseDirectory, BoxControl.GetNormalizedPath(item.Name));
                }

                var path = System.IO.Path.Combine(baseDirectory, BoxControl.GetNormalizedPath(seed.Name));

                _amoebaManager.Download(seed.DeepClone(), baseDirectory, 3);
            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _startPoint = new Point(-1, -1);

            if (_refresh)
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
                var seeds = Clipboard.GetSeeds();
                var boxes = Clipboard.GetBoxes();

                _listViewPasteMenuItem.IsEnabled = (seeds.Count() + boxes.Count()) > 0 ? true : false;
            }
        }

        private void _listViewNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (_listView.SelectedItem is BoxListViewItem)
            {
                var tl = _listView.SelectedItem as BoxListViewItem;
                var t = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                if (t != null)
                {
                    selectTreeViewItem = t;
                }
            }

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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

        private void _listViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (_listView.SelectedItem is BoxListViewItem)
            {
                var selectBoxListViewItems = _listView.SelectedItems.OfType<BoxListViewItem>();
                if (selectBoxListViewItems == null) return;

                if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                var editBoxs = (IList<Box>)selectBoxListViewItems.Select(n => n.Value.DeepClone()).ToList();
                if (editBoxs == null) return;

                BoxEditWindow window = new BoxEditWindow(editBoxs.ToArray());
                window.Owner = _mainWindow;

                if (window.ShowDialog() == true)
                {
                    foreach (var item in selectBoxListViewItems)
                    {
                        selectTreeViewItem.Value.Boxes.Remove(item.Value);
                    }

                    foreach (var seed in editBoxs)
                    {
                        selectTreeViewItem.Value.Boxes.Add(seed);
                    }

                    selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                    selectTreeViewItem.Update();
                }
            }
            else if (_listView.SelectedItem is SeedListViewItem)
            {
                var selectSeedListViewItems = _listView.SelectedItems.OfType<SeedListViewItem>();
                if (selectSeedListViewItems == null) return;

                if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                var editSeeds = (IList<Seed>)selectSeedListViewItems.Select(n => n.Value.DeepClone()).ToList();
                if (editSeeds == null) return;

                SeedEditWindow window = new SeedEditWindow(editSeeds.ToArray());
                window.Owner = _mainWindow;

                if (window.ShowDialog() == true)
                {
                    foreach (var item in selectSeedListViewItems)
                    {
                        selectTreeViewItem.Value.Seeds.Remove(item.Value);
                    }

                    foreach (var seed in editSeeds)
                    {
                        selectTreeViewItem.Value.Seeds.Add(seed);
                    }

                    selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                    selectTreeViewItem.Update();
                }
            }

            this.Update();
        }

        private void _listViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;

            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Box", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

            foreach (var item in boxes)
            {
                selectTreeViewItem.Value.Boxes.Remove(item);
            }

            foreach (var item in seeds)
            {
                selectTreeViewItem.Value.Seeds.Remove(item);
            }

            selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _listViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;

            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

            Clipboard.SetBoxAndSeeds(boxes, seeds);

            foreach (var item in boxes)
            {
                selectTreeViewItem.Value.Boxes.Remove(item);
            }

            foreach (var item in seeds)
            {
                selectTreeViewItem.Value.Seeds.Remove(item);
            }

            selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectTreeViewItem.Update();

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
            var selectTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (_listView.SelectedItem is BoxListViewItem)
            {
                var tl = _listView.SelectedItem as BoxListViewItem;
                var t = selectTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                if (t != null)
                {
                    selectTreeViewItem = t;
                }
            }

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            selectTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes());
            selectTreeViewItem.Value.Seeds.AddRange(Clipboard.GetSeeds());
            selectTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _listViewDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
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
                string baseDirectory = "";

                foreach (var item in BoxControl.GetBoxLineage(_treeViewItem.Value, seed))
                {
                    baseDirectory = System.IO.Path.Combine(baseDirectory, BoxControl.GetNormalizedPath(item.Name));
                }

                var path = System.IO.Path.Combine(baseDirectory, BoxControl.GetNormalizedPath(seed.Name));

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

                if (headerClicked != Settings.Instance.BoxControl_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.BoxControl_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                Settings.Instance.BoxControl_LastHeaderClicked = headerClicked;
                Settings.Instance.BoxControl_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.BoxControl_LastHeaderClicked != null)
                {
                    Sort(Settings.Instance.BoxControl_LastHeaderClicked, Settings.Instance.BoxControl_ListSortDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listView.Items.SortDescriptions.Clear();
            _listView.Items.SortDescriptions.Add(new SortDescription("Type", direction));

            if (sortBy == LanguagesManager.Instance.BoxControl_Name)
            {

            }
            else if (sortBy == LanguagesManager.Instance.BoxControl_Signature)
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
            else if (sortBy == LanguagesManager.Instance.BoxControl_CreationTime)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("CreationTime", direction));
            }
            else if (sortBy == LanguagesManager.Instance.BoxControl_Comment)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Comment", direction));
            }
            else if (sortBy == LanguagesManager.Instance.BoxControl_State)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("State", direction));
            }
            else if (sortBy == LanguagesManager.Instance.BoxControl_Hash)
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
                _listViewCutMenuItem_Click(null, null);
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
}
