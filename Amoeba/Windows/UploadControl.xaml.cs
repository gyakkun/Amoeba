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
    /// UploadControl.xaml の相互作用ロジック
    /// </summary>
    partial class UploadControl : UserControl
    {
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollection<UploadListViewItem> _uploadListViewItemCollection = new ObservableCollection<UploadListViewItem>();

        public UploadControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _uploadListView.ItemsSource = _uploadListViewItemCollection;

            ThreadPool.QueueUserWorkItem(new WaitCallback(this.UploadItemShow), this);
        }

        private void UploadItemShow(object state)
        {
            for (; ; )
            {
                Thread.Sleep(1000 * 1);

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<object>(delegate(object state2)
                {
                    try
                    {
                        var uploadingInformation = _amoebaManager.UploadingInformation.ToArray();

                        foreach (var item in _uploadListViewItemCollection.ToArray())
                        {
                            if (!uploadingInformation.Any(n => (int)n["Id"] == (int)item.Information["Id"]))
                            {
                                _uploadListViewItemCollection.Remove(item);
                            }
                        }

                        foreach (var information in uploadingInformation)
                        {
                            var item = _uploadListViewItemCollection.FirstOrDefault(n => (int)n.Information["Id"] == (int)information["Id"]);

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
                                _uploadListViewItemCollection.Add(new UploadListViewItem(information));

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

        private void _uploadListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_uploadListView.GetCurrentIndex(e.GetPosition) == -1)
            {
                _uploadListView.SelectedItems.Clear();
            }
        }

        private void _uploadListView_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void _uploadListView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var uploadFilePaths = ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(item => File.Exists(item)).ToList();

            if (uploadFilePaths.Count == 1)
            {
                UploadWindow window = new UploadWindow(uploadFilePaths[0], false, _amoebaManager);
                window.ShowDialog();
            }
            else if(uploadFilePaths.Count > 1)
            {
                UploadListWindow window = new UploadListWindow(uploadFilePaths, false, _amoebaManager);
                window.ShowDialog();
            }
        }

        private void _uploadListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_uploadListViewDeleteMenuItem != null) _uploadListViewDeleteMenuItem.IsEnabled = (_uploadListView.SelectedItems.Count > 0);
            if (_uploadListViewCopyMenuItem != null) _uploadListViewCopyMenuItem.IsEnabled = (_uploadListView.SelectedItems.Count > 0);
            if (_uploadListViewCopyInfoMenuItem != null) _uploadListViewCopyInfoMenuItem.IsEnabled = (_uploadListView.SelectedItems.Count > 0);
            if (_uploadListViewPriorityMenuItem != null) _uploadListViewPriorityMenuItem.IsEnabled = (_uploadListView.SelectedItems.Count > 0);
        }

        private void _uploadListViewAddMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Multiselect = true;
            dialog.ShowDialog();

            var uploadFilePaths = dialog.FileNames.ToList();

            if (uploadFilePaths.Count == 1)
            {
                UploadWindow window = new UploadWindow(uploadFilePaths[0], false, _amoebaManager);
                window.ShowDialog();
            }
            else if (uploadFilePaths.Count > 1)
            {
                UploadListWindow window = new UploadListWindow(uploadFilePaths, false, _amoebaManager);
                window.ShowDialog();
            }
        }
        
        private void _uploadListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var uploadItems = _uploadListView.SelectedItems;
            if (uploadItems == null) return;

            foreach (var item in uploadItems.Cast<UploadListViewItem>())
            {
                _amoebaManager.UploadRemove((int)item.Information["Id"]);
            }
        }

        private void _uploadListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var uploadItems = _uploadListView.SelectedItems;
            if (uploadItems == null) return;

            var sb = new StringBuilder();

            foreach (var item in uploadItems.Cast<UploadListViewItem>())
            {
                if (item.Value != null) sb.AppendLine(AmoebaConverter.ToSeedString(item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _uploadListViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _uploadListView.SelectedItems;
            if (selectItems == null) return;

            var item = selectItems.Cast<UploadListViewItem>().FirstOrDefault();
            if (item == null || item.Value == null) return;

            try
            {
                Clipboard.SetText(MessageConverter.ToInfoMessage(item.Value));
            }
            catch (Exception)
            {

            }
        }

        private void SetPriority(int i)
        {
            var uploadItems = _uploadListView.SelectedItems;
            if (uploadItems == null) return;

            foreach (var item in uploadItems.Cast<UploadListViewItem>())
            {
                _amoebaManager.SetUploadPriority((int)item.Information["Id"], i);
            }
        }

        #region Priority

        private void _uploadListViewPriority0MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(0);
        }

        private void _uploadListViewPriority1MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(1);
        }

        private void _uploadListViewPriority2MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(2);
        }

        private void _uploadListViewPriority3MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(3);
        }

        private void _uploadListViewPriority4MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(4);
        }

        private void _uploadListViewPriority5MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(5);
        }

        private void _uploadListViewPriority6MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(6);
        }

        #endregion

        #region Sort

        private void Sort()
        {
            this.GridViewColumnHeaderClickedHandler(null, null);
        }

        private string _lastHeaderClicked = LanguagesManager.Instance.UploadControl_Name;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                _uploadListView.SelectedIndex = -1;

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

            if (sortBy == LanguagesManager.Instance.UploadControl_Name)
            {
                propertyName = "Name";
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_Length)
            {
                propertyName = "Length";
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_Priority)
            {
                propertyName = "Priority";
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_Rate)
            {
                propertyName = "Rate";
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_State)
            {
                propertyName = "State";
            }

            if (propertyName == null) return;

            _uploadListView.Items.SortDescriptions.Clear();
            _uploadListView.Items.SortDescriptions.Add(new SortDescription(propertyName, direction));
        }

        #endregion

        private class UploadListViewItem : INotifyPropertyChanged
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
            private UploadState _state = 0;
            private long _length = 0;
            private int _priority = 0;
            private double _rate = 0;
            private string _rateText = null;
            private Seed _value = null;

            public UploadListViewItem(Information information)
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

                    if (_information.Contains("Name")) this.Name = (string)_information["Name"];
                    else this.Name = null;

                    if (_information.Contains("State")) this.State = (UploadState)_information["State"];
                    else this.State = 0;

                    if (_information.Contains("Length")) this.Length = (long)_information["Length"];
                    else this.Length = 0;

                    if (_information.Contains("Priority")) this.Priority = (int)_information["Priority"];
                    else this.Priority = 0;

                    if (_information.Contains("State"))
                    {
                        if (_information.Contains("UploadBlockCount") && _information.Contains("BlockCount")
                            && (UploadState)_information["State"] == UploadState.Uploading
                            && (int)_information["UploadBlockCount"] != 0)
                        {
                            this.Rate = Math.Round(((double)(int)_information["UploadBlockCount"] / (double)(int)_information["BlockCount"]) * 100, 2);
                        }
                        else if (_information.Contains("EncodeBytes") && _information.Contains("EncodingBytes")
                            && ((UploadState)_information["State"] == UploadState.ComputeHash || (UploadState)_information["State"] == UploadState.Encoding || (UploadState)_information["State"] == UploadState.ComputeCorrection)
                            && (long)_information["EncodingBytes"] != 0)
                        {
                            this.Rate = Math.Round(((double)(long)_information["EncodingBytes"] / (double)(long)_information["EncodeBytes"]) * 100, 2);
                        }
                        else if ((UploadState)_information["State"] == UploadState.Completed)
                        {
                            this.Rate = 100;
                        }
                        else
                        {
                            this.Rate = 0;
                        }
                    }
                    else
                    {
                        this.Rate = 0;
                    }

                    if (_information.Contains("State"))
                    {
                        if (_information.Contains("UploadBlockCount") && _information.Contains("BlockCount")
                            && ((UploadState)_information["State"] == UploadState.Uploading || (UploadState)_information["State"] == UploadState.Completed))
                        {
                            this.RateText = string.Format("{0}% {1}/{2}",
                                this.Rate,
                                (int)_information["UploadBlockCount"],
                               (int)_information["BlockCount"]);
                        }
                        else if (_information.Contains("EncodeBytes") && _information.Contains("EncodingBytes") && _information.Contains("Rank")
                            && ((UploadState)_information["State"] == UploadState.ComputeHash || (UploadState)_information["State"] == UploadState.Encoding || (UploadState)_information["State"] == UploadState.ComputeCorrection))
                        {
                            this.RateText = string.Format("{0}% {1}/{2} [{3}]",
                                this.Rate,
                                NetworkConverter.ToSizeString((long)_information["EncodingBytes"]),
                                NetworkConverter.ToSizeString((long)_information["EncodeBytes"]),
                               (int)_information["Rank"]);
                        }
                        else
                        {
                            this.RateText = null;
                        }
                    }
                    else
                    {
                        this.RateText = null;
                    }

                    if (_information.Contains("Seed")) this.Value = (Seed)_information["Seed"];
                    else this.Value = null;
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

            public UploadState State
            {
                get
                {
                    return _state;
                }
                set
                {
                    if (value != _state)
                    {
                        _state = value;

                        this.NotifyPropertyChanged("State");
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

            public int Priority
            {
                get
                {
                    return _priority;
                }
                set
                {
                    if (value != _priority)
                    {
                        _priority = value;

                        this.NotifyPropertyChanged("Priority");
                    }
                }
            }

            public double Rate
            {
                get
                {
                    return _rate;
                }
                set
                {
                    if (value != _rate)
                    {
                        _rate = value;

                        this.NotifyPropertyChanged("Rate");
                    }
                }
            }

            public string RateText
            {
                get
                {
                    return _rateText;
                }
                set
                {
                    if (value != _rateText)
                    {
                        _rateText = value;

                        this.NotifyPropertyChanged("RateText");
                    }
                }
            }

            public Seed Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    if (value != _value)
                    {
                        _value = value;

                        this.NotifyPropertyChanged("Value");
                    }
                }
            }
        }
    }
}
