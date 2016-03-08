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
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollectionEx<ShareListViewItem> _listViewItemCollection = new ObservableCollectionEx<ShareListViewItem>();

        private Thread _showShareItemThread;

        private volatile bool _shareAddIsRunning = false;

        public ShareControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _listView.ItemsSource = _listViewItemCollection;

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
                    Thread.Sleep(100);
                    if (_mainWindow.SelectedTab != MainWindowTabType.Share) continue;

                    Dictionary<int, Information> informaitonDic = new Dictionary<int, Information>();

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

                            informaitonDic[(int)item["Id"]] = item;
                        }
                    }

                    Dictionary<int, ShareListViewItem> listViewItemDic = new Dictionary<int, ShareListViewItem>();
                    List<ShareListViewItem> removeList = new List<ShareListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        foreach (var item in _listViewItemCollection.ToArray())
                        {
                            listViewItemDic[item.Id] = item;

                            if (!informaitonDic.ContainsKey(item.Id))
                            {
                                removeList.Add(item);
                            }
                        }
                    }));

                    List<ShareListViewItem> resultList = new List<ShareListViewItem>();
                    Dictionary<ShareListViewItem, Information> updateDic = new Dictionary<ShareListViewItem, Information>();

                    bool clearFlag = false;
                    var selectItems = new List<ShareListViewItem>();

                    if (removeList.Count > 100)
                    {
                        clearFlag = true;

                        removeList.Clear();
                        updateDic.Clear();

                        foreach (var information in informaitonDic.Values)
                        {
                            resultList.Add(new ShareListViewItem(information));
                        }

                        HashSet<int> hid = new HashSet<int>();

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            hid.UnionWith(_listView.SelectedItems.OfType<ShareListViewItem>().Select(n => n.Id));
                        }));

                        foreach (var item in resultList)
                        {
                            if (hid.Contains(item.Id))
                            {
                                selectItems.Add(item);
                            }
                        }
                    }
                    else
                    {
                        foreach (var information in informaitonDic.Values)
                        {
                            ShareListViewItem item;

                            if (listViewItemDic.TryGetValue((int)information["Id"], out item))
                            {
                                if (!CollectionUtilities.Equals(item.Information, information))
                                {
                                    updateDic[item] = information;
                                }
                            }
                            else
                            {
                                resultList.Add(new ShareListViewItem(information));
                            }
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        bool sortFlag = false;

                        if (resultList.Count != 0) sortFlag = true;
                        if (removeList.Count != 0) sortFlag = true;
                        if (updateDic.Count != 0) sortFlag = true;

                        if (clearFlag) _listViewItemCollection.Clear();

                        foreach (var item in resultList)
                        {
                            _listViewItemCollection.Add(item);
                        }

                        foreach (var item in removeList)
                        {
                            _listViewItemCollection.Remove(item);
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

            ThreadPool.QueueUserWorkItem((object wstate) =>
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
                            UploadWindow window = new UploadWindow(filePaths.First(), true, _amoebaManager);
                            window.Owner = _mainWindow;
                            window.ShowDialog();
                        }
                        else if (filePaths.Count > 1)
                        {
                            UploadListWindow window = new UploadListWindow(filePaths, true, _amoebaManager);
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
            else _listViewCheckExistMenuItem.IsEnabled = (_listViewItemCollection.Count > 0);

            if (!_listViewDeleteMenuItem_IsEnabled) _listViewDeleteMenuItem.IsEnabled = false;
            else _listViewDeleteMenuItem.IsEnabled = (_listViewItemCollection.Count > 0);
        }

        private void _listViewAddMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Multiselect = true;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var filePaths = new HashSet<string>(dialog.FileNames);

                ThreadPool.QueueUserWorkItem((object wstate) =>
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
                                UploadWindow window = new UploadWindow(filePaths.First(), true, _amoebaManager);
                                window.Owner = _mainWindow;
                                window.ShowDialog();
                            }
                            else if (filePaths.Count > 1)
                            {
                                UploadListWindow window = new UploadListWindow(filePaths, true, _amoebaManager);
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

            List<int> ids = new List<int>();

            foreach (var item in selectItems.Cast<ShareListViewItem>())
            {
                ids.Add(item.Id);
            }

            _listViewDeleteMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in ids)
                    {
                        _amoebaManager.RemoveShare(item);
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

            ThreadPool.QueueUserWorkItem((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    var shareInformation = _amoebaManager.ShareInformation.ToArray();
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Share Delete");

                    foreach (var item in shareInformation)
                    {
                        if (item.Contains("Path") && !File.Exists((string)item["Path"]))
                        {
                            try
                            {
                                _amoebaManager.RemoveShare((int)item["Id"]);
                                sb.AppendLine((string)item["Path"]);
                            }
                            catch (Exception)
                            {

                            }
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
                var list = this.Sort(_listViewItemCollection, Settings.Instance.ShareControl_LastHeaderClicked, Settings.Instance.ShareControl_ListSortDirection).ToList();

                for (int i = 0; i < list.Count; i++)
                {
                    var o = _listViewItemCollection.IndexOf(list[i]);

                    if (i != o) _listViewItemCollection.Move(o, i);
                }
            }
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            var item = e.OriginalSource as GridViewColumnHeader;
            if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

            string headerClicked = item.Column.Header as string;
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
            _listView.Items.SortDescriptions.Add(new SortDescription("Id", direction));
        }

        private IEnumerable<ShareListViewItem> Sort(IEnumerable<ShareListViewItem> collection, string sortBy, ListSortDirection direction)
        {
            List<ShareListViewItem> list = new List<ShareListViewItem>(collection);

            if (sortBy == LanguagesManager.Instance.ShareControl_Name)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
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
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
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
                    c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
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

        private void _serachCloseButton_Click(object sender, RoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(0);
            _searchTextBox.Text = "";
        }

        private class ShareListViewItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(string info)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
            }

            private int _id;
            private Information _information;
            private string _name;
            private string _path;
            private int _blockCount;

            public ShareListViewItem(Information information)
            {
                this.Information = information;

                _id = (int)this.Information["Id"];
            }

            public int Id
            {
                get
                {
                    return _id;
                }
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

                        this.NotifyPropertyChanged("Name");
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

                        this.NotifyPropertyChanged("Path");
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

                        this.NotifyPropertyChanged("BlockCount");
                    }
                }
            }
        }

        private void Execute_New(object sender, ExecutedRoutedEventArgs e)
        {
            _listViewAddMenuItem_Click(null, null);
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            _listViewDeleteMenuItem_Click(null, null);
        }

        private void Execute_Search(object sender, ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(24);
            _searchTextBox.Focus();
        }
    }
}
