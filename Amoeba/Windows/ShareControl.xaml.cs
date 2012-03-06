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
            for (; ; )
            {
                Thread.Sleep(1000 * 1);

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<object>(delegate(object state2)
                {
                    try
                    {
                        var shareingInformation = _amoebaManager.ShareInformation.ToArray();

                        foreach (var item in _shareListViewItemCollection.ToArray())
                        {
                            if (!shareingInformation.Any(n => (int)n["Id"] == (int)item.Information["Id"]))
                            {
                                _shareListViewItemCollection.Remove(item);
                            }
                        }

                        foreach (var information in shareingInformation)
                        {
                            var item = _shareListViewItemCollection.FirstOrDefault(n => (int)n.Information["Id"] == (int)information["Id"]);

                            if (item != null)
                            {
                                if (!Collection.Equals(item.Information, information))
                                {
                                    item.Information = information;

                                    this.Sort();
                                }
                            }
                            else
                            {
                                _shareListViewItemCollection.Add(new ShareListViewItem(information));

                                this.Sort();
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }), null);
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
            if (_shareListViewDeleteMenuItem != null) _shareListViewDeleteMenuItem.IsEnabled = (_shareListView.SelectedItems.Count > 0);
        }

        private void _shareListViewAddMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Multiselect = true;
            dialog.ShowDialog();

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

                Sort(headerClicked, direction);

                _lastHeaderClicked = headerClicked;
                _lastDirection = direction;
            }
            else
            {
                if (_lastHeaderClicked != null)
                {
                    Sort(_lastHeaderClicked, _lastDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            string propertyName = null;

            if (sortBy == LanguagesManager.Instance.ShareControl_Name)
            {
                propertyName = "Name";
            }
            else if (sortBy == LanguagesManager.Instance.ShareControl_Path)
            {
                propertyName = "Path";
            }
            else if (sortBy == LanguagesManager.Instance.ShareControl_Length)
            {
                propertyName = "Length";
            }
            else if (sortBy == LanguagesManager.Instance.ShareControl_BlockCount)
            {
                propertyName = "BlockCount";
            }

            if (propertyName == null) return;

            _shareListView.Items.SortDescriptions.Clear();
            _shareListView.Items.SortDescriptions.Add(new SortDescription(propertyName, direction));
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
            private long _length = 0;

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
                        this.Length = new FileInfo(this.Path).Length;
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

            public long Length
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
