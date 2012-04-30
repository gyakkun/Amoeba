using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Amoeba.Properties;
using Library;
using Library.Net;
using Library.Net.Amoeba;
using Library.Security;
using System.Collections;
using System.Threading;
using System.Windows.Threading;
using System.Collections.ObjectModel;

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

        private ObservableCollection<object> _listViewItemCollection;
        private Thread _searchThread = null;
        private volatile bool _refresh = false;

        public LibraryControl(MainWindow mainWindow, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            _listViewItemCollection = new ObservableCollection<object>();

            InitializeComponent();

            _listView.ItemsSource = _listViewItemCollection;
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
                        _refresh = false;

                        BoxTreeViewItem selectBoxTreeViewItem = null;

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
                        }), null);

                        if (selectBoxTreeViewItem == null) continue;

                        HashSet<object> newList = new HashSet<object>(new ReferenceEqualityComparer());
                        HashSet<object> oldList = new HashSet<object>(new ReferenceEqualityComparer());

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            oldList.UnionWith(_listViewItemCollection);
                        }), null);

                        foreach (var item in selectBoxTreeViewItem.Value.Boxes)
                        {
                            var boxesListViewItem = new BoxListViewItem();
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
                            var seedListViewItem = new SeedListViewItem();
                            seedListViewItem.Name = item.Name;
                            seedListViewItem.Signature = MessageConverter.ToSignatureString(item.Certificate);
                            seedListViewItem.Keywords = item.Keywords.Select(n => n.Value);
                            seedListViewItem.CreationTime = item.CreationTime;
                            seedListViewItem.Length = item.Length;
                            seedListViewItem.Comment = item.Comment;
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

                            if (sortFlag && _listViewItemCollection.Count < 10000) this.Sort();

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

        class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                if ((x == null) != (y == null)) return false;
                if (x == null && y == null) return true;

                if (x is BoxListViewItem)
                {
                    if (y is BoxListViewItem)
                    {
                        var xi = (BoxListViewItem)x;
                        var yi = (BoxListViewItem)y;

                        return object.ReferenceEquals(xi.Value, yi.Value);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (y is SeedListViewItem)
                    {
                        var xi = (SeedListViewItem)x;
                        var yi = (SeedListViewItem)y;

                        return object.ReferenceEquals(xi.Value, yi.Value);
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            public int GetHashCode(object obj)
            {
                var bl = obj as BoxListViewItem;
                if (bl != null) return bl.Name.GetHashCode();

                var sl = obj as SeedListViewItem;
                if (sl != null) return sl.Name.GetHashCode();

                return 0;
            }
        }

        private void Watch(object state)
        {
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

        private void Update()
        {
            Settings.Instance.LibraryControl_Box = _boxTreeViewItem.Value;

            _boxTreeView_SelectedItemChanged(this, null);
        }

        #region Grid

        private Point _startPoint;
        private IList<object> _selectedItems;
        private void _boxTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released)
            {
                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (e.Source.GetType() == typeof(BoxTreeViewItem) || e.Source.GetType() == typeof(TextBlock))
                    {
                        if (_boxTreeViewItem == _boxTreeView.SelectedItem) return;

                        DataObject data = new DataObject("item", _boxTreeView.SelectedItem);
                        DragDrop.DoDragDrop(_grid, data, DragDropEffects.Move);
                    }
                }
            }
        }

        bool _isMouseDown = false;

        private void _listView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isMouseDown && e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released)
            {
                Point position = e.GetPosition(null);
                Point lposition = e.GetPosition(_listView);

                if (lposition.Y < 20
                    || (_listView.ActualWidth - lposition.X) < 20 || (_listView.ActualHeight - lposition.Y) < 20)
                {
                    _isMouseDown = true;
                    return;
                }

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (_selectedItems != null && !_refresh)
                    {
                        var posithonIndex = _listView.GetCurrentIndex(e.GetPosition);
                        if (posithonIndex == -1) return;

                        var posithonItem = _listView.Items[posithonIndex];

                        if (_selectedItems.Any(n => object.ReferenceEquals(n, posithonItem)))
                        {
                            _listView.SelectedItems.Clear();

                            foreach (var item in _selectedItems)
                            {
                                _listView.SelectedItems.Add(item);
                            }
                        }
                        else
                        {
                            _listView.SelectedItems.Clear();
                            _listView.SelectedItems.Add(posithonItem);
                        }

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
                    var tl = _listView.Items[posithonIndex] as BoxListViewItem;
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
                    list[list.Count - 2].Update();

                    t.Value.Boxes.Add(s.Value);
                    t.Update();
                }
                else if (e.Data.GetDataPresent("list"))
                {
                    var boxes = ((IList)e.Data.GetData("list")).OfType<BoxListViewItem>().Select(n => n.Value).ToList();
                    var seeds = ((IList)e.Data.GetData("list")).OfType<SeedListViewItem>().Select(n => n.Value).ToList();

                    if (e.Source.GetType() == typeof(ListView))
                    {
                        var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
                        if (selectBoxTreeViewItem == null) return;

                        int index = _listView.GetCurrentIndex(e.GetPosition);
                        if (index == -1) return;

                        var tl = _listView.Items[index] as BoxListViewItem;
                        if (tl == null) return;

                        var t = selectBoxTreeViewItem.Items.OfType<BoxTreeViewItem>().First(n => object.ReferenceEquals(n.Value, tl.Value));

                        boxes = boxes.Where(n => !object.ReferenceEquals(n, t.Value)).ToList();

                        if (boxes.Count == 0 && seeds.Count == 0) return;

                        if (!this.DigitalSignatureRelease(new Box[] { t.Value })) return;
                        if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                        var tboxes = selectBoxTreeViewItem.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                        selectBoxTreeViewItem.Value.Boxes.Clear();
                        selectBoxTreeViewItem.Value.Boxes.AddRange(tboxes);

                        foreach (var item in seeds)
                        {
                            selectBoxTreeViewItem.Value.Seeds.Remove(item);
                        }

                        t.Value.Boxes.AddRange(boxes);
                        t.Value.Seeds.AddRange(seeds);

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

                        foreach (var item in seeds)
                        {
                            selectBoxTreeViewItem.Value.Seeds.Remove(item);
                        }

                        t.Value.Boxes.AddRange(boxes);
                        t.Value.Seeds.AddRange(seeds);

                        selectBoxTreeViewItem.Update();
                        t.Update();

                        _listView.SelectedItems.Clear();
                    }
                }
            }

            this.Update();
        }

        private void _grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        #endregion

        #region _listView

        private void _listView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _selectedItems = _listView.SelectedItems.OfType<object>().ToList();

            if (_listView.GetCurrentIndex(e.GetPosition) == -1)
            {
                _listView.SelectedItems.Clear();
            }

            _isMouseDown = false;
        }

        private void _listView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_listView.GetCurrentIndex(e.GetPosition) < 0) return;

            if (_listView.SelectedItem is BoxListViewItem)
            {
                var selectBoxListViewItem = _listView.SelectedItem as BoxListViewItem;
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
            else if (_listView.SelectedItem is SeedListViewItem)
            {
                var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
                if (selectBoxTreeViewItem == null) return;

                var selectSeedListViewItem = _listView.SelectedItem as SeedListViewItem;
                if (selectSeedListViewItem == null) return;

                var seed = selectSeedListViewItem.Value;
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

                _amoebaManager.Download(seed.DeepClone(), baseDirectory, 0);
            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_refresh)
            {
                _listViewAddBoxMenuItem.IsEnabled = false;
                _listViewEditMenuItem.IsEnabled = false;
                _listViewDeleteMenuItem.IsEnabled = false;
                _listViewCutMenuItem.IsEnabled = false;
                _listViewCopyMenuItem.IsEnabled = false;
                _listViewCopyInfoMenuItem.IsEnabled = false;
                _listViewKeyUploadMenuItem.IsEnabled = false;
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
            _listViewKeyUploadMenuItem.IsEnabled = (selectItems.OfType<SeedListViewItem>().Count() > 0);
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

            if (_listView.SelectedItem is BoxListViewItem)
            {
                var tl = _listView.SelectedItem as BoxListViewItem;
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

            if (true == window.ShowDialog())
            {
                selectBoxTreeViewItem.Value.Boxes.Add(box);
                selectBoxTreeViewItem.IsExpanded = true;

                selectBoxTreeViewItem.Update();
            }

            this.Update();
        }

        private void _listViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (_listView.SelectedItem is BoxListViewItem)
            {
                var selectBoxListViewItem = _listView.SelectedItem as BoxListViewItem;
                if (selectBoxListViewItem == null) return;

                if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                var selectBox = selectBoxListViewItem.Value.DeepClone();
                if (selectBox == null) return;

                BoxEditWindow window = new BoxEditWindow(ref selectBox);
                window.Owner = _mainWindow;
                window.ShowDialog();

                selectBoxTreeViewItem.Value.Boxes.Remove(selectBoxListViewItem.Value);
                selectBoxTreeViewItem.Value.Boxes.Add(selectBox);

                selectBoxTreeViewItem.Update();
            }
            else if (_listView.SelectedItem is SeedListViewItem)
            {
                var selectSeedListViewItem = _listView.SelectedItem as SeedListViewItem;
                if (selectSeedListViewItem == null) return;

                if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

                var selectSeed = selectSeedListViewItem.Value.DeepClone();
                if (selectSeed == null) return;

                SeedEditWindow window = new SeedEditWindow(ref selectSeed, _amoebaManager);
                window.Owner = _mainWindow;
                window.ShowDialog();

                selectBoxTreeViewItem.Value.Seeds.Remove(selectSeedListViewItem.Value);
                selectBoxTreeViewItem.Value.Seeds.Add(selectSeed);

                selectBoxTreeViewItem.Update();
            }

            _listView.SelectedItems.Clear();

            this.Update();
        }

        private void _listViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

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

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _listViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

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

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

            Clipboard.SetBoxes(boxes.Select(n => n.DeepClone()));
            Clipboard.SetSeeds(seeds.Select(n => n.DeepClone()));
        }

        private void _listViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

            StringBuilder builder = new StringBuilder();

            foreach (var item in boxes)
            {
                builder.AppendLine(MessageConverter.ToInfoMessage(item));
            }

            foreach (var item in seeds)
            {
                builder.AppendLine(MessageConverter.ToInfoMessage(item));
            }

            Clipboard.SetText(builder.ToString());
        }

        private void _listViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
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

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            selectBoxTreeViewItem.Value.Boxes.AddRange(Clipboard.GetBoxes().Select(n => n.DeepClone()));
            selectBoxTreeViewItem.Value.Seeds.AddRange(Clipboard.GetSeeds().Select(n => n.DeepClone()));

            selectBoxTreeViewItem.Update();

            this.Update();
        }

        private void _listViewKeyUploadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(LanguagesManager.Instance.LibraryControl_KeyUpload_Message, "Key Upload", MessageBoxButton.YesNo)
                == MessageBoxResult.Yes)
            {
                var keys = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);

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

            var boxes = _listView.SelectedItems.OfType<BoxListViewItem>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewItem>().Select(n => n.Value);
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

            foreach (var seed in seeds)
            {
                var downloadDirectory = baseDirectory;
                var path = System.IO.Path.Combine(baseDirectory, seed.Name);

                if (File.Exists(System.IO.Path.Combine(_amoebaManager.DownloadDirectory, path))) continue;

                if (_amoebaManager.DownloadingInformation.Any(n =>
                    (n.Contains("Path") && ((string)n["Path"]) == downloadDirectory)
                    && (n.Contains("Name") && ((string)n["Name"]) == seed.Name)))
                {
                    continue;
                }

                _amoebaManager.Download(seed.DeepClone(), downloadDirectory, 0);
            }

            foreach (var box in boxes)
            {
                var boxList = new List<Box>();
                var seedList = new List<Seed>();
                var keyDictionary = new Dictionary<string, List<Seed>>();

                boxList.Add(box);

                for (int i = 0; i < boxList.Count; i++)
                {
                    boxList.AddRange(boxList[i].Boxes);
                }

                foreach (var item in boxList)
                {
                    seedList.AddRange(item.Seeds);
                }

                foreach (var seed in seedList)
                {
                    List<string> paths = new List<string>();
                    Box tempDirectory = null;

                    for (int i = boxList.Count - 1; i >= 0; i--)
                    {
                        if (tempDirectory == null)
                        {
                            if (boxList[i].Seeds.Contains(seed))
                            {
                                paths.Add(boxList[i].Name);
                                tempDirectory = boxList[i];
                            }
                        }
                        else
                        {
                            if (boxList[i].Boxes.Contains(tempDirectory))
                            {
                                paths.Add(boxList[i].Name);
                                tempDirectory = boxList[i];
                            }
                        }
                    }

                    string path = "";
                    paths.Reverse();

                    foreach (var item in paths)
                    {
                        path += string.Format("{0}{1}", LibraryControl.GetNormalizedPath(item), "/");
                    }

                    if (!keyDictionary.ContainsKey(path))
                        keyDictionary[path] = new List<Seed>();

                    keyDictionary[path].Add(seed);
                }

                foreach (var path in keyDictionary.Keys)
                {
                    foreach (var seed in keyDictionary[path])
                    {
                        var downloadDirectory = System.IO.Path.Combine(baseDirectory, path);
                        var path2 = System.IO.Path.Combine(downloadDirectory, seed.Name);

                        if (File.Exists(System.IO.Path.Combine(_amoebaManager.DownloadDirectory, path2)))
                        {
                            continue;
                        }

                        if (_amoebaManager.DownloadingInformation.Any(n =>
                            (n.Contains("Path") && ((string)n["Path"]) == downloadDirectory)
                            && (n.Contains("Name") && ((string)n["Name"]) == seed.Name)))
                        {
                            continue;
                        }

                        _amoebaManager.Download(seed.DeepClone(), downloadDirectory, 0);
                    }
                }
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

            if (true == window.ShowDialog())
            {
                selectBoxTreeViewItem.Value.Boxes.Add(box);
                selectBoxTreeViewItem.IsExpanded = true;

                selectBoxTreeViewItem.Update();
            }

            this.Update();
        }

        private void _boxTreeViewEditContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectBoxTreeViewItem = _boxTreeView.SelectedItem as BoxTreeViewItem;
            if (selectBoxTreeViewItem == null) return;

            if (!this.DigitalSignatureRelease(_boxTreeViewItem.GetLineage(selectBoxTreeViewItem).OfType<BoxTreeViewItem>().Select(n => n.Value))) return;

            var directory = selectBoxTreeViewItem.Value;
            BoxEditWindow window = new BoxEditWindow(ref directory);
            window.Owner = _mainWindow;
            window.ShowDialog();

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
                _listView.SelectedIndex = -1;

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
            _listView.Items.SortDescriptions.Add(new SortDescription("Index", direction));

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
                var view = (ListCollectionView)CollectionViewSource.GetDefaultView(_listView.ItemsSource);
                view.CustomSort = (IComparer)new KeywordsIComparer(direction);
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_CreationTime)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("CreationTime", direction));
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_Comment)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Comment", direction));
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
                if (x is BoxListViewItem)
                {
                    if (y is BoxListViewItem)
                    {
                        return 0;
                    }
                    else
                    {
                        return flag * -1;
                    }
                }
                else
                {
                    if (y is SeedListViewItem)
                    {
                        var xi = (SeedListViewItem)x;
                        var yi = (SeedListViewItem)y;

                        int c = Collection.Compare<string>(xi.Keywords, yi.Keywords);
                        if (c != 0) return flag * c;
                        return flag * xi.GetHashCode().CompareTo(yi.GetHashCode());
                    }
                    else
                    {
                        return flag * 1;
                    }
                }
            }
        }

        #endregion

        private class BoxListViewItem
        {
            public int Index { get { return 0; } }
            public string Name { get; set; }
            public string Signature { get; set; }
            public IEnumerable<string> Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public long Length { get; set; }
            public string Comment { get; set; }
            public Box Value { get; set; }

            public override int GetHashCode()
            {
                if (this.Value == null) return 0;
                else return this.Name.GetHashCode();
            }
        }

        private class SeedListViewItem
        {
            public int Index { get { return 1; } }
            public string Name { get; set; }
            public string Signature { get; set; }
            public IEnumerable<string> Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public long Length { get; set; }
            public string Comment { get; set; }
            public Seed Value { get; set; }

            public override int GetHashCode()
            {
                if (this.Value == null) return 0;
                else return this.Name.GetHashCode();
            }
        }
    }

    public class BoxTreeViewItem : TreeViewItem
    {
        private Box _value = new Box();

        public BoxTreeViewItem()
        {

        }

        public BoxTreeViewItem(Box box)
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
                    FontWeight = FontWeight.FromOpenTypeWeight(700),
                });
                w.Children.Add(new TextBlock() { Text = string.Format(" ({0})", this.Value.Seeds.Count) });

                this.Header = w;
            }

            List<BoxTreeViewItem> list = new List<BoxTreeViewItem>();

            foreach (var item in _value.Boxes)
            {
                list.Add(new BoxTreeViewItem(item));
            }

            foreach (var item in this.Items.OfType<BoxTreeViewItem>().ToArray())
            {
                if (!list.Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    this.Items.Remove(item);
                }
            }

            foreach (var item in list)
            {
                if (!this.Items.OfType<BoxTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    this.Items.Add(item);
                }
            }

            this.Items.SortDescriptions.Clear();
            this.Items.SortDescriptions.Add(new SortDescription("Value.Name", ListSortDirection.Ascending));
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
