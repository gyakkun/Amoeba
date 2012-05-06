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

            _boxTreeViewItem.Value = Settings.Instance.LibraryControl_Box;

            try
            {
                _boxTreeViewItem.IsSelected = true;
            }
            catch (Exception)
            {

            }

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;

                if (App.SelectTab == "Library")
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
                            selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
                        }), null);

                        if (selectBoxTreeViewItem == null) continue;

                        HashSet<LibraryListViewItem> newList = new HashSet<LibraryListViewItem>();
                        HashSet<LibraryListViewItem> oldList = new HashSet<LibraryListViewItem>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            oldList.UnionWith(_listView.Items.OfType<LibraryListViewItem>());
                        }), null);

                        foreach (var item in selectBoxTreeViewItem.Value.Boxes)
                        {
                            var boxesListViewItem = new LibraryListViewItem();
                            boxesListViewItem.Type = 0;
                            boxesListViewItem.Index = newList.Count;
                            boxesListViewItem.Name = item.Name;
                            boxesListViewItem.Signature = MessageConverter.ToSignatureString(item.Certificate);
                            boxesListViewItem.CreationTime = item.CreationTime;
                            boxesListViewItem.Length = LibraryControl.GetBoxLength(item);
                            boxesListViewItem.Comment = item.Comment;
                            boxesListViewItem.Value = item;

                            newList.Add(boxesListViewItem);
                        }

                        foreach (var item in selectBoxTreeViewItem.Value.Seeds)
                        {
                            var seedListViewItem = new LibraryListViewItem();
                            seedListViewItem.Type = 1;
                            seedListViewItem.Index = newList.Count;
                            seedListViewItem.Name = item.Name;
                            seedListViewItem.Signature = MessageConverter.ToSignatureString(item.Certificate);
                            seedListViewItem.Keywords = string.Join(", ", item.Keywords.Select(n => n.Value));
                            seedListViewItem.CreationTime = item.CreationTime;
                            seedListViewItem.Length = item.Length;
                            seedListViewItem.Comment = item.Comment;
                            seedListViewItem.Value = item;

                            newList.Add(seedListViewItem);
                        }

                        var removeList = new List<LibraryListViewItem>();
                        var addList = new List<LibraryListViewItem>();

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
                            if (selectBoxTreeViewItem != _boxTreeView.SelectedItem) return;
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
                                var directory = AmoebaConverter.FromBoxStream(stream);

                                if (directory != null)
                                {
                                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                                    {
                                        try
                                        {
                                            if (!LibraryControl.BoxDigitalSignatureCheck(ref directory))
                                            {
                                                if (MessageBox.Show(
                                                        LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message,
                                                        "Digital Signature",
                                                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                                                {
                                                    _boxTreeViewItem.Value.Boxes.Add(directory);
                                                    _boxTreeViewItem.Update();
                                                }
                                            }
                                            else
                                            {
                                                _boxTreeViewItem.Value.Boxes.Add(directory);
                                                _boxTreeViewItem.Update();
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

                        File.Delete(filePath);
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
                    string.Format("{0}\r\n{1}", builder.ToString(), LanguagesManager.Instance.LibraryControl_DigitalSignatureAnnulled_Message),
                    "Digital Signature",
                    MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                var items = new List<BoxTreeViewItem>();
                items.Add(_boxTreeViewItem);

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
            Settings.Instance.LibraryControl_Box = _boxTreeViewItem.Value;

            _boxTreeView_SelectedItemChanged(this, null);
            _boxTreeViewItem.Sort();
        }

        #region Grid

        private Point _startPoint = new Point(-1, -1);

        private void _boxTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released)
            {
                if (_startPoint.X == -1 && _startPoint.Y == -1) return;

                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (_boxTreeViewItem == _boxTreeView.SelectedItem) return;

                    DataObject data = new DataObject("item", _boxTreeView.SelectedItem);
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
                var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
                if (selectBoxTreeViewItem == null) return;

                var posithonIndex = _listView.GetCurrentIndex(e.GetPosition);

                if (posithonIndex != -1)
                {
                    var tl = _listView.Items[posithonIndex] as LibraryListViewItem;
                    var t = selectBoxTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                    if (t != null)
                    {
                        selectBoxTreeViewItem = t;
                    }
                }

                var tempItem = _boxTreeView.GetCurrentItem(e.GetPosition) as BoxTreeViewItem;
                if (tempItem != null) selectBoxTreeViewItem = tempItem;

                foreach (string filePath in ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(item => File.Exists(item)))
                {
                    if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                    using (FileStream stream = new FileStream(filePath, FileMode.Open))
                    {
                        try
                        {
                            var box = AmoebaConverter.FromBoxStream(stream);

                            if (!LibraryControl.BoxDigitalSignatureCheck(ref box))
                            {
                                if (MessageBox.Show(
                                    LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message,
                                    "Digital Signature",
                                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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
                    var t = _boxTreeView.GetCurrentItem(e.GetPosition) as BoxTreeViewItem;
                    if (t == null || s == t
                        || t.Value.Boxes.Any(n => object.ReferenceEquals(n, s.Value))) return;

                    var list = _boxTreeViewItem.GetLineage(s).OfType<BoxTreeViewItem>().ToList();

                    if (_boxTreeViewItem.GetLineage(t).OfType<BoxTreeViewItem>().Any(n => object.ReferenceEquals(n, s))) return;
                    if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(t).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
                    if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(s).OfType<BoxTreeViewItem>().Where(n => n != s).Select(n => n.Value))) return;

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
                    var boxes = ((IList)e.Data.GetData("list")).OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Box>().ToList();
                    var seeds = ((IList)e.Data.GetData("list")).OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Seed>().ToList();

                    if (e.Source.GetType() == typeof(ListViewEx))
                    {
                        var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
                        if (selectBoxTreeViewItem == null) return;

                        int index = _listView.GetCurrentIndex(e.GetPosition);
                        if (index == -1) return;

                        var tl = _listView.Items[index] as LibraryListViewItem;
                        if (tl == null) return;

                        var t = selectBoxTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                        boxes = boxes.Where(n => !object.ReferenceEquals(n, t.Value)).ToList();

                        if (boxes.Count == 0 && seeds.Count == 0) return;

                        if (!this.DigitalSignatureRelease(new Box[] { t.Value })) return;
                        if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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
                        var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
                        if (selectBoxTreeViewItem == null) return;

                        var t = _boxTreeView.GetCurrentItem(e.GetPosition) as BoxTreeViewItem;
                        if (t == null
                            || t.Value.Boxes.Any(n => boxes.Any(m => object.ReferenceEquals(n, m)))
                            || t.Value.Seeds.Any(n => seeds.Any(m => object.ReferenceEquals(n, m)))) return;

                        boxes = boxes.Where(n => !object.ReferenceEquals(n, t.Value)).ToList();

                        if (boxes.Count == 0 && seeds.Count == 0) return;

                        foreach (var box in boxes)
                        {
                            if (_boxTreeViewItem.GetLineage(t).OfType<BoxTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, box))) return;
                        }

                        if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(t).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;
                        if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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

            if (((LibraryListViewItem)_listView.SelectedItem).Value is Box)
            {
                var selectBoxListViewItem = _listView.SelectedItem as LibraryListViewItem;
                if (selectBoxListViewItem == null) return;

                var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
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
            else if (((LibraryListViewItem)_listView.SelectedItem).Value is Seed)
            {
                var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
                if (selectBoxTreeViewItem == null) return;

                var selectSeedListViewItem = _listView.SelectedItem as LibraryListViewItem;
                if (selectSeedListViewItem == null) return;

                var seed = (Seed)selectSeedListViewItem.Value;
                string baseDirectory = "";

                foreach (var item in _boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().ToList().Select(n => n.Value))
                {
                    if (item.Certificate == null)
                    {
                        baseDirectory += string.Format("{0}/", LibraryControl.GetNormalizedPath(item.Name));
                    }
                    else
                    {
                        baseDirectory += string.Format("{0}@{1}/", LibraryControl.GetNormalizedPath(item.Name), MessageConverter.ToSignatureString(item.Certificate));
                    }
                }

                var path = System.IO.Path.Combine(baseDirectory, seed.Name);

                if (File.Exists(System.IO.Path.Combine(_amoebaManager.DownloadDirectory, path))) return;

                if (_amoebaManager.DownloadingInformation.Any(n =>
                    (n.Contains("Path") && ((string)n["Path"]) == baseDirectory)
                    && (n.Contains("Name") && ((string)n["Name"]) == seed.Name)))
                {
                    return;
                }

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
                _listViewSeedUploadMenuItem.IsEnabled = false;
                _listViewDownloadMenuItem.IsEnabled = false;
                _listViewPasteMenuItem.IsEnabled = false;

                return;
            }

            var selectItems = _listView.SelectedItems;
            if (selectItems == null) return;

            _listViewEditMenuItem.IsEnabled = (selectItems.Count > 0);
            _listViewDeleteMenuItem.IsEnabled = (selectItems.Count > 0);
            _listViewCutMenuItem.IsEnabled = (selectItems.Count > 0);
            _listViewCopyMenuItem.IsEnabled = (selectItems.Count > 0);
            _listViewCopyInfoMenuItem.IsEnabled = (selectItems.Count > 0);
            _listViewSeedUploadMenuItem.IsEnabled = (selectItems.OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Seed>().Count() > 0);
            _listViewDownloadMenuItem.IsEnabled = (selectItems.Count > 0);

            {
                var seeds = Clipboard.GetSeeds();
                var boxes = Clipboard.GetBoxes();

                _listViewPasteMenuItem.IsEnabled = (seeds.Count() + boxes.Count()) > 0 ? true : false;
            }
        }

        private void _listViewAddBoxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (_listView.SelectedItem is LibraryListViewItem)
            {
                var tl = _listView.SelectedItem as LibraryListViewItem;
                var t = selectBoxTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                if (t != null)
                {
                    selectBoxTreeViewItem = t;
                }
            }

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (((LibraryListViewItem)_listView.SelectedItem).Value is Box)
            {
                var selectBoxListViewItem = _listView.SelectedItem as LibraryListViewItem;
                if (selectBoxListViewItem == null) return;

                if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                var selectBox = ((Box)selectBoxListViewItem.Value).DeepClone();
                if (selectBox == null) return;

                BoxEditWindow window = new BoxEditWindow(ref selectBox);
                window.Owner = _mainWindow;

                if (window.ShowDialog() == true)
                {
                    selectBoxTreeViewItem.Value.Boxes.Remove((Box)selectBoxListViewItem.Value);
                    selectBoxTreeViewItem.Value.Boxes.Add(selectBox);
                    selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                    selectBoxTreeViewItem.Update();
                }
            }
            else if (((LibraryListViewItem)_listView.SelectedItem).Value is Seed)
            {
                var selectSeedListViewItem = _listView.SelectedItem as LibraryListViewItem;
                if (selectSeedListViewItem == null) return;

                if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                var selectSeed = ((Seed)selectSeedListViewItem.Value).DeepClone();
                if (selectSeed == null) return;

                SeedEditWindow window = new SeedEditWindow(ref selectSeed, _amoebaManager);
                window.Owner = _mainWindow;

                if (window.ShowDialog() == true)
                {
                    selectBoxTreeViewItem.Value.Seeds.Remove((Seed)selectSeedListViewItem.Value);
                    selectBoxTreeViewItem.Value.Seeds.Add(selectSeed);
                    selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

                    selectBoxTreeViewItem.Update();
                }
            }

            this.Update();
        }

        private void _listViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var boxes = _listView.SelectedItems.OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Box>();
            var seeds = _listView.SelectedItems.OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Seed>();

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
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var boxes = _listView.SelectedItems.OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Box>();
            var seeds = _listView.SelectedItems.OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Seed>();

            Clipboard.SetBoxes(boxes.Select(n => n.DeepClone()));
            Clipboard.SetSeeds(seeds.Select(n => n.DeepClone()));

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
            var boxes = _listView.SelectedItems.OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Box>();
            var seeds = _listView.SelectedItems.OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Seed>();

            Clipboard.SetBoxes(boxes.Select(n => n.DeepClone()));
            Clipboard.SetSeeds(seeds.Select(n => n.DeepClone()));
        }

        private void _listViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var boxes = _listView.SelectedItems.OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Box>();
            var seeds = _listView.SelectedItems.OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Seed>();

            var sb = new StringBuilder();

            foreach (var item in boxes)
            {
                sb.AppendLine(MessageConverter.ToInfoMessage(item));
                sb.AppendLine();
            }

            foreach (var item in seeds)
            {
                sb.AppendLine(MessageConverter.ToInfoMessage(item));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        private void _listViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (_listView.SelectedItem is LibraryListViewItem)
            {
                var tl = _listView.SelectedItem as LibraryListViewItem;
                var t = selectBoxTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                if (t != null)
                {
                    selectBoxTreeViewItem = t;
                }
            }

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            selectBoxTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes().Select(n => n.DeepClone()));
            selectBoxTreeViewItem.Value.Seeds.AddRange(Clipboard.GetSeeds().Select(n => n.DeepClone()));
            selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _listViewSeedUploadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(LanguagesManager.Instance.LibraryControl_SeedUpload_Message, "Seed Upload", MessageBoxButton.YesNo)
                == MessageBoxResult.Yes)
            {
                var keys = _listView.SelectedItems.OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Seed>();

                foreach (var key in keys)
                {
                    _amoebaManager.Upload(key);
                }
            }
        }

        private void _listViewDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            var seeds = _listView.SelectedItems.OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Seed>().ToList();

            {
                var boxes = _listView.SelectedItems.OfType<LibraryListViewItem>().Select(n => n.Value).OfType<Box>().ToList();

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

                foreach (var item in LibraryControl.GetBoxLineage(_boxTreeViewItem.Value, seed))
                {
                    if (item.Certificate == null)
                    {
                        baseDirectory += string.Format("{0}/", LibraryControl.GetNormalizedPath(item.Name));
                    }
                    else
                    {
                        baseDirectory += string.Format("{0}@{1}/", LibraryControl.GetNormalizedPath(item.Name), MessageConverter.ToSignatureString(item.Certificate));
                    }
                }

                var path = System.IO.Path.Combine(baseDirectory, seed.Name);

                if (File.Exists(System.IO.Path.Combine(_amoebaManager.DownloadDirectory, path))) continue;

                if (_amoebaManager.DownloadingInformation.Any(n =>
                    (n.Contains("Path") && ((string)n["Path"]) == baseDirectory)
                    && (n.Contains("Name") && ((string)n["Name"]) == seed.Name)))
                {
                    continue;
                }

                _amoebaManager.Download(seed.DeepClone(), baseDirectory, 3);
            }
        }

        #endregion

        #region _boxTreeView

        private void _boxTreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _boxTreeView.GetCurrentItem(e.GetPosition) as BoxTreeViewItem;
            if (item == null) return;

            item.IsSelected = true;
        }

        private void _boxTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _boxTreeView.GetCurrentItem(e.GetPosition) as BoxTreeViewItem;
            if (item == null)
            {
                _startPoint = new Point(-1, -1);

                return;
            }

            _startPoint = e.GetPosition(null);
        }

        private void _boxTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
            _refresh = true;
        }

        private void _boxTreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _startPoint = new Point(-1, -1);

            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            _boxTreeViewCutContextMenuItem.IsEnabled = (selectBoxTreeViewItem != _boxTreeViewItem);
            _boxTreeViewDeleteContextMenuItem.IsEnabled = (selectBoxTreeViewItem != _boxTreeViewItem);

            {
                var boxes = Clipboard.GetBoxes();
                var Seeds = Clipboard.GetSeeds();

                _boxTreeViewPasteContextMenuItem.IsEnabled = (boxes.Count() + Seeds.Count()) > 0 ? true : false;
            }
        }

        private void _boxTreeViewAddBoxContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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

        private void _boxTreeViewEditContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).Where(n => n != selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var directory = selectBoxTreeViewItem.Value;
            BoxEditWindow window = new BoxEditWindow(ref directory);
            window.Owner = _mainWindow;
            window.ShowDialog();

            selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _boxTreeViewDeleteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null || selectBoxTreeViewItem == _boxTreeViewItem) return;

            var list = _boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().ToList();
            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).Where(n => n != selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            list[list.Count - 2].Value.Boxes.Remove(selectBoxTreeViewItem.Value);
            list[list.Count - 2].Value.CreationTime = DateTime.UtcNow;

            list[list.Count - 2].Update();

            this.Update();
        }

        private void _boxTreeViewCutContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null || selectBoxTreeViewItem == _boxTreeViewItem) return;

            var list = _boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().ToList();
            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).Where(n => n != selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            Clipboard.SetBoxes(new List<Box>() { selectBoxTreeViewItem.Value.DeepClone() });

            list[list.Count - 2].Value.Boxes.Remove(selectBoxTreeViewItem.Value);
            list[list.Count - 2].Value.CreationTime = DateTime.UtcNow;

            list[list.Count - 2].Update();

            this.Update();
        }

        private void _boxTreeViewCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            Clipboard.SetBoxes(new List<Box>() { selectBoxTreeViewItem.Value.DeepClone() });
        }

        private void _boxTreeViewPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            selectBoxTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes().Select(n => n.DeepClone()));
            selectBoxTreeViewItem.Value.Seeds.AddRange(Clipboard.GetSeeds().Select(n => n.DeepClone()));
            selectBoxTreeViewItem.Value.CreationTime = DateTime.UtcNow;

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _boxTreeViewImportContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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
                                var directory = AmoebaConverter.FromBoxStream(stream);

                                if (!LibraryControl.BoxDigitalSignatureCheck(ref directory))
                                {
                                    if (MessageBox.Show(
                                            LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message,
                                            "Digital Signature",
                                            MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                                    {
                                        selectBoxTreeViewItem.Value.Boxes.Add(directory);
                                        selectBoxTreeViewItem.Update();
                                    }
                                }
                                else
                                {
                                    selectBoxTreeViewItem.Value.Boxes.Add(directory);
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

        private void _boxTreeViewExportContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
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

            _listView.Items.SortDescriptions.Add(new SortDescription("Index", direction));
        }

        #endregion

        private class LibraryListViewItem
        {
            public int Type { get; set; }
            public int Index { get; set; }
            public string Name { get; set; }
            public string Signature { get; set; }
            public string Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public long Length { get; set; }
            public string Comment { get; set; }
            public object Value { get; set; }

            public override int GetHashCode()
            {
                return this.Length.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (!(obj is LibraryListViewItem)) return false;
                if (obj == null) return false;
                if (object.ReferenceEquals(this, obj)) return true;
                if (this.GetHashCode() != obj.GetHashCode()) return false;

                var other = (LibraryListViewItem)obj;

                if (this.Type != other.Type
                    || this.Index != other.Index
                    || this.Name != other.Name
                    || this.Signature != other.Signature
                    || this.Keywords != other.Keywords
                    || this.CreationTime != other.CreationTime
                    || this.Length != other.Length
                    || this.Comment != other.Comment
                    || this.Value != other.Value)
                {
                    return false;
                }

                return true;
            }
        }
    }

    public class BoxTreeViewItem : TreeViewItem
    {
        private Box _value = new Box();
        private ObservableCollection<BoxTreeViewItem> _listViewItemCollection = new ObservableCollection<BoxTreeViewItem>();

        public BoxTreeViewItem()
            : base()
        {
            this.ItemsSource = _listViewItemCollection;
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
                w.Children.Add(new TextBlock()
                {
                    Text = "@" + MessageConverter.ToSignatureString(this.Value.Certificate),
                    //Foreground = new SolidColorBrush(Color.FromRgb(64, 255, 0))
                    FontWeight = FontWeight.FromOpenTypeWeight(800),
                });
                w.Children.Add(new TextBlock() { Text = string.Format(" ({0})", this.Value.Seeds.Count) });

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
