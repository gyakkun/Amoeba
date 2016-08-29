using System;
using System.Collections;
using System.Collections.Concurrent;
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
using Library.Net.Amoeba;

namespace Amoeba.Windows
{
    /// <summary>
    /// ShareControl.xaml の相互作用ロジック
    /// </summary>
    partial class ShareControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;

        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollectionEx<ShareListViewModel> _listViewModelCollection = new ObservableCollectionEx<ShareListViewModel>();

        private Thread _showShareItemThread;

        private volatile bool _shareAddIsRunning = false;

        public ShareControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _listView.ItemsSource = _listViewModelCollection;

            _showShareItemThread = new Thread(this.ShowShareItem);
            _showShareItemThread.Priority = ThreadPriority.Highest;
            _showShareItemThread.IsBackground = true;
            _showShareItemThread.Name = "ShareControl_ShowShareItemThread";
            _showShareItemThread.Start();

            _searchRowDefinition.Height = new GridLength(0);

            LanguagesManager.UsingLanguageChangedEvent += this.LanguagesManager_UsingLanguageChangedEvent;
        }

        private void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {
            _listView.Items.Refresh();
        }

        private void ShowShareItem()
        {
            try
            {
                for (;;)
                {
                    var informaitonDic = new Dictionary<string, Information>();

                    {
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

                        foreach (var item in _amoebaManager.ShareInformation.ToArray())
                        {
                            if (words != null)
                            {
                                var text = ((string)item["Path"] ?? "").ToLower();
                                if (!words.All(n => text.Contains(n))) continue;
                            }

                            informaitonDic[(string)item["Path"]] = item;
                        }
                    }

                    var listViewModelDic = new Dictionary<string, ShareListViewModel>();
                    var removeList = new List<ShareListViewModel>();

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        foreach (var item in _listViewModelCollection.ToArray())
                        {
                            listViewModelDic[item.Path] = item;

                            if (!informaitonDic.ContainsKey(item.Path))
                            {
                                removeList.Add(item);
                            }
                        }
                    }));

                    var resultList = new List<ShareListViewModel>();
                    var updateDic = new Dictionary<ShareListViewModel, Information>();

                    bool clearFlag = false;
                    var selectItems = new List<ShareListViewModel>();

                    if (removeList.Count > 100)
                    {
                        clearFlag = true;

                        removeList.Clear();
                        updateDic.Clear();

                        foreach (var information in informaitonDic.Values)
                        {
                            resultList.Add(new ShareListViewModel(information));
                        }

                        var hpath = new HashSet<string>();

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            hpath.UnionWith(_listView.SelectedItems.OfType<ShareListViewModel>().Select(n => n.Path));
                        }));

                        foreach (var item in resultList)
                        {
                            if (hpath.Contains(item.Path))
                            {
                                selectItems.Add(item);
                            }
                        }
                    }
                    else
                    {
                        foreach (var information in informaitonDic.Values)
                        {
                            ShareListViewModel item;

                            if (listViewModelDic.TryGetValue((string)information["Path"], out item))
                            {
                                if (!CollectionUtils.Equals(item.Information, information))
                                {
                                    updateDic[item] = information;
                                }
                            }
                            else
                            {
                                resultList.Add(new ShareListViewModel(information));
                            }
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        bool sortFlag = false;

                        if (resultList.Count != 0) sortFlag = true;
                        if (removeList.Count != 0) sortFlag = true;
                        if (updateDic.Count != 0) sortFlag = true;

                        if (clearFlag) _listViewModelCollection.Clear();

                        foreach (var item in resultList)
                        {
                            _listViewModelCollection.Add(item);
                        }

                        foreach (var item in removeList)
                        {
                            _listViewModelCollection.Remove(item);
                        }

                        foreach (var item in updateDic)
                        {
                            item.Key.Information = item.Value;
                        }

                        if (clearFlag)
                        {
                            _listView.SelectedItems.Clear();
                            _listView.SetSelectedItems(selectItems);
                        }

                        if (sortFlag) this.Sort();
                    }));

                    Thread.Sleep(1000 * 3);
                }
            }
            catch (Exception)
            {

            }
        }

        private void _listView_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void _listView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var result = ((string[])e.Data.GetData(DataFormats.FileDrop)).ToList();

            Task.Run(() =>
            {
                if (_shareAddIsRunning) return;
                _shareAddIsRunning = true;

                Thread.CurrentThread.IsBackground = true;

                try
                {
                    var filePaths = new HashSet<string>();

                    foreach (var item in result)
                    {
                        if (File.Exists(item)) filePaths.Add(item);
                        else if (Directory.Exists(item)) filePaths.UnionWith(Directory.GetFiles(item, "*", SearchOption.AllDirectories));
                    }

                    foreach (var informaiton in _amoebaManager.ShareInformation)
                    {
                        filePaths.Remove((string)informaiton["Path"]);
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        if (filePaths.Count == 1)
                        {
                            var window = new UploadWindow(filePaths.First(), true, _amoebaManager);
                            window.Owner = _mainWindow;
                            window.ShowDialog();
                        }
                        else if (filePaths.Count > 1)
                        {
                            var window = new UploadListWindow(filePaths, true, _amoebaManager);
                            window.Owner = _mainWindow;
                            window.ShowDialog();
                        }
                    }));
                }
                finally
                {
                    _shareAddIsRunning = false;
                }
            });
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _listView.SelectedItems;

            if (!_listViewDeleteMenuItem_IsEnabled) _listViewDeleteMenuItem.IsEnabled = false;
            else _listViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

            if (!_listViewCheckExistMenuItem_IsEnabled) _listViewCheckExistMenuItem.IsEnabled = false;
            else _listViewCheckExistMenuItem.IsEnabled = (_listViewModelCollection.Count > 0);

            if (!_listViewDeleteMenuItem_IsEnabled) _listViewDeleteMenuItem.IsEnabled = false;
            else _listViewDeleteMenuItem.IsEnabled = (_listViewModelCollection.Count > 0);
        }

        private void _listViewAddMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Multiselect = true;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var filePaths = new HashSet<string>(dialog.FileNames);

                Task.Run(() =>
                {
                    if (_shareAddIsRunning) return;
                    _shareAddIsRunning = true;

                    Thread.CurrentThread.IsBackground = true;

                    try
                    {
                        foreach (var informaiton in _amoebaManager.ShareInformation)
                        {
                            filePaths.Remove((string)informaiton["Path"]);
                        }

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            if (filePaths.Count == 1)
                            {
                                var window = new UploadWindow(filePaths.First(), true, _amoebaManager);
                                window.Owner = _mainWindow;
                                window.ShowDialog();
                            }
                            else if (filePaths.Count > 1)
                            {
                                var window = new UploadListWindow(filePaths, true, _amoebaManager);
                                window.Owner = _mainWindow;
                                window.ShowDialog();
                            }
                        }));
                    }
                    finally
                    {
                        _shareAddIsRunning = false;
                    }
                });
            }
        }

        volatile bool _listViewDeleteMenuItem_IsEnabled = true;

        private void _listViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _listView.SelectedItems;
            if (selectItems == null || selectItems.Count == 0) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Share", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var paths = new List<string>();

            foreach (var item in selectItems.Cast<ShareListViewModel>())
            {
                paths.Add(item.Path);
            }

            _listViewDeleteMenuItem_IsEnabled = false;

            Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var path in paths)
                    {
                        _amoebaManager.RemoveShare(path);
                    }
                }
                catch (Exception)
                {

                }

                _listViewDeleteMenuItem_IsEnabled = true;
            });
        }

        volatile bool _listViewCheckExistMenuItem_IsEnabled = true;

        private void _listViewCheckExistMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _listViewCheckExistMenuItem_IsEnabled = false;

            Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Share Delete");

                    var paths = _amoebaManager.ShareInformation.ToArray()
                        .Where(n => n.Contains("Path"))
                        .Select(n => (string)n["Path"]);

                    var dic = new ConcurrentDictionary<string, HashSet<string>>();

                    foreach (var path in paths)
                    {
                        var directoryPath = System.IO.Path.GetDirectoryName(path);

                        var hashSet = dic.GetOrAdd(directoryPath, _ =>
                        {
                            return new HashSet<string>(Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly));
                        });

                        if (!hashSet.Contains(path))
                        {
                            _amoebaManager.RemoveShare(path);
                            sb.AppendLine(path);
                        }
                    }

                    Log.Information(sb.ToString().TrimEnd('\r', '\n'));
                }
                catch (Exception)
                {

                }

                _listViewCheckExistMenuItem_IsEnabled = true;
            });
        }

        #region Sort

        private void Sort()
        {
            _listView.Items.SortDescriptions.Clear();

            if (Settings.Instance.ShareControl_LastHeaderClicked != null)
            {
                var list = this.Sort(_listViewModelCollection, Settings.Instance.ShareControl_LastHeaderClicked, Settings.Instance.ShareControl_ListSortDirection).ToList();

                for (int i = 0; i < list.Count; i++)
                {
                    var o = _listViewModelCollection.IndexOf(list[i]);

                    if (i != o) _listViewModelCollection.Move(o, i);
                }
            }
        }

        private void _listView_GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var item = e.OriginalSource as GridViewColumnHeader;
            if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

            var headerClicked = item.Column.Header as string;
            if (headerClicked == null) return;

            ListSortDirection direction;

            if (headerClicked != Settings.Instance.ShareControl_LastHeaderClicked)
            {
                direction = ListSortDirection.Ascending;
            }
            else
            {
                if (Settings.Instance.ShareControl_ListSortDirection == ListSortDirection.Ascending)
                {
                    direction = ListSortDirection.Descending;
                }
                else
                {
                    direction = ListSortDirection.Ascending;
                }
            }

            this.Sort(headerClicked, direction);

            Settings.Instance.ShareControl_LastHeaderClicked = headerClicked;
            Settings.Instance.ShareControl_ListSortDirection = direction;
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.ShareControl_Path)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Path", direction));
            }
            else if (sortBy == LanguagesManager.Instance.ShareControl_BlockCount)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("BlockCount", direction));
            }

            _listView.Items.SortDescriptions.Add(new SortDescription("Name", direction));
        }

        private IEnumerable<ShareListViewModel> Sort(IEnumerable<ShareListViewModel> collection, string sortBy, ListSortDirection direction)
        {
            var list = new List<ShareListViewModel>(collection);

            if (sortBy == LanguagesManager.Instance.ShareControl_Name)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Path.CompareTo(y.Path);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.ShareControl_Path)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Path.CompareTo(y.Path);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.ShareControl_BlockCount)
            {
                list.Sort((x, y) =>
                {
                    int c = x.BlockCount.CompareTo(y.BlockCount);
                    if (c != 0) return c;
                    c = x.Path.CompareTo(y.Path);
                    if (c != 0) return c;

                    return 0;
                });
            }

            if (direction == ListSortDirection.Descending)
            {
                list.Reverse();
            }

            return list;
        }

        #endregion

        private class ShareListViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(string info)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
            }

            private Information _information;
            private string _name;
            private string _path;
            private int _blockCount;

            public ShareListViewModel(Information information)
            {
                this.Information = information;
            }

            public Information Information
            {
                get
                {
                    return _information;
                }
                set
                {
                    _information = value;

                    if (_information.Contains("Path"))
                    {
                        var fullPath = (string)_information["Path"];

                        if (fullPath != null)
                        {
                            this.Path = fullPath;
                            this.Name = System.IO.Path.GetFileName(fullPath);
                        }
                    }
                    else this.Path = null;

                    if (_information.Contains("BlockCount")) this.BlockCount = (int)_information["BlockCount"];
                    else this.BlockCount = 0;
                }
            }

            public string Name
            {
                get
                {
                    return _name;
                }
                set
                {
                    if (value != _name)
                    {
                        _name = value;

                        this.NotifyPropertyChanged(nameof(this.Name));
                    }
                }
            }

            public string Path
            {
                get
                {
                    return _path;
                }
                set
                {
                    if (value != _path)
                    {
                        _path = value;

                        this.NotifyPropertyChanged(nameof(this.Path));
                    }
                }
            }

            public int BlockCount
            {
                get
                {
                    return _blockCount;
                }
                set
                {
                    if (value != _blockCount)
                    {
                        _blockCount = value;

                        this.NotifyPropertyChanged(nameof(this.BlockCount));
                    }
                }
            }
        }

        private void Execute_New(object sender, ExecutedRoutedEventArgs e)
        {
            _listViewAddMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            _listViewDeleteMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
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
        }
    }
}
