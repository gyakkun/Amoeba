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
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private Thread _searchThread = null;
        private volatile bool _refresh = false;

        public LibraryControl(MainWindow mainWindow, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            InitializeComponent();

            _treeViewItem.Value = Settings.Instance.LibraryControl_Box;

            try
            {
                _treeViewItem.IsSelected = true;
            }
            catch (Exception)
            {

            }

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;

                if (App.SelectTab == "Library" && !_refresh)
                    _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectBoxTreeViewItem.Value.Name);
            };

            _searchThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    for (; ; )
                    {
                        Thread.Sleep(100);
                        if (!_refresh) continue;

                        BoxTreeViewItem selectBoxTreeViewItem = null;

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
                        }), null);

                        if (selectBoxTreeViewItem == null) continue;

                        HashSet<object> newList = new HashSet<object>();
                        HashSet<object> oldList = new HashSet<object>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            oldList.UnionWith(_listView.Items.OfType<object>());
                        }), null);

                        foreach (var item in selectBoxTreeViewItem.Value.Boxes)
                        {
                            var boxesListViewItem = new BoxListViewItem();
                            boxesListViewItem.Index = newList.Count;
                            boxesListViewItem.Name = item.Name;
                            boxesListViewItem.Signature = MessageConverter.ToSignatureString(item.Certificate);
                            boxesListViewItem.CreationTime = item.CreationTime;
                            boxesListViewItem.Length = LibraryControl.GetBoxLength(item);
                            boxesListViewItem.Comment = item.Comment;
                            boxesListViewItem.Value = item;

                            newList.Add(boxesListViewItem);
                        }

                        Dictionary<Seed, SearchState> seedsDictionary = new Dictionary<Seed, SearchState>(new SeedHashEqualityComparer());

                        {
                            if (!Settings.Instance.Global_SearchFilterSettings_State.HasFlag(SearchState.Cache))
                            {
                                foreach (var seed in _amoebaManager.Seeds)
                                {
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

                        foreach (var item in selectBoxTreeViewItem.Value.Seeds)
                        {
                            var seedListViewItem = new SeedListViewItem();
                            seedListViewItem.Index = newList.Count;
                            seedListViewItem.Name = item.Name;
                            seedListViewItem.Signature = MessageConverter.ToSignatureString(item.Certificate);
                            seedListViewItem.Keywords = string.Join(", ", item.Keywords.Where(n => !string.IsNullOrWhiteSpace(n)));
                            seedListViewItem.CreationTime = item.CreationTime;
                            seedListViewItem.Length = item.Length;
                            seedListViewItem.Comment = item.Comment;

                            SearchState state;

                            if (seedsDictionary.TryGetValue(item, out state))
                            {
                                seedListViewItem.State = state;
                            }

                            seedListViewItem.Value = item;

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

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
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

                            if (App.SelectTab == "Library")
                                _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectBoxTreeViewItem.Value.Name);
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

            ThreadPool.QueueUserWorkItem(new WaitCallback(this.Watch), this);
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

        private void Watch(object state)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Thread.CurrentThread.IsBackground = true;

            try
            {
                for (; ; )
                {
                    Thread.Sleep(1000 * 3);

                    if (!Directory.Exists(App.DirectoryPaths["Input"])) continue;

                    foreach (var filePath in Directory.GetFiles(App.DirectoryPaths["Input"]))
                    {
                        if (!filePath.EndsWith(".box")) continue;

                        try
                        {
                            using (FileStream stream = new FileStream(filePath, FileMode.Open))
                            {
                                var box = AmoebaConverter.FromBoxStream(stream);

                                if (box != null)
                                {
                                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                                    {
                                        try
                                        {
                                            if (!LibraryControl.BoxDigitalSignatureCheck(ref box))
                                            {
                                                if (MessageBox.Show(
                                                        _mainWindow,
                                                        LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message,
                                                        "Digital Signature",
                                                        MessageBoxButton.OKCancel,
                                                        MessageBoxImage.Asterisk) == MessageBoxResult.OK)
                                                {
                                                    _treeViewItem.Value.Boxes.Add(box);
                                                    _treeViewItem.Update();
                                                }
                                            }
                                            else
                                            {
                                                _treeViewItem.Value.Boxes.Add(box);
                                                _treeViewItem.Update();
                                            }

                                            this.Update();
                                        }
                                        catch (Exception)
                                        {

                                        }
                                    }), null);
                                }
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
            }
            catch (Exception)
            {

            }
        }

        private static bool BoxDigitalSignatureCheck(ref Box box)
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

            foreach (var item in seedList)
            {
                if (!item.VerifyCertificate())
                {
                    flag = false;

                    item.CreateCertificate(null);
                }
            }

            foreach (var item in boxList)
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
                    string.Format("{0}\r\n{1}", builder.ToString(), LanguagesManager.Instance.LibraryControl_DigitalSignatureAnnulled_Message),
                    "Digital Signature",
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
                length += LibraryControl.GetBoxLength(item);
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
            Settings.Instance.LibraryControl_Box = _treeViewItem.Value;

            _treeView_SelectedItemChanged(this, null);
            _treeViewItem.Sort();
        }

        private void _textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
                if (selectBoxTreeViewItem == null) return;

                if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                Box box;

                box = new Box() { Name = _textBox.Text, CreationTime = DateTime.UtcNow };

                selectBoxTreeViewItem.Value.Boxes.Add(box);
                selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                selectBoxTreeViewItem.Update();

                _textBox.Text = "";
                this.Update();
            }
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
                var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
                if (selectBoxTreeViewItem == null) return;

                var posithonIndex = _listView.GetCurrentIndex(e.GetPosition);

                if (posithonIndex != -1)
                {
                    var tl = _listView.Items[posithonIndex] as BoxListViewItem;
                    var t = selectBoxTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                    if (t != null)
                    {
                        selectBoxTreeViewItem = t;
                    }
                }

                var tempItem = _treeView.GetCurrentItem(e.GetPosition) as BoxTreeViewItem;
                if (tempItem != null) selectBoxTreeViewItem = tempItem;

                foreach (string filePath in ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(item => File.Exists(item)))
                {
                    if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                    using (FileStream stream = new FileStream(filePath, FileMode.Open))
                    {
                        try
                        {
                            var box = AmoebaConverter.FromBoxStream(stream);

                            if (!LibraryControl.BoxDigitalSignatureCheck(ref box))
                            {
                                if (MessageBox.Show(
                                    _mainWindow,
                                    LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message,
                                    "Digital Signature",
                                    MessageBoxButton.OKCancel,
                                    MessageBoxImage.Asterisk) == MessageBoxResult.OK)
                                {
                                    selectBoxTreeViewItem.Value.Boxes.Add(box);
                                    selectBoxTreeViewItem.Update();
                                }
                            }
                            else
                            {
                                selectBoxTreeViewItem.Value.Boxes.Add(box);
                                selectBoxTreeViewItem.Update();
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
                if (e.Data.GetDataPresent("item"))
                {
                    var s = e.Data.GetData("item") as BoxTreeViewItem;
                    var t = _treeView.GetCurrentItem(e.GetPosition) as BoxTreeViewItem;
                    if (t == null || s == t
                        || t.Value.Boxes.Any(n => object.ReferenceEquals(n, s.Value))) return;

                    var list = _treeViewItem.GetLineage(s).OfType<BoxTreeViewItem>().ToList();

                    if (_treeViewItem.GetLineage(t).OfType<BoxTreeViewItem>().Any(n => object.ReferenceEquals(n, s))) return;
                    if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(t).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
                    if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(s).OfType<BoxTreeViewItem>().Where(n => n != s).Select(n => n.Value))) return;

                    t.IsSelected = true;

                    var tboxes = list[list.Count - 2].Value.Boxes.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                    list[list.Count - 2].Value.Boxes.Clear();
                    list[list.Count - 2].Value.Boxes.AddRange(tboxes);
                    list[list.Count - 2].Value.CreationTime = DateTime.UtcNow;

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
                        var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
                        if (selectBoxTreeViewItem == null) return;

                        int index = _listView.GetCurrentIndex(e.GetPosition);
                        if (index == -1) return;

                        var tl = _listView.Items[index] as BoxListViewItem;
                        if (tl == null) return;

                        var t = selectBoxTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                        boxes = boxes.Where(n => !object.ReferenceEquals(n, t.Value)).ToList();

                        if (boxes.Count == 0 && seeds.Count == 0) return;

                        if (!this.DigitalSignatureRelease(new Box[] { t.Value })) return;
                        if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                        var tboxes = selectBoxTreeViewItem.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                        selectBoxTreeViewItem.Value.Boxes.Clear();
                        selectBoxTreeViewItem.Value.Boxes.AddRange(tboxes);
                        var tseeds = selectBoxTreeViewItem.Value.Seeds.Where(n => !seeds.Any(m => object.ReferenceEquals(n, m))).ToArray();
                        selectBoxTreeViewItem.Value.Seeds.Clear();
                        selectBoxTreeViewItem.Value.Seeds.AddRange(tseeds);
                        selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                        t.Value.Boxes.AddRange(boxes);
                        t.Value.Seeds.AddRange(seeds);
                        t.Value.CreationTime = DateTime.UtcNow;

                        selectBoxTreeViewItem.Update();
                        t.Update();
                    }
                    else
                    {
                        var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
                        if (selectBoxTreeViewItem == null) return;

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
                        if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                        t.IsSelected = true;

                        var tboxes = selectBoxTreeViewItem.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                        selectBoxTreeViewItem.Value.Boxes.Clear();
                        selectBoxTreeViewItem.Value.Boxes.AddRange(tboxes);
                        var tseeds = selectBoxTreeViewItem.Value.Seeds.Where(n => !seeds.Any(m => object.ReferenceEquals(n, m))).ToArray();
                        selectBoxTreeViewItem.Value.Seeds.Clear();
                        selectBoxTreeViewItem.Value.Seeds.AddRange(tseeds);
                        selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                        t.Value.Boxes.AddRange(boxes);
                        t.Value.Seeds.AddRange(seeds);
                        t.Value.CreationTime = DateTime.UtcNow;

                        selectBoxTreeViewItem.Update();
                        t.Update();
                    }
                }
            }

            this.Update();
        }

        #endregion

        #region _treeView

        private void _treeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _treeView.GetCurrentItem(e.GetPosition) as BoxTreeViewItem;
            if (item == null) return;

            item.IsSelected = true;
        }

        private void _treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _treeView.GetCurrentItem(e.GetPosition) as BoxTreeViewItem;
            if (item == null)
            {
                _startPoint = new Point(-1, -1);

                return;
            }

            _startPoint = e.GetPosition(null);

            _treeView_SelectedItemChanged(null, null);
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
            _refresh = true;
        }

        private void _treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _startPoint = new Point(-1, -1);

            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            _treeViewCutMenuItem.IsEnabled = (selectBoxTreeViewItem != _treeViewItem);
            _treeViewDeleteMenuItem.IsEnabled = (selectBoxTreeViewItem != _treeViewItem);

            {
                var boxes = Clipboard.GetBoxes();
                var Seeds = Clipboard.GetSeeds();

                _treeViewPasteMenuItem.IsEnabled = (boxes.Count() + Seeds.Count()) > 0 ? true : false;
            }
        }

        private void _treeViewAddBoxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            Box box;

            if (!selectBoxTreeViewItem.Value.Boxes.Any(n => n.Name == "New Box"))
            {
                box = new Box() { Name = "New Box", CreationTime = DateTime.UtcNow };
            }
            else
            {
                int i = 1;
                while (selectBoxTreeViewItem.Value.Boxes.Any(n => n.Name == "New Box_" + i)) i++;

                box = new Box() { Name = "New Box_" + i, CreationTime = DateTime.UtcNow };
            }

            BoxEditWindow window = new BoxEditWindow(ref box);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectBoxTreeViewItem.Value.Boxes.Add(box);
                selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                selectBoxTreeViewItem.Update();
            }

            this.Update();
        }

        private void _treeViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).Where(n => n != selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var directory = selectBoxTreeViewItem.Value;
            BoxEditWindow window = new BoxEditWindow(ref directory);
            window.Owner = _mainWindow;
            window.ShowDialog();

            selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _treeViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null || selectBoxTreeViewItem == _treeViewItem) return;

            var list = _treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().ToList();
            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).Where(n => n != selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            list[list.Count - 2].Value.Boxes.Remove(selectBoxTreeViewItem.Value);
            list[list.Count - 2].Value.CreationTime = DateTime.UtcNow;

            list[list.Count - 2].Update();

            this.Update();
        }

        private void _treeViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null || selectBoxTreeViewItem == _treeViewItem) return;

            var list = _treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().ToList();
            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).Where(n => n != selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            Clipboard.SetBoxes(new Box[] { selectBoxTreeViewItem.Value });
            Clipboard.SetSeeds(new Seed[0]);

            list[list.Count - 2].Value.Boxes.Remove(selectBoxTreeViewItem.Value);
            list[list.Count - 2].Value.CreationTime = DateTime.UtcNow;

            list[list.Count - 2].Update();

            this.Update();
        }

        private void _treeViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            Clipboard.SetBoxes(new Box[] { selectBoxTreeViewItem.Value });
            Clipboard.SetSeeds(new Seed[0]);
        }

        private void _treeViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            selectBoxTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes());
            Clipboard.SetBoxes(new Box[0]);
            selectBoxTreeViewItem.Value.Seeds.AddRange(Clipboard.GetSeeds());
            selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _treeViewImportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.DefaultExt = ".box";
                dialog.Filter = "Box (*.box)|*.box";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (var filePath in dialog.FileNames)
                    {
                        using (FileStream stream = new FileStream(filePath, FileMode.Open))
                        {
                            try
                            {
                                var box = AmoebaConverter.FromBoxStream(stream);

                                if (!LibraryControl.BoxDigitalSignatureCheck(ref box))
                                {
                                    if (MessageBox.Show(
                                            _mainWindow,
                                            LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message,
                                            "Digital Signature",
                                            MessageBoxButton.OKCancel,
                                            MessageBoxImage.Asterisk) == MessageBoxResult.OK)
                                    {
                                        selectBoxTreeViewItem.Value.Boxes.Add(box);
                                        selectBoxTreeViewItem.Update();
                                    }
                                }
                                else
                                {
                                    selectBoxTreeViewItem.Value.Boxes.Add(box);
                                    selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;
                                    selectBoxTreeViewItem.Update();
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }

                this.Update();
            }
        }

        private void _treeViewExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.FileName = selectBoxTreeViewItem.Value.Name;
                dialog.DefaultExt = ".box";
                dialog.Filter = "Box (*.box)|*.box";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileName = dialog.FileName;

                    using (FileStream stream = new FileStream(fileName, FileMode.Create))
                    using (Stream directoryStream = AmoebaConverter.ToBoxStream(selectBoxTreeViewItem.Value))
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

            if (_listView.SelectedItem is BoxListViewItem)
            {
                var selectBoxListViewItem = _listView.SelectedItem as BoxListViewItem;
                if (selectBoxListViewItem == null) return;

                var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
                if (selectBoxTreeViewItem == null) return;

                var selectBox = selectBoxListViewItem.Value;
                var item = selectBoxTreeViewItem.Items.OfType<BoxTreeViewItem>().FirstOrDefault(n => object.ReferenceEquals(n.Value, selectBox));

                try
                {
                    selectBoxTreeViewItem.IsExpanded = true;
                    item.IsSelected = true;
                }
                catch (Exception)
                {

                }
            }
            else if (_listView.SelectedItem is SeedListViewItem)
            {
                var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
                if (selectBoxTreeViewItem == null) return;

                var selectSeedListViewItem = _listView.SelectedItem as SeedListViewItem;
                if (selectSeedListViewItem == null) return;

                var seed = selectSeedListViewItem.Value;
                string baseDirectory = "";

                foreach (var item in _treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().ToList().Select(n => n.Value))
                {
                    baseDirectory = System.IO.Path.Combine(baseDirectory, LibraryControl.GetNormalizedPath(item.Name));
                }

                var path = System.IO.Path.Combine(baseDirectory, LibraryControl.GetNormalizedPath(seed.Name));

                _amoebaManager.Download(seed.DeepClone(), baseDirectory, 3);
            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _startPoint = new Point(-1, -1);

            if (_refresh)
            {
                _listViewAddBoxMenuItem.IsEnabled = false;
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

        private void _listViewAddBoxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (_listView.SelectedItem is BoxListViewItem)
            {
                var tl = _listView.SelectedItem as BoxListViewItem;
                var t = selectBoxTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                if (t != null)
                {
                    selectBoxTreeViewItem = t;
                }
            }

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            Box box;

            if (!selectBoxTreeViewItem.Value.Boxes.Any(n => n.Name == "New Box"))
            {
                box = new Box() { Name = "New Box", CreationTime = DateTime.UtcNow };
            }
            else
            {
                int i = 1;
                while (selectBoxTreeViewItem.Value.Boxes.Any(n => n.Name == "New Box_" + i)) i++;

                box = new Box() { Name = "New Box_" + i, CreationTime = DateTime.UtcNow };
            }

            BoxEditWindow window = new BoxEditWindow(ref box);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectBoxTreeViewItem.Value.Boxes.Add(box);

                selectBoxTreeViewItem.Update();
            }

            this.Update();
        }

        private void _listViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (_listView.SelectedItem is BoxListViewItem)
            {
                var selectBoxListViewItem = _listView.SelectedItem as BoxListViewItem;
                if (selectBoxListViewItem == null) return;

                if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                var selectBox = selectBoxListViewItem.Value.DeepClone();
                if (selectBox == null) return;

                BoxEditWindow window = new BoxEditWindow(ref selectBox);
                window.Owner = _mainWindow;

                if (window.ShowDialog() == true)
                {
                    selectBoxTreeViewItem.Value.Boxes.Remove(selectBoxListViewItem.Value);
                    selectBoxTreeViewItem.Value.Boxes.Add(selectBox);
                    selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                    selectBoxTreeViewItem.Update();
                }
            }
            else if (_listView.SelectedItem is SeedListViewItem)
            {
                var selectSeedListViewItems = _listView.SelectedItems.OfType<SeedListViewItem>();
                if (selectSeedListViewItems == null) return;

                if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                var selectSeeds = (IList<Seed>)selectSeedListViewItems.Select(n => n.Value.DeepClone()).ToList();
                if (selectSeeds == null) return;

                SeedEditWindow window = new SeedEditWindow(ref selectSeeds, _amoebaManager);
                window.Owner = _mainWindow;

                if (window.ShowDialog() == true)
                {
                    foreach (var item in selectSeedListViewItems)
                    {
                        selectBoxTreeViewItem.Value.Seeds.Remove(item.Value);
                    }

                    foreach (var seed in selectSeeds)
                    {
                        selectBoxTreeViewItem.Value.Seeds.Add(seed);
                    }

                    selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                    selectBoxTreeViewItem.Update();
                }
            }

            this.Update();
        }

        private void _listViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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

            this.Update();
        }

        private void _listViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

            Clipboard.SetBoxes(boxes);
            Clipboard.SetSeeds(seeds);

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

            this.Update();
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

            Clipboard.SetBoxes(boxes);
            Clipboard.SetSeeds(seeds);
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
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (_listView.SelectedItem is BoxListViewItem)
            {
                var tl = _listView.SelectedItem as BoxListViewItem;
                var t = selectBoxTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                if (t != null)
                {
                    selectBoxTreeViewItem = t;
                }
            }

            if (!this.DigitalSignatureRelease(_treeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            selectBoxTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes());
            Clipboard.SetBoxes(new Box[0]);
            selectBoxTreeViewItem.Value.Seeds.AddRange(Clipboard.GetSeeds());
            selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _listViewDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _treeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

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

                foreach (var item in LibraryControl.GetBoxLineage(_treeViewItem.Value, seed))
                {
                    baseDirectory = System.IO.Path.Combine(baseDirectory, LibraryControl.GetNormalizedPath(item.Name));
                }

                var path = System.IO.Path.Combine(baseDirectory, LibraryControl.GetNormalizedPath(seed.Name));

                _amoebaManager.Download(seed.DeepClone(), baseDirectory, 3);
            }
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

                Sort(headerClicked, direction);

                Settings.Instance.LibraryControl_LastHeaderClicked = headerClicked;
                Settings.Instance.LibraryControl_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.LibraryControl_LastHeaderClicked != null)
                {
                    Sort(Settings.Instance.LibraryControl_LastHeaderClicked, Settings.Instance.LibraryControl_ListSortDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listView.Items.SortDescriptions.Clear();
            _listView.Items.SortDescriptions.Add(new SortDescription("Type", direction));

            if (sortBy == LanguagesManager.Instance.LibraryControl_Name)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Name", direction));
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_Signature)
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
            else if (sortBy == LanguagesManager.Instance.LibraryControl_CreationTime)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("CreationTime", direction));
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_Comment)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Comment", direction));
            }

            _listView.Items.SortDescriptions.Add(new SortDescription("Index", direction));
        }

        #endregion

        private class BoxListViewItem
        {
            public int Type { get { return 0; } }
            public int Index { get; set; }
            public string Name { get; set; }
            public string Signature { get; set; }
            public string Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public long Length { get; set; }
            public string Comment { get; set; }
            public SearchState State { get; set; }
            public Box Value { get; set; }

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
            public SearchState State { get; set; }
            public Seed Value { get; set; }

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
                    || this.State != other.State
                    || this.Value != other.Value)
                {
                    return false;
                }

                return true;
            }
        }
    }

    class BoxTreeViewItem : TreeViewItem
    {
        private Box _value = new Box();
        private ObservableCollection<BoxTreeViewItem> _listViewItemCollection = new ObservableCollection<BoxTreeViewItem>();

        public BoxTreeViewItem()
            : base()
        {
            this.ItemsSource = _listViewItemCollection;
           
            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        public BoxTreeViewItem(Box box)
            : this()
        {
            this.Value = box;
        }

        public void Update()
        {
            this.AllowDrop = true;

            if (this.Value.Certificate == null)
            {
                this.Header = string.Format("{0} ({1})", this.Value.Name, this.Value.Seeds.Count);
            }
            else
            {
                var w = new WrapPanel();
                w.Children.Add(new TextBlock() { Text = this.Value.Name });
                w.Children.Add(new TextBlock() { Text = string.Format(" ({0}) ", this.Value.Seeds.Count) });
                w.Children.Add(new TextBlock()
                {
                    Text = MessageConverter.ToSignatureString(this.Value.Certificate),
                    //Foreground = new SolidColorBrush(Color.FromRgb(64, 255, 0))
                    //FontWeight = FontWeight.FromOpenTypeWeight(800),
                });

                this.Header = w;
            }

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

            list.Sort(delegate(BoxTreeViewItem x, BoxTreeViewItem y)
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
                c = x.Value.Seeds.Count.CompareTo(y.Value.Seeds.Count);
                if (c != 0) return c;
                c = x.Value.Boxes.Count.CompareTo(y.Value.Boxes.Count);
                if (c != 0) return c;

                return x.Value.GetHashCode().CompareTo(y.Value.GetHashCode());
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

        public Box Value
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
}
