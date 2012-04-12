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
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollection<ShareListViewItem> _shareListViewItemCollection = new ObservableCollection<ShareListViewItem>();

        public ShareControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _shareListView.ItemsSource = _shareListViewItemCollection;

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

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in _shareListViewItemCollection.ToArray())
                        {
                            dic2[(int)item.Information["Id"]] = item;
                        }
                    }), null);

                    List<ShareListViewItem> removeList = new List<ShareListViewItem>();
                    Dictionary<ShareListViewItem, Information> updateDic = new Dictionary<ShareListViewItem, Information>();
                    List<ShareListViewItem> newList = new List<ShareListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in _shareListViewItemCollection.ToArray())
                        {
                            if (!dic.ContainsKey((int)item.Information["Id"]))
                            {
                                removeList.Add(item);
                            }
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

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            foreach (var item in removeList)
                            {
                                _shareListViewItemCollection.Remove(item);
                            }

                            foreach (var item in newList)
                            {
                                _shareListViewItemCollection.Add(item);
                            }

                            foreach (var item in updateDic)
                            {
                                item.Key.Information = item.Value;
                            }

                            this.Sort();
                        }
                        catch (Exception)
                        {

                        }
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

            var uploadFilePaths = ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(item => File.Exists(item)).ToList();

            if (uploadFilePaths.Count == 1)
            {
                UploadWindow window = new UploadWindow(uploadFilePaths[0], true, _amoebaManager);
                window.ShowDialog();
            }
            else if (uploadFilePaths.Count > 1)
            {
                UploadListWindow window = new UploadListWindow(uploadFilePaths, true, _amoebaManager);
                window.ShowDialog();
            }
        }

        private void _shareListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _shareListView.SelectedItems;
            if (selectItems == null) return;

            _shareListViewDeleteMenuItem.IsEnabled = (selectItems.Count > 0);
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
                    window.ShowDialog();
                }
                else if (uploadFilePaths.Count > 1)
                {
                    UploadListWindow window = new UploadListWindow(uploadFilePaths, true, _amoebaManager);
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

        #region Sort

        private void Sort()
        {
            this.GridViewColumnHeaderClickedHandler(null, null);
        }

        private string _lastHeaderClicked = LanguagesManager.Instance.ShareControl_Path;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

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

                if (headerClicked != _lastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (_lastDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<object>(delegate(object state2)
                {
                    var list = new List<ShareListViewItem>(_shareListViewItemCollection);
                    var list2 = Sort(list, headerClicked, direction).ToList();

                    for (int i = 0; i < list2.Count; i++)
                    {
                        var o = _shareListViewItemCollection.IndexOf(list2[i]);

                        if (i != o) _shareListViewItemCollection.Move(o, i);
                    }
                }), null);

                _lastHeaderClicked = headerClicked;
                _lastDirection = direction;
            }
            else
            {
                if (_lastHeaderClicked != null)
                {
                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<object>(delegate(object state2)
                    {
                        var list = new List<ShareListViewItem>(_shareListViewItemCollection);
                        var list2 = Sort(list, _lastHeaderClicked, _lastDirection).ToList();

                        for (int i = 0; i < list2.Count; i++)
                        {
                            var o = _shareListViewItemCollection.IndexOf(list2[i]);

                            if (i != o) _shareListViewItemCollection.Move(o, i);
                        }
                    }), null);
                }
            }
        }

        private IEnumerable<ShareListViewItem> Sort(IEnumerable<ShareListViewItem> collection, string sortBy, ListSortDirection direction)
        {
            List<ShareListViewItem> list = new List<ShareListViewItem>(collection);

            if (sortBy == LanguagesManager.Instance.ShareControl_Name)
            {
                list.Sort(delegate(ShareListViewItem x, ShareListViewItem y)
                {
                    int c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.ShareControl_Path)
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
            else if (sortBy == LanguagesManager.Instance.ShareControl_Length)
            {
                list.Sort(delegate(ShareListViewItem x, ShareListViewItem y)
                {
                    int c = ((long)x.Information["Length"]).CompareTo((long)y.Information["Length"]);
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
            private string _name = null;
            private string _path = null;
            private int _blockCount = 0;
            private string _length = "";

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

                    if (_information.Contains("Path")) this.Name = System.IO.Path.GetFileName((string)_information["Path"]);
                    else this.Name = null;

                    if (_information.Contains("Path")) this.Path = (string)_information["Path"];
                    else this.Path = null;

                    if (_information.Contains("BlockCount")) this.BlockCount = (int)_information["BlockCount"];
                    else this.BlockCount = 0;

                    try
                    {
                        this.Length = NetworkConverter.ToSizeString(new FileInfo(this.Path).Length);
                    }
                    catch (Exception)
                    {

                    }
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

            public string Length
            {
                get
                {
                    return _length;
                }
                set
                {
                    if (value != _length)
                    {
                        _length = value;

                        this.NotifyPropertyChanged("Length");
                    }
                }
            }
        }
    }
}
