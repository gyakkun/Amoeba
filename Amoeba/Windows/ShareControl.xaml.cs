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
using Library.Collections;
using Library.Net.Amoeba;

namespace Amoeba.Windows
{
    /// <summary>
    /// ShareControl.xaml の相互作用ロジック
    /// </summary>
    partial class ShareControl : UserControl
    {
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollection<ShareListViewItem> _listViewItemCollection = new ObservableCollection<ShareListViewItem>();

        public ShareControl(MainWindow mainWindow, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _shareListView.ItemsSource = _listViewItemCollection;

            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ShareItemShow), this);
        }

        private void ShareItemShow(object state)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Thread.CurrentThread.IsBackground = true;

            try
            {
                for (; ; )
                {
                    Thread.Sleep(1000 * 3);
                    if (App.SelectTab != "Share") continue;

                    var shareInformation = _amoebaManager.ShareInformation.ToArray();
                    Dictionary<int, Information> dic = new Dictionary<int, Information>();

                    foreach (var item in shareInformation.ToArray())
                    {
                        dic[(int)item["Id"]] = item;
                    }

                    Dictionary<int, ShareListViewItem> dic2 = new Dictionary<int, ShareListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in _listViewItemCollection.ToArray())
                        {
                            dic2[(int)item.Information["Id"]] = item;
                        }
                    }), null);

                    List<ShareListViewItem> removeList = new List<ShareListViewItem>();
                    Dictionary<ShareListViewItem, Information> updateDic = new Dictionary<ShareListViewItem, Information>();
                    List<ShareListViewItem> newList = new List<ShareListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in _listViewItemCollection.ToArray())
                        {
                            if (!dic.ContainsKey((int)item.Information["Id"]))
                            {
                                removeList.Add(item);
                            }
                        }

                        if (removeList.Count > 100)
                        {
                            updateDic.Clear();
                            removeList.Clear();
                            _listViewItemCollection.Clear();
                        }
                    }), null);

                    foreach (var information in shareInformation)
                    {
                        ShareListViewItem item = null;

                        if (dic2.ContainsKey((int)information["Id"]))
                            item = dic2[(int)information["Id"]];

                        if (item != null)
                        {
                            if (!Collection.Equals(item.Information, information))
                            {
                                updateDic[item] = information;
                            }
                        }
                        else
                        {
                            newList.Add(new ShareListViewItem(information));
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        bool sortFlag = false;

                        if (newList.Count != 0) sortFlag = true;
                        if (removeList.Count != 0) sortFlag = true;
                        if (updateDic.Count != 0) sortFlag = true;

                        foreach (var item in newList)
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

                        if (sortFlag && _listViewItemCollection.Count < 10000) this.Sort();
                    }), null);
                }
            }
            catch (Exception)
            {

            }
        }

        private void _shareListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_shareListView.GetCurrentIndex(e.GetPosition) == -1)
            {
                _shareListView.SelectedItems.Clear();
            }
        }

        private void _shareListView_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void _shareListView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var filePaths = new List<string>();

            foreach (var item in ((string[])e.Data.GetData(DataFormats.FileDrop)).ToList())
            {
                if (File.Exists(item)) filePaths.Add(item);
                else if (Directory.Exists(item)) filePaths.AddRange(Directory.GetFiles(item));
            }


            if (filePaths.Count == 1)
            {
                UploadWindow window = new UploadWindow(filePaths[0], true, _amoebaManager);
                window.Owner = _mainWindow;
                window.ShowDialog();
            }
            else if (filePaths.Count > 1)
            {
                UploadListWindow window = new UploadListWindow(filePaths, true, _amoebaManager);
                window.Owner = _mainWindow;
                window.ShowDialog();
            }
        }

        private void _shareListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _shareListView.SelectedItems;
            if (selectItems == null) return;

            _shareListViewDeleteMenuItem.IsEnabled = (selectItems.Count > 0);
            _shareListViewCheckExistMenuItem.IsEnabled = (_listViewItemCollection.Count > 0);
        }

        private void _shareListViewAddMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var uploadFilePaths = dialog.FileNames.ToList();

                if (uploadFilePaths.Count == 1)
                {
                    UploadWindow window = new UploadWindow(uploadFilePaths[0], true, _amoebaManager);
                    window.Owner = _mainWindow;
                    window.ShowDialog();
                }
                else if (uploadFilePaths.Count > 1)
                {
                    UploadListWindow window = new UploadListWindow(uploadFilePaths, true, _amoebaManager);
                    window.Owner = _mainWindow;
                    window.ShowDialog();
                }
            }
        }
        
        private void _shareListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var shareItems = _shareListView.SelectedItems;
            if (shareItems == null) return;

            foreach (var item in shareItems.Cast<ShareListViewItem>())
            {
                _amoebaManager.ShareRemove((int)item.Information["Id"]);
            }
        }

        private void _shareListViewCheckExistMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                var shareInformation = _amoebaManager.ShareInformation.ToArray();

                foreach (var item in shareInformation)
                {
                    if (item.Contains("Path") && !File.Exists((string)item["Path"]))
                    {
                        try
                        {
                            _amoebaManager.ShareRemove((int)item["Id"]);
                            Log.Information("Share Delete: " + (string)item["Path"]);
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }));
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
                _shareListView.SelectedIndex = -1;

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

                Sort(headerClicked, direction);

                Settings.Instance.ShareControl_LastHeaderClicked = headerClicked;
                Settings.Instance.ShareControl_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.ShareControl_LastHeaderClicked != null)
                {
                    var list = new List<ShareListViewItem>(_listViewItemCollection);
                    var list2 = Sort(list, Settings.Instance.ShareControl_LastHeaderClicked, Settings.Instance.ShareControl_ListSortDirection).ToList();

                    for (int i = 0; i < list2.Count; i++)
                    {
                        var o = _listViewItemCollection.IndexOf(list2[i]);

                        if (i != o) _listViewItemCollection.Move(o, i);
                    }
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _shareListView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.ShareControl_Path)
            {
                _shareListView.Items.SortDescriptions.Add(new SortDescription("Path", direction));
            }
            else if (sortBy == LanguagesManager.Instance.ShareControl_BlockCount)
            {
                _shareListView.Items.SortDescriptions.Add(new SortDescription("BlockCount", direction));
            }
        }

        private IEnumerable<ShareListViewItem> Sort(IEnumerable<ShareListViewItem> collection, string sortBy, ListSortDirection direction)
        {
            List<ShareListViewItem> list = new List<ShareListViewItem>(collection);

            if (sortBy == LanguagesManager.Instance.ShareControl_Path)
            {
                list.Sort(delegate(ShareListViewItem x, ShareListViewItem y)
                {
                    int c = x.Path.CompareTo(y.Path);
                    if (c != 0) return c;
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.ShareControl_BlockCount)
            {
                list.Sort(delegate(ShareListViewItem x, ShareListViewItem y)
                {
                    int c = x.BlockCount.CompareTo(y.BlockCount);
                    if (c != 0) return c;
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
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

            private Information _information;
            private string _path = null;
            private int _blockCount = 0;

            public ShareListViewItem(Information information)
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

                    if (_information.Contains("Path")) this.Path = (string)_information["Path"];
                    else this.Path = null;

                    if (_information.Contains("BlockCount")) this.BlockCount = (int)_information["BlockCount"];
                    else this.BlockCount = 0;
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
    }
}
