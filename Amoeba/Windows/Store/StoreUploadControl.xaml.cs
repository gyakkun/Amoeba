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
using System.Xml;
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
    /// Interaction logic for StoreUploadControl.xaml
    /// </summary>
    partial class StoreUploadControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private StoreControl _storeControl;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private AutoResetEvent _updateEvent = new AutoResetEvent(false);
        private volatile bool _refreshing = false;

        private StoreCategorizeTreeViewModel _treeViewModel;
        private ObservableCollectionEx<IListViewModel> _listViewModelCollection = new ObservableCollectionEx<IListViewModel>();

        private Thread _searchThread;

        public StoreUploadControl(StoreControl storeControl, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _storeControl = storeControl;
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            _treeViewModel = new StoreCategorizeTreeViewModel(null, Settings.Instance.StoreUploadControl_StoreCategorizeTreeItem);

            InitializeComponent();

            _treeView.Items.Add(_treeViewModel);

            _listView.ItemsSource = _listViewModelCollection;

            foreach (var path in Settings.Instance.StoreUploadControl_ExpandedPaths.ToArray())
            {
                if (path.Count == 0 || path[0] != _treeViewModel.Value.Name) goto End;
                TreeViewModelBase treeViewModel = _treeViewModel;

                foreach (var name in path.Skip(1))
                {
                    treeViewModel = treeViewModel.Children.OfType<TreeViewModelBase>().FirstOrDefault(n =>
                    {
                        if (n is StoreCategorizeTreeViewModel) return ((StoreCategorizeTreeViewModel)n).Value.Name == name;
                        else if (n is StoreTreeViewModel) return ((StoreTreeViewModel)n).Value.Signature == name;
                        else if (n is BoxTreeViewModel) return ((BoxTreeViewModel)n).Value.Name == name;

                        return false;
                    });

                    if (treeViewModel == null) goto End;
                }

                treeViewModel.IsExpanded = true;
                continue;

                End:;

                Settings.Instance.StoreUploadControl_ExpandedPaths.Remove(path);
            }

            SelectionChangedEventHandler selectionChanged = (object sender, SelectionChangedEventArgs e) =>
            {
                if (e.OriginalSource != _mainWindow._tabControl && e.OriginalSource != _storeControl._tabControl) return;

                if (_mainWindow.SelectedTab == MainWindowTabType.Store && _storeControl.SelectedTab == StoreControlTabType.Upload)
                {
                   this.Update_Title();
                }
            };

            _mainWindow._tabControl.SelectionChanged += selectionChanged;
            _storeControl._tabControl.SelectionChanged += selectionChanged;

            _searchThread = new Thread(this.SearchThread);
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "StoreUploadControl_SearchThread";
            _searchThread.Start();
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

                        TreeViewModelBase tempTreeViewModel = null;

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            tempTreeViewModel = (TreeViewModelBase)_treeView.SelectedItem;
                            _listView.ContextMenu.IsOpen = false;
                        }));

                        if (tempTreeViewModel is StoreCategorizeTreeViewModel)
                        {
                            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                if (tempTreeViewModel != _treeView.SelectedItem) return;

                                _listViewModelCollection.Clear();

                                this.Update_Title();
                            }));
                        }
                        else if (tempTreeViewModel is StoreTreeViewModel || tempTreeViewModel is BoxTreeViewModel)
                        {
                            var boxes = new BoxCollection();
                            var seeds = new SeedCollection();

                            if (tempTreeViewModel is StoreTreeViewModel)
                            {
                                var storeTreeViewModel = (StoreTreeViewModel)tempTreeViewModel;

                                boxes.AddRange(storeTreeViewModel.Value.Boxes);
                            }
                            else if (tempTreeViewModel is BoxTreeViewModel)
                            {
                                var boxTreeViewModel = (BoxTreeViewModel)tempTreeViewModel;

                                boxes.AddRange(boxTreeViewModel.Value.Boxes);
                                seeds.AddRange(boxTreeViewModel.Value.Seeds);
                            }

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

                            foreach (var box in boxes)
                            {
                                if (words != null)
                                {
                                    var text = (box.Name ?? "").ToLower();
                                    if (!words.All(n => text.Contains(n))) continue;
                                }

                                var boxesListViewModel = new BoxListViewModel();
                                boxesListViewModel.Index = newList.Count;
                                boxesListViewModel.Name = box.Name;
                                if (box.Certificate != null) boxesListViewModel.Signature = box.Certificate.ToString();
                                boxesListViewModel.Length = BoxUtilities.GetBoxLength(box);
                                boxesListViewModel.CreationTime = BoxUtilities.GetBoxCreationTime(box);
                                boxesListViewModel.Value = box;

                                newList.Add(boxesListViewModel);
                            }

                            foreach (var seed in seeds)
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
                    }
                    finally
                    {
                        _refreshing = false;
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

        private bool DigitalSignatureRelease(IEnumerable<BoxTreeViewModel> treeViewModels)
        {
            var targetList = new List<BoxTreeViewModel>();
            var builder = new StringBuilder();

            foreach (var item in treeViewModels)
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
                    string.Format("{0}\r\n{1}", builder.ToString(), LanguagesManager.Instance.StoreUploadControl_DigitalSignatureAnnulled_Message),
                    "StoreUpload",
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
            this.Update_TreeView_Color();

            _mainWindow.Title = string.Format("Amoeba {0}", App.AmoebaVersion);
            _updateEvent.Set();
        }

        private void Update_Title()
        {
            if (_mainWindow.SelectedTab == MainWindowTabType.Store && _storeControl.SelectedTab == StoreControlTabType.Upload)
            {
                if (_treeView.SelectedItem is StoreCategorizeTreeViewModel)
                {
                    var selectTreeViewModel = (StoreCategorizeTreeViewModel)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectTreeViewModel.Value.Name);
                }
                else if (_treeView.SelectedItem is StoreTreeViewModel)
                {
                    var selectTreeViewModel = (StoreTreeViewModel)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectTreeViewModel.Value.Signature);
                }
                else if (_treeView.SelectedItem is BoxTreeViewModel)
                {
                    var selectTreeViewModel = (BoxTreeViewModel)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Amoeba {0} - {1}", App.AmoebaVersion, selectTreeViewModel.Value.Name);
                }
            }
        }

        private void Update_TreeView_Color()
        {
            var selectTreeViewModel = _treeView.SelectedItem as TreeViewModelBase;

            {
                var items = new List<TreeViewModelBase>();
                items.Add(_treeViewModel);

                for (int i = 0; i < items.Count; i++)
                {
                    foreach (var item in items[i].Children)
                    {
                        items.Add(item);
                    }
                }

                var hitItems = new HashSet<TreeViewModelBase>();

                foreach (var item in items.OfType<StoreTreeViewModel>().Where(n => n.Value.IsUpdated))
                {
                    hitItems.UnionWith(item.GetAncestors());
                }

                foreach (var item in items)
                {
                    if (item is StoreCategorizeTreeViewModel)
                    {
                        ((StoreCategorizeTreeViewModel)item).IsHit = hitItems.Contains(item);
                    }
                    else if (item is StoreTreeViewModel)
                    {
                        ((StoreTreeViewModel)item).IsHit = hitItems.Contains(item);
                    }
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

                if (destinationItem is StoreTreeViewModel)
                {
                    var d = (StoreTreeViewModel)destinationItem;

                    foreach (string filePath in paths)
                    {
                        using (FileStream stream = new FileStream(filePath, FileMode.Open))
                        {
                            try
                            {
                                var box = AmoebaConverter.FromBoxStream(stream);
                                if (box == null) continue;

                                if (!StoreUploadControl.CheckBoxDigitalSignature(ref box))
                                {
                                    if (MessageBox.Show(
                                        _mainWindow,
                                        string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.StoreUploadControl_DigitalSignatureError_Message),
                                        "Library",
                                        MessageBoxButton.OKCancel,
                                        MessageBoxImage.Asterisk) == MessageBoxResult.OK)
                                    {
                                        d.Value.Boxes.Add(box);
                                        d.Value.IsUpdated = true;
                                    }
                                }
                                else
                                {
                                    d.Value.Boxes.Add(box);
                                    d.Value.IsUpdated = true;
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }

                    d.Update();
                }
                else if (destinationItem is BoxTreeViewModel)
                {
                    var d = (BoxTreeViewModel)destinationItem;

                    if (!this.DigitalSignatureRelease(d.GetAncestors().OfType<BoxTreeViewModel>())) return;

                    foreach (string filePath in paths)
                    {
                        using (FileStream stream = new FileStream(filePath, FileMode.Open))
                        {
                            try
                            {
                                var box = AmoebaConverter.FromBoxStream(stream);
                                if (box == null) continue;

                                if (!StoreUploadControl.CheckBoxDigitalSignature(ref box))
                                {
                                    if (MessageBox.Show(
                                        _mainWindow,
                                        string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.StoreUploadControl_DigitalSignatureError_Message),
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
                if (e.Data.GetDataPresent("TreeViewModel"))
                {
                    var sourceItem = (TreeViewModelBase)e.Data.GetData("TreeViewModel");

                    if (sourceItem is StoreCategorizeTreeViewModel)
                    {
                        var destinationItem = this.GetDropDestination(e.GetPosition, (UIElement)e.OriginalSource);

                        if (destinationItem is StoreCategorizeTreeViewModel)
                        {
                            var s = (StoreCategorizeTreeViewModel)sourceItem;
                            var d = (StoreCategorizeTreeViewModel)destinationItem;

                            if (d.Value.Children.Any(n => object.ReferenceEquals(n, s.Value))) return;
                            if (d.GetAncestors().Any(n => object.ReferenceEquals(n, s))) return;

                            var parentItem = s.Parent;

                            if (parentItem is StoreCategorizeTreeViewModel)
                            {
                                var p = (StoreCategorizeTreeViewModel)parentItem;

                                var tItems = p.Value.Children.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                                p.Value.Children.Clear();
                                p.Value.Children.AddRange(tItems);

                                p.Update();
                            }

                            d.Value.Children.Add(s.Value);
                            d.IsSelected = true;

                            d.Update();
                        }
                    }
                    else if (sourceItem is StoreTreeViewModel)
                    {
                        var destinationItem = this.GetDropDestination(e.GetPosition, (UIElement)e.OriginalSource);

                        if (destinationItem is StoreCategorizeTreeViewModel)
                        {
                            var s = (StoreTreeViewModel)sourceItem;
                            var d = (StoreCategorizeTreeViewModel)destinationItem;

                            if (d.Value.StoreTreeItems.Any(n => object.ReferenceEquals(n, s.Value))) return;

                            var parentItem = s.Parent;

                            if (parentItem is StoreCategorizeTreeViewModel)
                            {
                                var p = (StoreCategorizeTreeViewModel)parentItem;

                                var tItems = p.Value.StoreTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                                p.Value.StoreTreeItems.Clear();
                                p.Value.StoreTreeItems.AddRange(tItems);

                                p.Update();
                            }

                            d.Value.StoreTreeItems.Add(s.Value);
                            d.IsSelected = true;

                            d.Update();
                        }
                    }
                    else if (sourceItem is BoxTreeViewModel)
                    {
                        var destinationItem = this.GetDropDestination(e.GetPosition, (UIElement)e.OriginalSource);

                        if (destinationItem is StoreTreeViewModel)
                        {
                            var s = (BoxTreeViewModel)sourceItem;
                            var d = (StoreTreeViewModel)destinationItem;

                            if (d.Value.Boxes.Any(n => object.ReferenceEquals(n, s.Value))) return;
                            if (d.GetAncestors().Any(n => object.ReferenceEquals(n, s))) return;

                            if (!this.DigitalSignatureRelease(s.GetAncestors().OfType<BoxTreeViewModel>().Where(n => n != s))) return;

                            var parentItem = s.Parent;

                            if (parentItem is StoreTreeViewModel)
                            {
                                var p = (StoreTreeViewModel)parentItem;

                                var tItems = p.Value.Boxes.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                                p.Value.Boxes.Clear();
                                p.Value.Boxes.AddRange(tItems);
                                p.Value.IsUpdated = true;

                                p.Update();
                            }
                            else if (parentItem is BoxTreeViewModel)
                            {
                                var p = (BoxTreeViewModel)parentItem;

                                var tItems = p.Value.Boxes.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                                p.Value.Boxes.Clear();
                                p.Value.Boxes.AddRange(tItems);
                                p.Value.CreationTime = DateTime.UtcNow;

                                p.Update();
                            }

                            d.Value.Boxes.Add(s.Value);
                            d.Value.IsUpdated = true;
                            d.IsSelected = true;

                            d.Update();
                        }
                        else if (destinationItem is BoxTreeViewModel)
                        {
                            var s = (BoxTreeViewModel)sourceItem;
                            var d = (BoxTreeViewModel)destinationItem;

                            if (d.Value.Boxes.Any(n => object.ReferenceEquals(n, s.Value))) return;
                            if (d.GetAncestors().Any(n => object.ReferenceEquals(n, s))) return;

                            if (!this.DigitalSignatureRelease(s.GetAncestors().OfType<BoxTreeViewModel>().Where(n => n != s))) return;
                            if (!this.DigitalSignatureRelease(d.GetAncestors().OfType<BoxTreeViewModel>())) return;

                            var parentItem = s.Parent;

                            if (parentItem is StoreTreeViewModel)
                            {
                                var p = (StoreTreeViewModel)parentItem;

                                var tItems = p.Value.Boxes.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                                p.Value.Boxes.Clear();
                                p.Value.Boxes.AddRange(tItems);
                                p.Value.IsUpdated = true;

                                p.Update();
                            }
                            else if (parentItem is BoxTreeViewModel)
                            {
                                var p = (BoxTreeViewModel)parentItem;

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
                else if (e.Data.GetDataPresent("ListViewModels"))
                {
                    var boxes = ((IList)e.Data.GetData("ListViewModels")).OfType<BoxListViewModel>().Select(n => n.Value).ToList();
                    var seeds = ((IList)e.Data.GetData("ListViewModels")).OfType<SeedListViewModel>().Select(n => n.Value).ToList();
                    bool isListView = _listView.GetCurrentIndex(e.GetPosition) != -1;

                    var parentItem = (TreeViewModelBase)_treeView.SelectedItem;

                    if (parentItem is StoreTreeViewModel)
                    {
                        var destinationItem = this.GetDropDestination(e.GetPosition, (UIElement)e.OriginalSource);

                        if (destinationItem is StoreTreeViewModel)
                        {
                            var p = (StoreTreeViewModel)parentItem;
                            var d = (StoreTreeViewModel)destinationItem;

                            boxes = boxes.Where(n => !object.ReferenceEquals(n, d.Value)).ToList();

                            if (boxes.Count == 0) return;

                            var tboxes = p.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                            p.Value.Boxes.Clear();
                            p.Value.Boxes.AddRange(tboxes);
                            p.Value.IsUpdated = true;

                            p.Update();

                            d.Value.Boxes.AddRange(boxes);
                            d.Value.IsUpdated = true;
                            if (!isListView) d.IsSelected = true;

                            d.Update();
                        }
                        else if (destinationItem is BoxTreeViewModel)
                        {
                            var p = (StoreTreeViewModel)parentItem;
                            var d = (BoxTreeViewModel)destinationItem;

                            boxes = boxes.Where(n => !object.ReferenceEquals(n, d.Value)).ToList();

                            if (boxes.Count == 0) return;

                            if (!this.DigitalSignatureRelease(d.GetAncestors().OfType<BoxTreeViewModel>())) return;

                            var tboxes = p.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                            p.Value.Boxes.Clear();
                            p.Value.Boxes.AddRange(tboxes);
                            p.Value.IsUpdated = true;

                            p.Update();

                            d.Value.Boxes.AddRange(boxes);
                            d.Value.CreationTime = DateTime.UtcNow;
                            if (!isListView) d.IsSelected = true;

                            d.Update();
                        }
                    }
                    else if (parentItem is BoxTreeViewModel)
                    {
                        var destinationItem = this.GetDropDestination(e.GetPosition, (UIElement)e.OriginalSource);

                        if (destinationItem is StoreTreeViewModel)
                        {
                            var p = (BoxTreeViewModel)parentItem;
                            var d = (StoreTreeViewModel)destinationItem;

                            boxes = boxes.Where(n => !object.ReferenceEquals(n, d.Value)).ToList();

                            if (boxes.Count == 0) return;

                            if (!this.DigitalSignatureRelease(p.GetAncestors().OfType<BoxTreeViewModel>())) return;

                            var tboxes = p.Value.Boxes.Where(n => !boxes.Any(m => object.ReferenceEquals(n, m))).ToArray();
                            p.Value.Boxes.Clear();
                            p.Value.Boxes.AddRange(tboxes);
                            p.Value.CreationTime = DateTime.UtcNow;

                            p.Update();

                            d.Value.Boxes.AddRange(boxes);
                            d.Value.IsUpdated = true;
                            if (!isListView) d.IsSelected = true;

                            d.Update();
                        }
                        else if (destinationItem is BoxTreeViewModel)
                        {
                            var p = (BoxTreeViewModel)parentItem;
                            var d = (BoxTreeViewModel)destinationItem;

                            if (p == d) return;

                            boxes = boxes.Where(n => !object.ReferenceEquals(n, d.Value)).ToList();

                            if (boxes.Count == 0 && seeds.Count == 0) return;

                            if (!this.DigitalSignatureRelease(p.GetAncestors().OfType<BoxTreeViewModel>())) return;
                            if (!this.DigitalSignatureRelease(d.GetAncestors().OfType<BoxTreeViewModel>())) return;

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

        private TreeViewModelBase GetDropDestination(GetPositionDelegate getPosition, UIElement originalSource)
        {
            var posithonIndex = _listView.GetCurrentIndex(getPosition);

            if (posithonIndex != -1)
            {
                var selectItem = _treeView.SelectedItem;

                if (selectItem is StoreTreeViewModel)
                {
                    var listViewModel = _listView.Items[posithonIndex];

                    if (listViewModel is BoxListViewModel)
                    {
                        var selectTreeViewModel = (StoreTreeViewModel)selectItem;
                        var boxListViewModel = (BoxListViewModel)listViewModel;

                        return selectTreeViewModel.Children.OfType<BoxTreeViewModel>().FirstOrDefault(n => object.ReferenceEquals(n.Value, boxListViewModel.Value));
                    }
                }
                else if (selectItem is BoxTreeViewModel)
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

                if (selectItem is StoreTreeViewModel)
                {
                    var listViewModel = _listView.Items[selectIndex];

                    if (listViewModel is BoxListViewModel)
                    {
                        var selectTreeViewModel = (StoreTreeViewModel)selectItem;
                        var boxListViewModel = (BoxListViewModel)listViewModel;

                        return selectTreeViewModel.Children.OfType<BoxTreeViewModel>().FirstOrDefault(n => object.ReferenceEquals(n.Value, boxListViewModel.Value));
                    }
                }
                else if (selectItem is BoxTreeViewModel)
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

        private void _treeView_Expanded(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            var treeViewModel = (TreeViewModelBase)_treeView.SearchItemFromElement((DependencyObject)treeViewItem);

            var path = new Route();

            foreach (var item in treeViewModel.GetAncestors())
            {
                if (item is StoreCategorizeTreeViewModel) path.Add(((StoreCategorizeTreeViewModel)item).Value.Name);
                else if (item is StoreTreeViewModel) path.Add(((StoreTreeViewModel)item).Value.Signature);
                else if (item is BoxTreeViewModel) path.Add(((BoxTreeViewModel)item).Value.Name);
            }

            Settings.Instance.StoreUploadControl_ExpandedPaths.Add(path);
        }

        private void _treeView_Collapsed(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            var treeViewModel = (TreeViewModelBase)_treeView.SearchItemFromElement((DependencyObject)treeViewItem);

            var path = new Route();

            foreach (var item in treeViewModel.GetAncestors())
            {
                if (item is StoreCategorizeTreeViewModel) path.Add(((StoreCategorizeTreeViewModel)item).Value.Name);
                else if (item is StoreTreeViewModel) path.Add(((StoreTreeViewModel)item).Value.Signature);
                else if (item is BoxTreeViewModel) path.Add(((BoxTreeViewModel)item).Value.Name);
            }

            Settings.Instance.StoreUploadControl_ExpandedPaths.Remove(path);
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
            var element = ((UIElement)e.OriginalSource).FindAncestor<TreeViewItem>();
            if (element == null) return;

            var item = _treeView.SearchItemFromElement(element) as TreeViewModelBase;
            if (item == null)
            {
                _startPoint = new Point(-1, -1);

                return;
            }

            Point lposition = e.GetPosition(_treeView);

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
            var treeViewItem = sender as TreeViewItem;
            if (treeViewItem == null) return;

            var treeViewModel = (StoreCategorizeTreeViewModel)_treeView.SearchItemFromElement((DependencyObject)treeViewItem);
            if (_treeView.SelectedItem != treeViewModel) return;

            var contextMenu = treeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            _startPoint = new Point(-1, -1);

            MenuItem storeCategorizeTreeViewItemDeleteMenuItem = contextMenu.GetMenuItem("_storeCategorizeTreeViewItemDeleteMenuItem");
            MenuItem storeCategorizeTreeViewItemCutMenuItem = contextMenu.GetMenuItem("_storeCategorizeTreeViewItemCutMenuItem");
            MenuItem storeCategorizeTreeViewItemPasteMenuItem = contextMenu.GetMenuItem("_storeCategorizeTreeViewItemPasteMenuItem");
            MenuItem storeCategorizeTreeViewItemUploadMenuItem = contextMenu.GetMenuItem("_storeCategorizeTreeViewItemUploadMenuItem");

            storeCategorizeTreeViewItemDeleteMenuItem.IsEnabled = (_treeViewModel != treeViewModel);
            storeCategorizeTreeViewItemCutMenuItem.IsEnabled = (_treeViewModel != treeViewModel);
            storeCategorizeTreeViewItemPasteMenuItem.IsEnabled = Clipboard.ContainsStoreCategorizeTreeItems() || Clipboard.ContainsStoreTreeItems();

            {
                var storeTreeItems = new List<StoreTreeItem>();

                {
                    var storeCategorizeTreeItems = new List<StoreCategorizeTreeItem>();
                    storeCategorizeTreeItems.Add(treeViewModel.Value);

                    for (int i = 0; i < storeCategorizeTreeItems.Count; i++)
                    {
                        storeCategorizeTreeItems.AddRange(storeCategorizeTreeItems[i].Children);
                        storeTreeItems.AddRange(storeCategorizeTreeItems[i].StoreTreeItems);
                    }
                }

                storeCategorizeTreeViewItemUploadMenuItem.IsEnabled = storeTreeItems.Count != 0;
            }
        }

        private void _storeCategorizeTreeViewItemNewStoreMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            var window = new SignatureWindow();
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewModel.Value.StoreTreeItems.Add(new StoreTreeItem() { Signature = window.DigitalSignature.ToString() });

                selectTreeViewModel.Update();
            }

            this.Update();
        }

        private void _storeCategorizeTreeViewItemNewCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            string name;

            if (!selectTreeViewModel.Value.Children.Any(n => n.Name == "New category"))
            {
                name = "New category";
            }
            else
            {
                int i = 1;
                while (selectTreeViewModel.Value.Children.Any(n => n.Name == "New category_" + i)) i++;

                name = "New category_" + i;
            }

            var window = new NameWindow(name);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewModel.Value.Children.Add(new StoreCategorizeTreeItem() { Name = window.Text });

                selectTreeViewModel.Update();
            }

            this.Update();
        }

        private void _storeCategorizeTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            var window = new NameWindow(selectTreeViewModel.Value.Name);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewModel.Value.Name = window.Text;
                selectTreeViewModel.Update();
            }

            this.Update();
        }

        private void _storeCategorizeTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreCategorizeTreeViewModel;
            if (selectTreeViewModel == null || _treeView.SelectedItem != selectTreeViewModel) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parentItem = selectTreeViewModel.Parent;

            if (parentItem is StoreCategorizeTreeViewModel)
            {
                var p = (StoreCategorizeTreeViewModel)parentItem;

                p.Value.Children.Remove(selectTreeViewModel.Value);

                p.Update();
            }

            this.Update();
        }

        private void _storeCategorizeTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreCategorizeTreeViewModel;
            if (selectTreeViewModel == null || _treeView.SelectedItem != selectTreeViewModel) return;

            Clipboard.SetStoreCategorizeTreeItems(new StoreCategorizeTreeItem[] { selectTreeViewModel.Value });

            var parentItem = selectTreeViewModel.Parent;

            if (parentItem is StoreCategorizeTreeViewModel)
            {
                var p = (StoreCategorizeTreeViewModel)parentItem;

                p.Value.Children.Remove(selectTreeViewModel.Value);

                p.Update();
            }

            this.Update();
        }

        private void _storeCategorizeTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreCategorizeTreeViewModel;
            if (selectTreeViewModel == null || _treeView.SelectedItem != selectTreeViewModel) return;

            Clipboard.SetStoreCategorizeTreeItems(new StoreCategorizeTreeItem[] { selectTreeViewModel.Value });
        }

        private void _storeCategorizeTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            selectTreeViewModel.Value.Children.AddRange(Clipboard.GetStoreCategorizeTreeItems());
            selectTreeViewModel.Value.StoreTreeItems.AddRange(Clipboard.GetStoreTreeItems());

            selectTreeViewModel.Update();

            this.Update();
        }

        private void _storeCategorizeTreeViewItemUploadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            var storeTreeViewModels = new List<StoreTreeViewModel>();

            {
                var storeCategorizeTreeViewModels = new List<StoreCategorizeTreeViewModel>();
                storeCategorizeTreeViewModels.Add(_treeViewModel);

                for (int i = 0; i < storeCategorizeTreeViewModels.Count; i++)
                {
                    storeCategorizeTreeViewModels.AddRange(storeCategorizeTreeViewModels[i].Children.OfType<StoreCategorizeTreeViewModel>());
                    storeTreeViewModels.AddRange(storeCategorizeTreeViewModels[i].Children.OfType<StoreTreeViewModel>());
                }
            }

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Upload_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            foreach (var storeTreeViewModel in storeTreeViewModels)
            {
                storeTreeViewModel.Value.IsUpdated = false;
                storeTreeViewModel.Update();

                var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == storeTreeViewModel.Value.Signature);
                if (digitalSignature == null) return;

                var store = new Store();
                store.Boxes.AddRange(storeTreeViewModel.Value.Boxes);

                _amoebaManager.Upload(store.Clone(), digitalSignature);
            }

            this.Update();
        }

        private void _storeTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var treeViewItem = sender as TreeViewItem;
            if (treeViewItem == null) return;

            var treeViewModel = (StoreTreeViewModel)_treeView.SearchItemFromElement((DependencyObject)treeViewItem);
            if (_treeView.SelectedItem != treeViewModel) return;

            var contextMenu = treeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            _startPoint = new Point(-1, -1);

            MenuItem storeTreeViewItemPasteMenuItem = contextMenu.GetMenuItem("_storeTreeViewItemPasteMenuItem");

            storeTreeViewItemPasteMenuItem.IsEnabled = Clipboard.ContainsBoxes();
        }

        private void _storeTreeViewItemNewBoxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreTreeViewModel;
            if (selectTreeViewModel == null) return;

            Box box;

            if (!selectTreeViewModel.Value.Boxes.Any(n => n.Name == "New box"))
            {
                box = new Box() { Name = "New box", CreationTime = DateTime.UtcNow };
            }
            else
            {
                int i = 1;
                while (selectTreeViewModel.Value.Boxes.Any(n => n.Name == "New box_" + i)) i++;

                box = new Box() { Name = "New box_" + i, CreationTime = DateTime.UtcNow };
            }

            var window = new BoxEditWindow(box);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewModel.Value.Boxes.Add(box);
                selectTreeViewModel.Value.IsUpdated = true;

                selectTreeViewModel.Update();
            }

            this.Update();
        }

        private void _storeTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreTreeViewModel;
            if (selectTreeViewModel == null) return;

            var window = new SignatureWindow(selectTreeViewModel.Value.Signature);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewModel.Value.Signature = window.DigitalSignature.ToString();
                selectTreeViewModel.Value.IsUpdated = true;

                selectTreeViewModel.Update();
            }

            this.Update();
        }

        private void _storeTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreTreeViewModel;
            if (selectTreeViewModel == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parentItem = selectTreeViewModel.Parent;

            if (parentItem is StoreCategorizeTreeViewModel)
            {
                var p = (StoreCategorizeTreeViewModel)parentItem;

                p.Value.StoreTreeItems.Remove(selectTreeViewModel.Value);

                p.Update();
            }

            this.Update();
        }

        private void _storeTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreTreeViewModel;
            if (selectTreeViewModel == null) return;

            Clipboard.SetStoreTreeItems(new StoreTreeItem[] { selectTreeViewModel.Value });

            var parentItem = selectTreeViewModel.Parent;

            if (parentItem is StoreCategorizeTreeViewModel)
            {
                var p = (StoreCategorizeTreeViewModel)parentItem;

                p.Value.StoreTreeItems.Remove(selectTreeViewModel.Value);

                p.Update();
            }

            this.Update();
        }

        private void _storeTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreTreeViewModel;
            if (selectTreeViewModel == null) return;

            Clipboard.SetStoreTreeItems(new StoreTreeItem[] { selectTreeViewModel.Value });
        }

        private void _storeTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreTreeViewModel;
            if (selectTreeViewModel == null) return;

            selectTreeViewModel.Value.Boxes.AddRange(Clipboard.GetBoxes());
            selectTreeViewModel.Value.IsUpdated = true;

            selectTreeViewModel.Update();

            this.Update();
        }

        private void _storeTreeViewItemImportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreTreeViewModel;
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

                                if (!StoreUploadControl.CheckBoxDigitalSignature(ref box))
                                {
                                    if (MessageBox.Show(
                                            _mainWindow,
                                            string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message),
                                            "Store",
                                            MessageBoxButton.OKCancel,
                                            MessageBoxImage.Asterisk) == MessageBoxResult.OK)
                                    {
                                        selectTreeViewModel.Value.Boxes.Add(box);
                                    }
                                }
                                else
                                {
                                    selectTreeViewModel.Value.Boxes.Add(box);
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }

                    selectTreeViewModel.Value.IsUpdated = true;

                    selectTreeViewModel.Update();

                    this.Update();
                }
            }
        }

        private void _storeTreeViewItemExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreTreeViewModel;
            if (selectTreeViewModel == null) return;

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.RestoreDirectory = true;
                dialog.FileName = "Store - " + Signature.GetNickname(selectTreeViewModel.Value.Signature);
                dialog.DefaultExt = ".box";
                dialog.Filter = "Box (*.box)|*.box";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileName = dialog.FileName;

                    var box = new Box();
                    box.Name = "Store - " + Signature.GetNickname(selectTreeViewModel.Value.Signature);
                    box.Boxes.AddRange(selectTreeViewModel.Value.Boxes);
                    box.CreationTime = DateTime.UtcNow;

                    using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                    using (Stream boxStream = AmoebaConverter.ToBoxStream(box))
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

        private void _storeTreeViewItemUploadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as StoreTreeViewModel;
            if (selectTreeViewModel == null) return;

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == selectTreeViewModel.Value.Signature);
            if (digitalSignature == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Upload_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            selectTreeViewModel.Value.IsUpdated = false;
            selectTreeViewModel.Update();

            var store = new Store();
            store.Boxes.AddRange(selectTreeViewModel.Value.Boxes);

            _amoebaManager.Upload(store.Clone(), digitalSignature);

            this.Update();
        }

        private void _boxTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var treeViewItem = sender as TreeViewItem;
            if (treeViewItem == null) return;

            var treeViewModel = (BoxTreeViewModel)_treeView.SearchItemFromElement((DependencyObject)treeViewItem);
            if (_treeView.SelectedItem != treeViewModel) return;

            var contextMenu = treeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            _startPoint = new Point(-1, -1);

            MenuItem boxTreeViewItemPasteMenuItem = contextMenu.GetMenuItem("_boxTreeViewItemPasteMenuItem");

            boxTreeViewItemPasteMenuItem.IsEnabled = Clipboard.ContainsBoxes() || Clipboard.ContainsSeeds();
        }

        private void _boxTreeViewItemNewBoxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null) return;

            var storeTreeViewModel = selectTreeViewModel.GetAncestors().OfType<StoreTreeViewModel>().FirstOrDefault();
            if (storeTreeViewModel == null) return;

            if (!this.DigitalSignatureRelease(selectTreeViewModel.GetAncestors().OfType<BoxTreeViewModel>())) return;

            Box box;

            if (!selectTreeViewModel.Value.Boxes.Any(n => n.Name == "New box"))
            {
                box = new Box() { Name = "New box", CreationTime = DateTime.UtcNow };
            }
            else
            {
                int i = 1;
                while (selectTreeViewModel.Value.Boxes.Any(n => n.Name == "New box_" + i)) i++;

                box = new Box() { Name = "New box_" + i, CreationTime = DateTime.UtcNow };
            }

            var window = new BoxEditWindow(box);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewModel.Value.Boxes.Add(box);
                selectTreeViewModel.Value.CreationTime = DateTime.UtcNow;

                selectTreeViewModel.Update();

                storeTreeViewModel.Value.IsUpdated = true;

                storeTreeViewModel.Update();
            }

            this.Update();
        }

        private void _boxTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null) return;

            var storeTreeViewModel = selectTreeViewModel.GetAncestors().OfType<StoreTreeViewModel>().FirstOrDefault();
            if (storeTreeViewModel == null) return;

            if (!this.DigitalSignatureRelease(selectTreeViewModel.GetAncestors().Where(n => n != selectTreeViewModel).OfType<BoxTreeViewModel>())) return;

            var box = selectTreeViewModel.Value;

            var window = new BoxEditWindow(box);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewModel.Value.CreationTime = DateTime.UtcNow;

                selectTreeViewModel.Update();

                storeTreeViewModel.Value.IsUpdated = true;

                storeTreeViewModel.Update();
            }

            this.Update();
        }

        private void _boxTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null) return;

            var storeTreeViewModel = selectTreeViewModel.GetAncestors().OfType<StoreTreeViewModel>().FirstOrDefault();
            if (storeTreeViewModel == null) return;

            if (!this.DigitalSignatureRelease(selectTreeViewModel.GetAncestors().Where(n => n != selectTreeViewModel).OfType<BoxTreeViewModel>())) return;
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parentItem = selectTreeViewModel.Parent;

            if (parentItem is StoreTreeViewModel)
            {
                var p = (StoreTreeViewModel)parentItem;

                p.Value.Boxes.Remove(selectTreeViewModel.Value);
                p.Value.IsUpdated = true;

                p.Update();
            }
            else if (parentItem is BoxTreeViewModel)
            {
                var p = (BoxTreeViewModel)parentItem;

                p.Value.Boxes.Remove(selectTreeViewModel.Value);
                p.Value.CreationTime = DateTime.UtcNow;

                p.Update();
            }

            storeTreeViewModel.Value.IsUpdated = true;

            storeTreeViewModel.Update();

            this.Update();
        }

        private void _boxTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null) return;

            var storeTreeViewModel = selectTreeViewModel.GetAncestors().OfType<StoreTreeViewModel>().FirstOrDefault();
            if (storeTreeViewModel == null) return;

            if (!this.DigitalSignatureRelease(selectTreeViewModel.GetAncestors().Where(n => n != selectTreeViewModel).OfType<BoxTreeViewModel>())) return;

            Clipboard.SetBoxes(new Box[] { selectTreeViewModel.Value });

            var parentItem = selectTreeViewModel.Parent;

            if (parentItem is StoreTreeViewModel)
            {
                var p = (StoreTreeViewModel)parentItem;

                p.Value.Boxes.Remove(selectTreeViewModel.Value);
                p.Value.IsUpdated = true;

                p.Update();
            }
            else if (parentItem is BoxTreeViewModel)
            {
                var p = (BoxTreeViewModel)parentItem;

                p.Value.Boxes.Remove(selectTreeViewModel.Value);
                p.Value.CreationTime = DateTime.UtcNow;

                p.Update();
            }

            storeTreeViewModel.Value.IsUpdated = true;

            storeTreeViewModel.Update();

            this.Update();
        }

        private void _boxTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null) return;

            Clipboard.SetBoxes(new Box[] { selectTreeViewModel.Value });
        }

        private void _boxTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null) return;

            var storeTreeViewModel = selectTreeViewModel.GetAncestors().OfType<StoreTreeViewModel>().FirstOrDefault();
            if (storeTreeViewModel == null) return;

            if (!this.DigitalSignatureRelease(selectTreeViewModel.GetAncestors().OfType<BoxTreeViewModel>())) return;

            selectTreeViewModel.Value.Boxes.AddRange(Clipboard.GetBoxes());
            selectTreeViewModel.Value.Seeds.AddRange(Clipboard.GetSeeds());
            selectTreeViewModel.Value.CreationTime = DateTime.UtcNow;

            selectTreeViewModel.Update();

            storeTreeViewModel.Value.IsUpdated = true;

            storeTreeViewModel.Update();

            this.Update();
        }

        private void _boxTreeViewItemImportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as BoxTreeViewModel;
            if (selectTreeViewModel == null) return;

            var storeTreeViewModel = selectTreeViewModel.GetAncestors().OfType<StoreTreeViewModel>().FirstOrDefault();
            if (storeTreeViewModel == null) return;

            if (!this.DigitalSignatureRelease(selectTreeViewModel.GetAncestors().OfType<BoxTreeViewModel>())) return;

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

                                if (!StoreUploadControl.CheckBoxDigitalSignature(ref box))
                                {
                                    if (MessageBox.Show(
                                            _mainWindow,
                                            string.Format("\"{0}\"\r\n\r\n{1}", box.Name, LanguagesManager.Instance.LibraryControl_DigitalSignatureError_Message),
                                            "Store",
                                            MessageBoxButton.OKCancel,
                                            MessageBoxImage.Asterisk) == MessageBoxResult.OK)
                                    {
                                        selectTreeViewModel.Value.Boxes.Add(box);
                                    }
                                }
                                else
                                {
                                    selectTreeViewModel.Value.Boxes.Add(box);
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }

                    selectTreeViewModel.Value.CreationTime = DateTime.UtcNow;
                    selectTreeViewModel.Update();

                    storeTreeViewModel.Value.IsUpdated = true;

                    storeTreeViewModel.Update();
                    this.Update();
                }
            }
        }

        private void _boxTreeViewItemExportMenuItem_Click(object sender, RoutedEventArgs e)
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

        private void _listView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var posithonIndex = _listView.GetCurrentIndex(e.GetPosition);

            if (posithonIndex != -1)
            {
                var selectItem = _treeView.SelectedItem;

                if (selectItem is StoreTreeViewModel)
                {
                    var listViewModel = _listView.Items[posithonIndex];

                    if (listViewModel is BoxListViewModel)
                    {
                        var selectTreeViewModel = (StoreTreeViewModel)selectItem;
                        var boxListViewModel = (BoxListViewModel)listViewModel;

                        var item = selectTreeViewModel.Children.OfType<BoxTreeViewModel>().FirstOrDefault(n => object.ReferenceEquals(n.Value, boxListViewModel.Value));

                        {
                            selectTreeViewModel.IsExpanded = true;
                            item.IsSelected = true;
                        }
                    }
                }
                else if (selectItem is BoxTreeViewModel)
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
                                if (item is StoreCategorizeTreeViewModel) path.Add(((StoreCategorizeTreeViewModel)item).Value.Name);
                                else if (item is StoreTreeViewModel) path.Add(Signature.GetNickname(((StoreTreeViewModel)item).Value.Signature));
                                else if (item is BoxTreeViewModel) path.Add(((BoxTreeViewModel)item).Value.Name);
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

            if (_refreshing || (_treeView.SelectedItem == null || _treeView.SelectedItem is StoreCategorizeTreeViewModel))
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

                    if (destinationItem is StoreTreeViewModel)
                    {
                        _listViewPasteMenuItem.IsEnabled = Clipboard.ContainsBoxes();
                    }
                    else if (destinationItem is BoxTreeViewModel)
                    {
                        _listViewPasteMenuItem.IsEnabled = Clipboard.ContainsBoxes() || Clipboard.ContainsSeeds();
                    }
                }
            }
        }

        private void _listViewNewBoxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var destinationItem = this.GetSelectedItem();

            if (destinationItem is StoreTreeViewModel)
            {
                var d = (StoreTreeViewModel)destinationItem;

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

                var window = new BoxEditWindow(box);
                window.Owner = _mainWindow;

                if (window.ShowDialog() == true)
                {
                    d.Value.Boxes.Add(box);
                    d.Value.IsUpdated = true;

                    d.Update();
                }
            }
            else if (destinationItem is BoxTreeViewModel)
            {
                var d = (BoxTreeViewModel)destinationItem;

                var storeTreeViewModel = d.GetAncestors().OfType<StoreTreeViewModel>().FirstOrDefault();
                if (storeTreeViewModel == null) return;

                if (!this.DigitalSignatureRelease(d.GetAncestors().OfType<BoxTreeViewModel>())) return;

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

                var window = new BoxEditWindow(box);
                window.Owner = _mainWindow;

                if (window.ShowDialog() == true)
                {
                    d.Value.Boxes.Add(box);
                    d.Value.CreationTime = DateTime.UtcNow;

                    d.Update();

                    storeTreeViewModel.Value.IsUpdated = true;

                    storeTreeViewModel.Update();
                }
            }

            this.Update();
        }

        private void _listViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as TreeViewModelBase;
            if (selectTreeViewModel == null) return;

            if (selectTreeViewModel is StoreTreeViewModel)
            {
                var selectStoreTreeViewModel = (StoreTreeViewModel)selectTreeViewModel;

                if (_listView.SelectedItem is BoxListViewModel)
                {
                    var selectBoxListViewModels = _listView.SelectedItems.OfType<BoxListViewModel>();
                    if (selectBoxListViewModels == null) return;

                    var editBoxs = selectBoxListViewModels.Select(n => n.Value.Clone()).ToList();
                    if (editBoxs == null) return;

                    var window = new BoxEditWindow(editBoxs);
                    window.Owner = _mainWindow;

                    if (window.ShowDialog() == true)
                    {
                        foreach (var item in selectBoxListViewModels)
                        {
                            selectStoreTreeViewModel.Value.Boxes.Remove(item.Value);
                        }

                        foreach (var seed in editBoxs)
                        {
                            selectStoreTreeViewModel.Value.Boxes.Add(seed);
                        }

                        selectStoreTreeViewModel.Value.IsUpdated = true;

                        selectStoreTreeViewModel.Update();
                    }
                }
            }
            else if (selectTreeViewModel is BoxTreeViewModel)
            {
                var selectBoxTreeViewModel = (BoxTreeViewModel)selectTreeViewModel;

                if (_listView.SelectedItem is BoxListViewModel)
                {
                    var selectBoxListViewModels = _listView.SelectedItems.OfType<BoxListViewModel>();
                    if (selectBoxListViewModels == null) return;

                    var storeTreeViewModel = selectTreeViewModel.GetAncestors().OfType<StoreTreeViewModel>().FirstOrDefault();
                    if (storeTreeViewModel == null) return;

                    if (!this.DigitalSignatureRelease(selectBoxTreeViewModel.GetAncestors().OfType<BoxTreeViewModel>())) return;

                    var editBoxs = selectBoxListViewModels.Select(n => n.Value.Clone()).ToList();
                    if (editBoxs == null) return;

                    var window = new BoxEditWindow(editBoxs);
                    window.Owner = _mainWindow;

                    if (window.ShowDialog() == true)
                    {
                        foreach (var item in selectBoxListViewModels)
                        {
                            selectBoxTreeViewModel.Value.Boxes.Remove(item.Value);
                        }

                        foreach (var seed in editBoxs)
                        {
                            selectBoxTreeViewModel.Value.Boxes.Add(seed);
                        }

                        selectBoxTreeViewModel.Value.CreationTime = DateTime.UtcNow;

                        selectBoxTreeViewModel.Update();

                        storeTreeViewModel.Value.IsUpdated = true;

                        storeTreeViewModel.Update();
                    }
                }
                else if (_listView.SelectedItem is SeedListViewModel)
                {
                    var selectSeedListViewModels = _listView.SelectedItems.OfType<SeedListViewModel>();
                    if (selectSeedListViewModels == null) return;

                    var storeTreeViewModel = selectTreeViewModel.GetAncestors().OfType<StoreTreeViewModel>().FirstOrDefault();
                    if (storeTreeViewModel == null) return;

                    if (!this.DigitalSignatureRelease(selectBoxTreeViewModel.GetAncestors().OfType<BoxTreeViewModel>())) return;

                    var editSeeds = selectSeedListViewModels.Select(n => n.Value.Clone()).ToList();
                    if (editSeeds == null) return;

                    var window = new SeedEditWindow(editSeeds);
                    window.Owner = _mainWindow;

                    if (window.ShowDialog() == true)
                    {
                        foreach (var item in selectSeedListViewModels)
                        {
                            selectBoxTreeViewModel.Value.Seeds.Remove(item.Value);
                        }

                        foreach (var seed in editSeeds)
                        {
                            selectBoxTreeViewModel.Value.Seeds.Add(seed);
                        }

                        selectBoxTreeViewModel.Value.CreationTime = DateTime.UtcNow;

                        selectBoxTreeViewModel.Update();

                        storeTreeViewModel.Value.IsUpdated = true;

                        storeTreeViewModel.Update();
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

            if (selectTreeViewModel is StoreTreeViewModel)
            {
                var selectStoreTreeViewModel = (StoreTreeViewModel)selectTreeViewModel;

                if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Store", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

                var boxes = _listView.SelectedItems.OfType<BoxListViewModel>().Select(n => n.Value);

                foreach (var item in boxes)
                {
                    selectStoreTreeViewModel.Value.Boxes.Remove(item);
                }

                selectStoreTreeViewModel.Value.IsUpdated = true;

                selectStoreTreeViewModel.Update();
            }
            else if (selectTreeViewModel is BoxTreeViewModel)
            {
                var selectBoxTreeViewModel = (BoxTreeViewModel)selectTreeViewModel;

                var storeTreeViewModel = selectTreeViewModel.GetAncestors().OfType<StoreTreeViewModel>().FirstOrDefault();
                if (storeTreeViewModel == null) return;

                if (!this.DigitalSignatureRelease(selectBoxTreeViewModel.GetAncestors().OfType<BoxTreeViewModel>())) return;
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

                selectBoxTreeViewModel.Value.CreationTime = DateTime.UtcNow;

                selectBoxTreeViewModel.Update();

                storeTreeViewModel.Value.IsUpdated = true;

                storeTreeViewModel.Update();
            }

            this.Update();
        }

        private void _listViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;

            var selectTreeViewModel = _treeView.SelectedItem as TreeViewModelBase;
            if (selectTreeViewModel == null) return;

            if (selectTreeViewModel is StoreTreeViewModel)
            {
                var selectStoreTreeViewModel = (StoreTreeViewModel)selectTreeViewModel;

                var boxes = _listView.SelectedItems.OfType<BoxListViewModel>().Select(n => n.Value);

                Clipboard.SetBoxes(boxes);

                foreach (var item in boxes)
                {
                    selectStoreTreeViewModel.Value.Boxes.Remove(item);
                }

                selectStoreTreeViewModel.Value.IsUpdated = true;

                selectStoreTreeViewModel.Update();
            }
            else if (selectTreeViewModel is BoxTreeViewModel)
            {
                var selectBoxTreeViewModel = (BoxTreeViewModel)selectTreeViewModel;

                var storeTreeViewModel = selectTreeViewModel.GetAncestors().OfType<StoreTreeViewModel>().FirstOrDefault();
                if (storeTreeViewModel == null) return;

                if (!this.DigitalSignatureRelease(selectBoxTreeViewModel.GetAncestors().OfType<BoxTreeViewModel>())) return;

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

                selectBoxTreeViewModel.Value.CreationTime = DateTime.UtcNow;

                selectBoxTreeViewModel.Update();

                storeTreeViewModel.Value.IsUpdated = true;

                storeTreeViewModel.Update();
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

            if (destinationItem is StoreTreeViewModel)
            {
                var d = (StoreTreeViewModel)destinationItem;

                d.Value.Boxes.AddRange(Clipboard.GetBoxes());
                d.Value.IsUpdated = true;

                d.Update();
            }
            else if (destinationItem is BoxTreeViewModel)
            {
                var d = (BoxTreeViewModel)destinationItem;

                var storeTreeViewModel = d.GetAncestors().OfType<StoreTreeViewModel>().FirstOrDefault();
                if (storeTreeViewModel == null) return;

                if (!this.DigitalSignatureRelease(d.GetAncestors().OfType<BoxTreeViewModel>())) return;

                d.Value.Boxes.AddRange(Clipboard.GetBoxes());
                d.Value.Seeds.AddRange(Clipboard.GetSeeds());
                d.Value.CreationTime = DateTime.UtcNow;

                d.Update();

                storeTreeViewModel.Value.IsUpdated = true;

                storeTreeViewModel.Update();
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
                    if (item is StoreCategorizeTreeViewModel) path.Add(((StoreCategorizeTreeViewModel)item).Value.Name);
                    else if (item is StoreTreeViewModel) path.Add(Signature.GetNickname(((StoreTreeViewModel)item).Value.Signature));
                    else if (item is BoxTreeViewModel) path.Add(((BoxTreeViewModel)item).Value.Name);
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

            if (headerClicked != Settings.Instance.StoreUploadControl_LastHeaderClicked)
            {
                direction = ListSortDirection.Ascending;
            }
            else
            {
                if (Settings.Instance.StoreUploadControl_ListSortDirection == ListSortDirection.Ascending)
                {
                    direction = ListSortDirection.Descending;
                }
                else
                {
                    direction = ListSortDirection.Ascending;
                }
            }

            Settings.Instance.StoreUploadControl_LastHeaderClicked = headerClicked;
            Settings.Instance.StoreUploadControl_ListSortDirection = direction;

            this.Update();
        }

        private IEnumerable<IListViewModel> Sort(IEnumerable<IListViewModel> collection, int maxCount)
        {
            var sortBy = Settings.Instance.StoreUploadControl_LastHeaderClicked;
            var direction = Settings.Instance.StoreUploadControl_ListSortDirection;

            var list = new List<IListViewModel>(collection);

            if (sortBy == LanguagesManager.Instance.StoreUploadControl_Name)
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
            else if (sortBy == LanguagesManager.Instance.StoreUploadControl_Signature)
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
            else if (sortBy == LanguagesManager.Instance.StoreUploadControl_Length)
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
            else if (sortBy == LanguagesManager.Instance.StoreUploadControl_Keywords)
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
            else if (sortBy == LanguagesManager.Instance.StoreUploadControl_CreationTime)
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
            else if (sortBy == LanguagesManager.Instance.StoreUploadControl_State)
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
            if (_treeView.SelectedItem is StoreCategorizeTreeViewModel)
            {
                _storeCategorizeTreeViewItemNewCategoryMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is StoreTreeViewModel)
            {
                _storeTreeViewItemNewBoxMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is BoxTreeViewModel)
            {
                _boxTreeViewItemNewBoxMenuItem_Click(null, null);
            }
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                if (_treeView.SelectedItem is StoreCategorizeTreeViewModel)
                {
                    _storeCategorizeTreeViewItemDeleteMenuItem_Click(null, null);
                }
                else if (_treeView.SelectedItem is StoreTreeViewModel)
                {
                    _storeTreeViewItemDeleteMenuItem_Click(null, null);
                }
                else if (_treeView.SelectedItem is BoxTreeViewModel)
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
                if (_treeView.SelectedItem is StoreCategorizeTreeViewModel)
                {
                    _storeCategorizeTreeViewItemCopyMenuItem_Click(null, null);
                }
                else if (_treeView.SelectedItem is StoreTreeViewModel)
                {
                    _storeTreeViewItemCopyMenuItem_Click(null, null);
                }
                else if (_treeView.SelectedItem is BoxTreeViewModel)
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
                if (_treeView.SelectedItem is StoreCategorizeTreeViewModel)
                {
                    _storeCategorizeTreeViewItemCutMenuItem_Click(null, null);
                }
                else if (_treeView.SelectedItem is StoreTreeViewModel)
                {
                    _storeTreeViewItemCutMenuItem_Click(null, null);
                }
                else if (_treeView.SelectedItem is BoxTreeViewModel)
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
            if (_treeView.SelectedItem is StoreCategorizeTreeViewModel)
            {
                _storeCategorizeTreeViewItemPasteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is StoreTreeViewModel)
            {
                _storeTreeViewItemPasteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is BoxTreeViewModel)
            {
                _boxTreeViewItemPasteMenuItem_Click(null, null);
            }
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
