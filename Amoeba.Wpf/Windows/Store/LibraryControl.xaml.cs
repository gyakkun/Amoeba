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
using System.Threading.Tasks;
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
using Library.Utilities;

namespace Amoeba.Windows
{
    /// <summary>
    /// LibraryControl.xaml の相互作用ロジック
    /// </summary>
    partial class LibraryControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private StoreControl _storeControl;

        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private AutoResetEvent _updateEvent = new AutoResetEvent(false);
        private volatile bool _refreshing = false;

        private BoxTreeViewModel _treeViewModel;
        private ObservableCollectionEx<IListViewModel> _listViewModelCollection = new ObservableCollectionEx<IListViewModel>();

        private Thread _searchThread;
        private Thread _watchThread;

        public LibraryControl(StoreControl storeControl, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _storeControl = storeControl;
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            _treeViewModel = new BoxTreeViewModel(null, Settings.Instance.LibraryControl_Box);

            InitializeComponent();

            _treeView.Items.Add(_treeViewModel);

            _listView.ItemsSource = _listViewModelCollection;

            foreach (var path in Settings.Instance.LibraryControl_ExpandedPaths.ToArray())
            {
                if (path.Count == 0 || path[0] != _treeViewModel.Value.Name) goto End;
                BoxTreeViewModel treeViewModel = _treeViewModel;

                foreach (var name in path.Skip(1))
                {
                    treeViewModel = treeViewModel.Children.OfType<BoxTreeViewModel>().FirstOrDefault(n => n.Value.Name == name);
                    if (treeViewModel == null) goto End;
                }

                treeViewModel.IsExpanded = true;

                continue;

                End:;

                Settings.Instance.LibraryControl_ExpandedPaths.Remove(path);
            }

            SelectionChangedEventHandler selectionChanged = (object sender, SelectionChangedEventArgs e) =>
            {
                if (e.OriginalSource != _mainWindow._tabControl && e.OriginalSource != _storeControl._tabControl) return;

                if (_mainWindow.SelectedTab == MainWindowTabType.Store && _storeControl.SelectedTab == StoreControlTabType.Library)
                {
                    this.Update_Title();
                }
            };

            _mainWindow._tabControl.SelectionChanged += selectionChanged;
            _storeControl._tabControl.SelectionChanged += selectionChanged;

            _searchThread = new Thread(this.SearchThread);
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "LibraryControl_SearchThread";
            _searchThread.Start();

            _watchThread = new Thread(this.WatchThread);
            _watchThread.Priority = ThreadPriority.Highest;
            _watchThread.IsBackground = true;
            _watchThread.Name = "LibraryControl_WatchThread";
            _watchThread.Start();

            _searchRowDefinition.Height = new GridLength(0);

            LanguagesManager.UsingLanguageChangedEvent += this.LanguagesManager_UsingLanguageChangedEvent;

            this.Update();
        }

        private void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {
            _listView.Items.Refresh();
        }

        private void SearchThread()
        {
            try
            {
                for (;;)
                {
                    _updateEvent.WaitOne();

                    try
                    {
                        _refreshing = true;

                        BoxTreeViewModel tempTreeViewModel = null;

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            tempTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
                            _listView.ContextMenu.IsOpen = false;
                        }));

                        if (tempTreeViewModel == null) continue;

                        var newList = new HashSet<IListViewModel>(new ReferenceEqualityComparer());

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

                        foreach (var box in tempTreeViewModel.Value.Boxes)
                        {
                            if (words != null)
                            {
                                var text = (box.Name ?? "").ToLower();
                                if (!words.All(n => text.Contains(n))) continue;
                            }

                            var boxesListViewModel = new BoxListViewModel();
                            boxesListViewModel.Index = newList.Count;
                            boxesListViewModel.Name = box.Name;
                            boxesListViewModel.Length = BoxUtils.GetLength(box);
                            boxesListViewModel.CreationTime = BoxUtils.GetCreationTime(box);
                            boxesListViewModel.Value = box;

                            newList.Add(boxesListViewModel);
                        }

                        foreach (var seed in tempTreeViewModel.Value.Seeds)
                        {
                            if (words != null)
                            {
                                var text = (seed.Name ?? "").ToLower();
                                if (!words.All(n => text.Contains(n))) continue;
                            }

                            var seedListViewModel = new SeedListViewModel();
                            seedListViewModel.Index = newList.Count;
                            seedListViewModel.Name = seed.Name;
                            if (seed.Certificate != null) seedListViewModel.Signature = seed.Certificate.ToString();
                            seedListViewModel.Length = seed.Length;
                            seedListViewModel.Keywords = string.Join(", ", seed.Keywords.Where(n => !string.IsNullOrWhiteSpace(n)));
                            seedListViewModel.CreationTime = seed.CreationTime;

                            seedListViewModel.State = _storeControl.GetState(seed);

                            seedListViewModel.Value = seed;

                            newList.Add(seedListViewModel);
                        }

                        var sortList = this.Sort(newList, 100000);

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            if (tempTreeViewModel != _treeView.SelectedItem) return;

                            _listViewModelCollection.Clear();
                            _listViewModelCollection.AddRange(sortList);

                            this.Update_Title();
                        }));
                    }
                    finally
                    {
                        _refreshing = false;
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void WatchThread()
        {
            try
            {
                for (;;)
                {
                    Thread.Sleep(1000 * 3);

                    try
                    {
                        if (Directory.Exists(_serviceManager.Paths["Input"]))
                        {
                            this.OpenBox(_serviceManager.Paths["Input"]);
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
                                var treeViewModel = this.GetExtractToBox();

                                bool flag = true;

                                if (!LibraryControl.CheckDigitalSignature(box))
                                {
                                    flag = MessageBox.Show(
                                            _mainWindow,
                                            string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message),
                                            "Library",
                                            MessageBoxButton.OKCancel,
                                            MessageBoxImage.Asterisk) == MessageBoxResult.OK;
                                }

                                if (flag)
                                {
                                    treeViewModel.Value.Boxes.Remove(box);
                                    treeViewModel.Value.Boxes.Add(box);
                                }

                                treeViewModel.Update();
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

        private BoxTreeViewModel GetExtractToBox()
        {
            var paths = Settings.Instance.Global_BoxExtractTo_Path.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (paths.Count == 0 || _treeViewModel.Value.Name != paths[0]) return _treeViewModel;

            BoxTreeViewModel treeViewModel = _treeViewModel;

            for (int i = 1; i < paths.Count; i++)
            {
                treeViewModel = treeViewModel.Children.OfType<BoxTreeViewModel>().FirstOrDefault(n => n.Value.Name == paths[i]);
                if (treeViewModel == null) return _treeViewModel;
            }

            return treeViewModel;
        }

        private static bool CheckDigitalSignature(Box box)
        {
            bool flag = true;

            var seedList = new List<Seed>();
            {
                var boxList = new List<Box>();
                boxList.Add(box);

                for (int i = 0; i < boxList.Count; i++)
                {
                    boxList.AddRange(boxList[i].Boxes);
                    seedList.AddRange(boxList[i].Seeds);
                }
            }

            foreach (var item in seedList)
            {
                if (!item.VerifyCertificate())
                {
                    flag = false;

                    item.CreateCertificate(null);
                }
            }

            return flag;
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
            _mainWindow.Title = string.Format("Amoeba {0}", _serviceManager.AmoebaVersion);
            _updateEvent.Set();
        }

        private void Update_Title()
        {
            if (_mainWindow.SelectedTab == MainWindowTabType.Store && _storeControl.SelectedTab == StoreControlTabType.Library)
            {
                if (_treeView.SelectedItem is BoxTreeViewModel)
                {
                    var selectTreeViewModel = (BoxTreeViewModel)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Amoeba {0} - {1}", _serviceManager.AmoebaVersion, selectTreeViewModel.Value.Name);
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
                    if (_treeViewModel == _treeView.SelectedItem) return;

                    var data = new DataObject("TreeViewModel", _treeView.SelectedItem);
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
                    if (_refreshing || _listView.SelectedItems.Count == 0) return;

                    var data = new DataObject("ListViewModels", _listView.SelectedItems);
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

                var destinationItem = this.GetDropDestination(e.GetPosition, (UIElement)e.OriginalSource);
                if (destinationItem == null) destinationItem = (TreeViewModelBase)_treeView.SelectedItem;

                if (destinationItem is BoxTreeViewModel)
                {
                    var d = (BoxTreeViewModel)destinationItem;

                    foreach (string filePath in paths)
                    {
                        using (FileStream stream = new FileStream(filePath, FileMode.Open))
                        {
                            try
                            {
                                var box = AmoebaConverter.FromBoxStream(stream);
                                if (box == null) continue;

                                bool flag = true;

                                if (!LibraryControl.CheckDigitalSignature(box))
                                {
                                    flag = MessageBox.Show(
                                            _mainWindow,
                                            string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message),
                                            "Library",
                                            MessageBoxButton.OKCancel,
                                            MessageBoxImage.Asterisk) == MessageBoxResult.OK;
                                }

                                if (flag)
                                {
                                    d.Value.Boxes.Add(box);
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
                if (e.Data.GetDataPresent("TreeViewModel"))
                {
                    var sourceItem = (TreeViewModelBase)e.Data.GetData("TreeViewModel");

                    if (sourceItem is BoxTreeViewModel)
                    {
                        var destinationItem = this.GetDropDestination(e.GetPosition, (UIElement)e.OriginalSource);

                        if (destinationItem is BoxTreeViewModel)
                        {
                            var s = (BoxTreeViewModel)sourceItem;
                            var d = (BoxTreeViewModel)destinationItem;

                            if (d.Value.Boxes.Any(n => object.ReferenceEquals(n, s.Value))) return;
                            if (d.GetAncestors().Any(n => object.ReferenceEquals(n, s))) return;

                            var parentItem = s.Parent;

                            if (parentItem is BoxTreeViewModel)
                            {
                                var p = (BoxTreeViewModel)parentItem;

                                var tItems = p.Value.Boxes.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                                p.Value.Boxes.Clear();
                                p.Value.Boxes.AddRange(tItems);

                                p.Update();
                            }

                            d.Value.Boxes.Add(s.Value);
                            d.IsSelected = true;

                            d.Update();
                        }
                    }
                }
                else if (e.Data.GetDataPresent("ListViewModels"))
                {
                    var boxes = ((IList)e.Data.GetData("ListViewModels")).OfType<BoxListViewModel>().Select(n => n.Value).ToList();
                    var seeds = ((IList)e.Data.GetData("ListViewModels")).OfType<SeedListViewModel>().Select(n => n.Value).ToList();
                    bool isListView = _listView.GetCurrentIndex(e.GetPosition) != -1;

                    var parentItem = (TreeViewModelBase)_treeView.SelectedItem;

                    if (parentItem is BoxTreeViewModel)
                    {
                        var destinationItem = this.GetDropDestination(e.GetPosition, (UIElement)e.OriginalSource);

                        if (destinationItem is BoxTreeViewModel)
                        {
                            var p = (BoxTreeViewModel)parentItem;
                            var d = (BoxTreeViewModel)destinationItem;

                            if (p == d) return;

                            boxes = boxes.Where(n => !object.ReferenceEquals(n, d.Value)).ToList();

                            if (boxes.Count == 0 && seeds.Count == 0) return;

                            var tboxes = p.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                            p.Value.Boxes.Clear();
                            p.Value.Boxes.AddRange(tboxes);
                            var tseeds = p.Value.Seeds.Where(n => !seeds.Any(m => object.ReferenceEquals(n, m))).ToArray();
                            p.Value.Seeds.Clear();
                            p.Value.Seeds.AddRange(tseeds);

                            p.Update();

                            d.Value.Boxes.AddRange(boxes);
                            d.Value.Seeds.AddRange(seeds);
                            if (!isListView) d.IsSelected = true;

                            d.Update();
                        }
                    }
                }
            }

            this.Update();
        }

        private TreeViewModelBase GetDropDestination(GetPositionDelegate getPosition, UIElement originalSource)
        {
            var posithonIndex = _listView.GetCurrentIndex(getPosition);

            if (posithonIndex != -1)
            {
                var selectItem = _treeView.SelectedItem;

                if (selectItem is BoxTreeViewModel)
                {
                    var listViewModel = _listView.Items[posithonIndex];

                    if (listViewModel is BoxListViewModel)
                    {
                        var selectTreeViewModel = (BoxTreeViewModel)selectItem;
                        var boxListViewModel = (BoxListViewModel)listViewModel;

                        return selectTreeViewModel.Children.OfType<BoxTreeViewModel>().FirstOrDefault(n => object.ReferenceEquals(n.Value, boxListViewModel.Value));
                    }
                }
            }
            else
            {
                var element = originalSource.FindAncestor<TreeViewItem>();
                if (element == null) return null;

                return (TreeViewModelBase)_treeView.SearchItemFromElement(element) as TreeViewModelBase;
            }

            return null;
        }

        private TreeViewModelBase GetSelectedItem()
        {
            var selectIndex = _listView.SelectedIndex;

            if (selectIndex != -1)
            {
                var selectItem = _treeView.SelectedItem;

                if (selectItem is BoxTreeViewModel)
                {
                    var listViewModel = _listView.Items[selectIndex];

                    if (listViewModel is BoxListViewModel)
                    {
                        var selectTreeViewModel = (BoxTreeViewModel)selectItem;
                        var boxListViewModel = (BoxListViewModel)listViewModel;

                        return selectTreeViewModel.Children.OfType<BoxTreeViewModel>().FirstOrDefault(n => object.ReferenceEquals(n.Value, boxListViewModel.Value));
                    }
                }
            }
            else
            {
                return (TreeViewModelBase)_treeView.SelectedItem;
            }

            return null;
        }

        #endregion

        #region _treeView

        private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private void TreeViewItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = ((UIElement)e.OriginalSource).FindAncestor<TreeViewItem>();
            if (element == null) return;

            var item = _treeView.SearchItemFromElement(element) as TreeViewModelBase;
            if (item == null) return;

            item.IsSelected = true;

            e.Handled = true;
        }

        private void _treeView_PreviewDragOver(object sender, DragEventArgs e)
        {
            Point position = MouseUtils.GetMousePosition(_treeView);

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
            var element = ((UIElement)e.OriginalSource).FindAncestor<TreeViewItem>();
            if (element == null) return;

            var item = _treeView.SearchItemFromElement(element) as TreeViewModelBase;
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
            this.Update();
        }

        private void _treeView_Expanded(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            var treeViewModel = (TreeViewModelBase)_treeView.SearchItemFromElement((DependencyObject)treeViewItem);

            var path = new Route();

            foreach (var item in treeViewModel.GetAncestors())
            {
                if (item is BoxTreeViewModel) path.Add(((BoxTreeViewModel)item).Value.Name);
            }

            Settings.Instance.LibraryControl_ExpandedPaths.Add(path);
        }

        private void _treeView_Collapsed(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            var treeViewModel = (TreeViewModelBase)_treeView.SearchItemFromElement((DependencyObject)treeViewItem);

            var path = new Route();

            foreach (var item in treeViewModel.GetAncestors())
            {
                if (item is BoxTreeViewModel) path.Add(((BoxTreeViewModel)item).Value.Name);
            }

            Settings.Instance.LibraryControl_ExpandedPaths.Remove(path);
        }

        private void _treeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var treeViewItem = sender as TreeViewItem;
            if (treeViewItem == null) return;

            var treeViewModel = (TreeViewModelBase)_treeView.SearchItemFromElement((DependencyObject)treeViewItem);
            if (_treeView.SelectedItem != treeViewModel) return;

            var contextMenu = treeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            _startPoint = new Point(-1, -1);

            MenuItem treeViewItemDeleteMenuItem = contextMenu.GetItem<MenuItem>("_treeViewItemDeleteMenuItem");
            MenuItem treeViewItemCutMenuItem = contextMenu.GetItem<MenuItem>("_treeViewItemCutMenuItem");
            MenuItem treeViewItemPasteMenuItem = contextMenu.GetItem<MenuItem>("_treeViewItemPasteMenuItem");

            treeViewItemDeleteMenuItem.IsEnabled = (_treeViewModel != treeViewModel);
            treeViewItemCutMenuItem.IsEnabled = (_treeViewModel != treeViewModel);
            treeViewItemPasteMenuItem.IsEnabled = Clipboard.ContainsBoxes() || Clipboard.ContainsSeeds();
        }

        private void _treeViewItemNewBoxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null) return;

            Box box;

            if (!selectTreeViewModel.Value.Boxes.Any(n => n.Name == "New box"))
            {
                box = new Box() { Name = "New box" };
            }
            else
            {
                int i = 1;
                while (selectTreeViewModel.Value.Boxes.Any(n => n.Name == "New box_" + i)) i++;

                box = new Box() { Name = "New box_" + i };
            }

            var window = new BoxEditWindow(box);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewModel.Value.Boxes.Add(box);

                selectTreeViewModel.Update();
            }

            this.Update();
        }

        private void _treeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null) return;

            var box = selectTreeViewModel.Value;

            var window = new BoxEditWindow(box);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewModel.Update();
            }

            this.Update();
        }

        private void _treeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null || selectTreeViewModel == _treeViewModel) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Library", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parentItem = selectTreeViewModel.Parent;

            if (parentItem is BoxTreeViewModel)
            {
                var p = (BoxTreeViewModel)parentItem;

                p.Value.Boxes.Remove(selectTreeViewModel.Value);

                p.Update();
            }

            this.Update();
        }

        private void _treeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null || selectTreeViewModel == _treeViewModel) return;

            Clipboard.SetBoxes(new Box[] { selectTreeViewModel.Value });

            var parentItem = selectTreeViewModel.Parent;

            if (parentItem is BoxTreeViewModel)
            {
                var p = (BoxTreeViewModel)parentItem;

                p.Value.Boxes.Remove(selectTreeViewModel.Value);

                p.Update();
            }

            this.Update();
        }

        private void _treeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null) return;

            Clipboard.SetBoxes(new Box[] { selectTreeViewModel.Value });
        }

        private void _treeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null) return;

            selectTreeViewModel.Value.Boxes.AddRange(Clipboard.GetBoxes());
            selectTreeViewModel.Value.Seeds.AddRange(Clipboard.GetSeeds());

            selectTreeViewModel.Update();

            this.Update();
        }

        private void _treeViewItemImportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null) return;

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

                                bool flag = true;

                                if (!LibraryControl.CheckDigitalSignature(box))
                                {
                                    flag = MessageBox.Show(
                                            _mainWindow,
                                            string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message),
                                            "Library",
                                            MessageBoxButton.OKCancel,
                                            MessageBoxImage.Asterisk) == MessageBoxResult.OK;
                                }

                                if (flag)
                                {
                                    selectTreeViewModel.Value.Boxes.Add(box);
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }

                    selectTreeViewModel.Update();
                    this.Update();
                }
            }
        }

        private void _treeViewItemExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null) return;

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.RestoreDirectory = true;
                dialog.FileName = selectTreeViewModel.Value.Name;
                dialog.DefaultExt = ".box";
                dialog.Filter = "Box (*.box)|*.box";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileName = dialog.FileName;

                    using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                    using (Stream boxStream = AmoebaConverter.ToBoxStream(selectTreeViewModel.Value))
                    using (var safeBuffer = _bufferManager.CreateSafeBuffer(1024 * 4))
                    {
                        int length;

                        while ((length = boxStream.Read(safeBuffer.Value, 0, safeBuffer.Value.Length)) > 0)
                        {
                            fileStream.Write(safeBuffer.Value, 0, length);
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
            Point position = MouseUtils.GetMousePosition(_listView);

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

        private void _listView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var posithonIndex = _listView.GetCurrentIndex(e.GetPosition);

            if (posithonIndex != -1)
            {
                var selectItem = _treeView.SelectedItem;

                if (selectItem is BoxTreeViewModel)
                {
                    var listViewModel = _listView.Items[posithonIndex];

                    if (listViewModel is BoxListViewModel)
                    {
                        var selectTreeViewModel = (BoxTreeViewModel)selectItem;
                        var boxListViewModel = (BoxListViewModel)listViewModel;

                        var item = selectTreeViewModel.Children.OfType<BoxTreeViewModel>().FirstOrDefault(n => object.ReferenceEquals(n.Value, boxListViewModel.Value));

                        {
                            selectTreeViewModel.IsExpanded = true;
                            item.IsSelected = true;
                        }
                    }
                    else if (listViewModel is SeedListViewModel)
                    {
                        var selectTreeViewModel = (BoxTreeViewModel)selectItem;
                        var seedListViewModel = (SeedListViewModel)listViewModel;

                        string baseDirectory = "";

                        {
                            var path = new List<string>();

                            foreach (var item in selectTreeViewModel.GetAncestors())
                            {
                                if (item is BoxTreeViewModel) path.Add(((BoxTreeViewModel)item).Value.Name);
                            }

                            baseDirectory = System.IO.Path.Combine(path.ToArray());
                        }

                        var seed = seedListViewModel.Value;

                        Task.Run(() =>
                        {
                            Thread.CurrentThread.IsBackground = true;

                            try
                            {
                                _amoebaManager.Download(seed, baseDirectory, 3);
                                _storeControl.SetState(seed, SearchState.Downloading);
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

            if (_refreshing || _treeView.SelectedItem == null)
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
                _listViewDownloadMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

                // Paste
                {
                    var destinationItem = this.GetSelectedItem();

                    if (destinationItem is BoxTreeViewModel)
                    {
                        _listViewPasteMenuItem.IsEnabled = Clipboard.ContainsBoxes() || Clipboard.ContainsSeeds();
                    }
                }
            }
        }

        private void _listViewNewBoxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var destinationItem = this.GetSelectedItem();

            if (destinationItem is BoxTreeViewModel)
            {
                var d = (BoxTreeViewModel)destinationItem;

                Box box;

                if (!d.Value.Boxes.Any(n => n.Name == "New box"))
                {
                    box = new Box() { Name = "New box" };
                }
                else
                {
                    int i = 1;
                    while (d.Value.Boxes.Any(n => n.Name == "New box_" + i)) i++;

                    box = new Box() { Name = "New box_" + i };
                }

                var window = new BoxEditWindow(box);
                window.Owner = _mainWindow;

                if (window.ShowDialog() == true)
                {
                    d.Value.Boxes.Add(box);

                    d.Update();
                }
            }

            this.Update();
        }

        private void _listViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as TreeViewModelBase;
            if (selectTreeViewModel == null) return;

            if (selectTreeViewModel is BoxTreeViewModel)
            {
                var selectBoxTreeViewModel = (BoxTreeViewModel)selectTreeViewModel;

                if (_listView.SelectedItem is BoxListViewModel)
                {
                    var editBox = ((BoxListViewModel)_listView.SelectedItem).Value;

                    var window = new BoxEditWindow(editBox);
                    window.Owner = _mainWindow;

                    if (window.ShowDialog() == true)
                    {
                        selectBoxTreeViewModel.Update();
                    }
                }
                else if (_listView.SelectedItem is SeedListViewModel)
                {
                    var selectSeedListViewModels = _listView.SelectedItems.OfType<SeedListViewModel>();
                    if (selectSeedListViewModels == null) return;

                    var editSeeds = selectSeedListViewModels.Select(n => n.Value.Clone()).ToList();
                    if (editSeeds == null) return;

                    var window = new SeedEditWindow(editSeeds);
                    window.Owner = _mainWindow;

                    if (window.ShowDialog() == true)
                    {
                        selectBoxTreeViewModel.Update();
                    }
                }
            }

            this.Update();
        }

        private void _listViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;

            var selectTreeViewModel = _treeView.SelectedItem as TreeViewModelBase;
            if (selectTreeViewModel == null) return;

            if (selectTreeViewModel is BoxTreeViewModel)
            {
                var selectBoxTreeViewModel = (BoxTreeViewModel)selectTreeViewModel;

                if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

                var boxes = _listView.SelectedItems.OfType<BoxListViewModel>().Select(n => n.Value);
                var seeds = _listView.SelectedItems.OfType<SeedListViewModel>().Select(n => n.Value);

                foreach (var item in boxes)
                {
                    selectBoxTreeViewModel.Value.Boxes.Remove(item);
                }

                foreach (var item in seeds)
                {
                    selectBoxTreeViewModel.Value.Seeds.Remove(item);
                }

                selectBoxTreeViewModel.Update();
            }

            this.Update();
        }

        private void _listViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;

            var selectTreeViewModel = _treeView.SelectedItem as TreeViewModelBase;
            if (selectTreeViewModel == null) return;

            if (selectTreeViewModel is BoxTreeViewModel)
            {
                var selectBoxTreeViewModel = (BoxTreeViewModel)selectTreeViewModel;

                var boxes = _listView.SelectedItems.OfType<BoxListViewModel>().Select(n => n.Value);
                var seeds = _listView.SelectedItems.OfType<SeedListViewModel>().Select(n => n.Value);

                Clipboard.SetBoxAndSeeds(boxes, seeds);

                foreach (var item in boxes)
                {
                    selectBoxTreeViewModel.Value.Boxes.Remove(item);
                }

                foreach (var item in seeds)
                {
                    selectBoxTreeViewModel.Value.Seeds.Remove(item);
                }

                selectBoxTreeViewModel.Update();
            }

            this.Update();
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var boxes = _listView.SelectedItems.OfType<BoxListViewModel>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewModel>().Select(n => n.Value);

            Clipboard.SetBoxAndSeeds(boxes, seeds);
        }

        private void _listViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var boxes = _listView.SelectedItems.OfType<BoxListViewModel>().Select(n => n.Value);
            var seeds = _listView.SelectedItems.OfType<SeedListViewModel>().Select(n => n.Value);

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

            if (destinationItem is BoxTreeViewModel)
            {
                var d = (BoxTreeViewModel)destinationItem;

                d.Value.Boxes.AddRange(Clipboard.GetBoxes());
                d.Value.Seeds.AddRange(Clipboard.GetSeeds());

                d.Update();
            }

            this.Update();
        }

        private void _listViewDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as TreeViewModelBase;
            if (selectTreeViewModel == null) return;

            string baseDirectory = "";

            {
                var path = new List<string>();

                foreach (var item in selectTreeViewModel.GetAncestors())
                {
                    if (item is BoxTreeViewModel) path.Add(((BoxTreeViewModel)item).Value.Name);
                }

                baseDirectory = System.IO.Path.Combine(path.ToArray());
            }

            var list = new List<KeyValuePair<Seed, string>>();

            {
                foreach (var seed in _listView.SelectedItems.OfType<SeedListViewModel>().Select(n => n.Value))
                {
                    list.Add(new KeyValuePair<Seed, string>(seed.Clone(), baseDirectory));
                }

                foreach (var box in _listView.SelectedItems.OfType<BoxListViewModel>().Select(n => n.Value))
                {
                    this.BoxDownload(box, baseDirectory, list);
                }
            }

            Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var pair in list)
                    {
                        var seed = pair.Key;
                        var path = pair.Value;

                        _amoebaManager.Download(seed, path, 3);
                        _storeControl.SetState(seed, SearchState.Downloading);
                    }
                }
                catch (Exception)
                {

                }
            });
        }

        private void BoxDownload(Box currentBox, string baseDirectory, List<KeyValuePair<Seed, string>> list)
        {
            baseDirectory = System.IO.Path.Combine(baseDirectory, currentBox.Name);

            foreach (var seed in currentBox.Seeds)
            {
                list.Add(new KeyValuePair<Seed, string>(seed.Clone(), baseDirectory));
            }

            foreach (var box in currentBox.Boxes)
            {
                this.BoxDownload(box, baseDirectory, list);
            }
        }

        #endregion

        private void _searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.Update();
            }
        }

        #region Sort

        private void _listView_GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var item = e.OriginalSource as GridViewColumnHeader;
            if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

            var headerClicked = item.Column.Header as string;
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

            Settings.Instance.LibraryControl_LastHeaderClicked = headerClicked;
            Settings.Instance.LibraryControl_ListSortDirection = direction;

            this.Update();
        }

        private IEnumerable<IListViewModel> Sort(IEnumerable<IListViewModel> collection, int maxCount)
        {
            var sortBy = Settings.Instance.LibraryControl_LastHeaderClicked;
            var direction = Settings.Instance.LibraryControl_ListSortDirection;

            var list = new List<IListViewModel>(collection);

            if (sortBy == LanguagesManager.Instance.LibraryControl_Name)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Type.CompareTo(y.Type);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Index.CompareTo(y.Index);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_Signature)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Type.CompareTo(y.Type);
                    if (c != 0) return c;

                    if (x.Signature != null)
                    {
                        if (y.Signature != null)
                        {
                            c = x.Signature.CompareTo(y.Signature);
                        }
                        else
                        {
                            c = 1;
                        }
                    }
                    else
                    {
                        if (y.Signature != null)
                        {
                            c = -1;
                        }
                    }

                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Index.CompareTo(y.Index);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_Length)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Type.CompareTo(y.Type);
                    if (c != 0) return c;
                    c = x.Length.CompareTo(y.Length);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Index.CompareTo(y.Index);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_Keywords)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Type.CompareTo(y.Type);
                    if (c != 0) return c;
                    c = x.Keywords.CompareTo(y.Keywords);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Index.CompareTo(y.Index);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_CreationTime)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Type.CompareTo(y.Type);
                    if (c != 0) return c;
                    c = x.CreationTime.CompareTo(y.CreationTime);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Index.CompareTo(y.Index);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.LibraryControl_State)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Type.CompareTo(y.Type);
                    if (c != 0) return c;
                    c = x.State.CompareTo(y.State);
                    if (c != 0) return c;
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Index.CompareTo(y.Index);
                    if (c != 0) return c;

                    return 0;
                });
            }

            if (direction == ListSortDirection.Descending)
            {
                list.Reverse();
            }

            if (list.Count <= maxCount) return list;
            else return list.GetRange(0, maxCount);
        }

        #endregion

        private interface IListViewModel
        {
            int Type { get; }
            int Index { get; }
            string Name { get; }
            string Signature { get; }
            long Length { get; }
            string Keywords { get; }
            DateTime CreationTime { get; }
            SearchState State { get; }
            object Value { get; }
        }

        private class BoxListViewModel : IListViewModel, IEquatable<BoxListViewModel>
        {
            int IListViewModel.Type { get { return 0; } }
            public int Index { get; set; }
            public string Name { get; set; }
            public string Signature { get; set; }
            public long Length { get; set; }
            string IListViewModel.Keywords { get { return null; } }
            public DateTime CreationTime { get; set; }
            SearchState IListViewModel.State { get { return (SearchState)0; } }
            object IListViewModel.Value { get { return this.Value; } }

            public Box Value { get; set; }

            public override int GetHashCode()
            {
                if (this.Name == null) return 0;
                else return this.Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if ((object)obj == null || !(obj is BoxListViewModel)) return false;

                return this.Equals((BoxListViewModel)obj);
            }

            public bool Equals(BoxListViewModel other)
            {
                if ((object)other == null) return false;
                if (object.ReferenceEquals(this, other)) return true;

                if (this.Index != other.Index
                    || this.Name != other.Name
                    || this.Signature != other.Signature
                    || this.Length != other.Length
                    || this.CreationTime != other.CreationTime
                    || this.Value != other.Value)
                {
                    return false;
                }

                return true;
            }
        }

        private class SeedListViewModel : IListViewModel, IEquatable<SeedListViewModel>
        {
            int IListViewModel.Type { get { return 1; } }
            public int Index { get; set; }
            public string Name { get; set; }
            public string Signature { get; set; }
            public long Length { get; set; }
            public string Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public SearchState State { get; set; }
            object IListViewModel.Value { get { return this.Value; } }

            public Seed Value { get; set; }

            public override int GetHashCode()
            {
                if (this.Name == null) return 0;
                else return this.Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if ((object)obj == null || !(obj is SeedListViewModel)) return false;

                return this.Equals((SeedListViewModel)obj);
            }

            public bool Equals(SeedListViewModel other)
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

        private void Execute_Close(object sender, ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(0);
            _searchTextBox.Text = "";

            this.Update();
        }
    }
}
