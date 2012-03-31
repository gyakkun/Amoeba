using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// DownloadControl.xaml の相互作用ロジック
    /// </summary>
    partial class DownloadControl : UserControl
    {
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollection<DownloadListViewItem> _downloadListViewItemCollection = new ObservableCollection<DownloadListViewItem>();
        private object _listLock = new object();

        public DownloadControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            InitializeComponent();

            _downloadListView.ItemsSource = _downloadListViewItemCollection;

            ThreadPool.QueueUserWorkItem(new WaitCallback(this.DownloadItemShow), this);
        }

        private void DownloadItemShow(object state)
        {
            for (; ; )
            {
                Thread.Sleep(1000 * 3);

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<object>(delegate(object state2)
                {
                    try
                    {
                        var downloadingInformation = _amoebaManager.DownloadingInformation.ToArray();

                        foreach (var item in _downloadListViewItemCollection.ToArray())
                        {
                            if (!downloadingInformation.Any(n => (int)n["Id"] == (int)item.Information["Id"]))
                            {
                                _downloadListViewItemCollection.Remove(item);
                            }
                        }

                        foreach (var information in downloadingInformation)
                        {
                            var item = _downloadListViewItemCollection.FirstOrDefault(n => (int)n.Information["Id"] == (int)information["Id"]);

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
                                _downloadListViewItemCollection.Add(new DownloadListViewItem(information));

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

        private void _downloadListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_downloadListView.GetCurrentIndex(e.GetPosition) == -1)
            {
                _downloadListView.SelectedItems.Clear();
            }
        }

        private void _downloadListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_downloadListViewDeleteMenuItem != null) _downloadListViewDeleteMenuItem.IsEnabled = (_downloadListView.SelectedItems.Count > 0);
            if (_downloadListViewCopyMenuItem != null) _downloadListViewCopyMenuItem.IsEnabled = (_downloadListView.SelectedItems.Count > 0);
            if (_downloadListViewCopyInfoMenuItem != null) _downloadListViewCopyInfoMenuItem.IsEnabled = (_downloadListView.SelectedItems.Count > 0);
            if (_downloadListViewPriorityMenuItem != null) _downloadListViewPriorityMenuItem.IsEnabled = (_downloadListView.SelectedItems.Count > 0);
        }

        private void _downloadListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var downloadItems = _downloadListView.SelectedItems;
            if (downloadItems == null) return;

            foreach (var item in downloadItems.Cast<DownloadListViewItem>())
            {
                _amoebaManager.DownloadRemove((int)item.Information["Id"]);
            }
        }

        private void _downloadListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var downloadItems = _downloadListView.SelectedItems;
            if (downloadItems == null) return;

            var sb = new StringBuilder();

            foreach (var item in downloadItems.Cast<DownloadListViewItem>())
            {
                if (item.Value != null) sb.AppendLine(AmoebaConverter.ToSeedString(item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _downloadListViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _downloadListView.SelectedItems;
            if (selectItems == null) return;

            var item = selectItems.Cast<DownloadListViewItem>().FirstOrDefault();
            if (item == null || item.Value == null) return;

            try
            {
                Clipboard.SetText(MessageConverter.ToInfoMessage(item.Value));
            }
            catch (Exception)
            {

            }
        }

        private void _downloadListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Clipboard.GetSeeds())
            {
                _amoebaManager.Download(item);
            }
        }

        private void _downloadListViewResetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var downloadItems = _downloadListView.SelectedItems;
            if (downloadItems == null) return;

            foreach (var item in downloadItems.Cast<DownloadListViewItem>())
            {
                _amoebaManager.DownloadRemove((int)item.Information["Id"]);
            }


            foreach (var item in downloadItems.Cast<DownloadListViewItem>())
            {
                _amoebaManager.Download((Seed)item.Information["Seed"]);
            }
        }

        private void SetPriority(int i)
        {
            var downloadItems = _downloadListView.SelectedItems;
            if (downloadItems == null) return;

            foreach (var item in downloadItems.Cast<DownloadListViewItem>())
            {
                _amoebaManager.SetDownloadPriority((int)item.Information["Id"], i);
            }
        }

        #region Priority

        private void _downloadListViewPriority0MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(0);
        }

        private void _downloadListViewPriority1MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(1);
        }

        private void _downloadListViewPriority2MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(2);
        }

        private void _downloadListViewPriority3MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(3);
        }

        private void _downloadListViewPriority4MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(4);
        }

        private void _downloadListViewPriority5MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(5);
        }

        private void _downloadListViewPriority6MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(6);
        }

        #endregion

        #region Sort

        private void Sort()
        {
            this.GridViewColumnHeaderClickedHandler(null, null);
        }

        private string _lastHeaderClicked = LanguagesManager.Instance.DownloadControl_Name;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                _downloadListView.SelectedIndex = -1;

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

            if (sortBy == LanguagesManager.Instance.DownloadControl_Name)
            {
                propertyName = "Name";
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Length)
            {
                propertyName = "Length";
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Priority)
            {
                propertyName = "Priority";
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Rate)
            {
                propertyName = "Rate";
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Path)
            {
                propertyName = "Path";
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_State)
            {
                propertyName = "State";
            }

            if (propertyName == null) return;

            _downloadListView.Items.SortDescriptions.Clear();
            _downloadListView.Items.SortDescriptions.Add(new SortDescription(propertyName, direction));
        }

        #endregion

        private class DownloadListViewItem : INotifyPropertyChanged
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
            private DownloadState _state = 0;
            private long _length = 0;
            private int _priority = 0;
            private double _rate = 0;
            private string _rateText = null;
            private string _path = null;
            private Seed _value = null;

            public DownloadListViewItem(Information information)
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

                    if (_information.Contains("Length")) this.Length = (long)_information["Length"];
                    else this.Length = 0;

                    if (_information.Contains("State")) this.State = (DownloadState)_information["State"];
                    else this.State = 0;

                    if (_information.Contains("Priority")) this.Priority = (int)_information["Priority"];
                    else this.Priority = 0;

                    if (_information.Contains("DownloadBlockCount") && _information.Contains("BlockCount")
                        && _information.Contains("ParityBlockCount"))
                    {
                        this.Rate = Math.Round(((double)(int)_information["DownloadBlockCount"] / (double)((int)_information["BlockCount"] - (int)_information["ParityBlockCount"])) * 100, 2);
                    }
                    else
                    {
                        this.Rate = 0;
                    }

                    if (_information.Contains("DownloadBlockCount") && _information.Contains("BlockCount")
                        && _information.Contains("ParityBlockCount") && _information.Contains("Rank")
                        && _information.Contains("Seed"))
                    {
                        if (0 == (int)_information["ParityBlockCount"])
                        {
                            this.RateText = string.Format("{0}% {1}/{2} [{3}/{4}]",
                                this.Rate,
                                (int)_information["DownloadBlockCount"],
                                ((int)_information["BlockCount"] - (int)_information["ParityBlockCount"]),
                                (int)_information["Rank"],
                                ((Seed)_information["Seed"]).Rank);
                        }
                        else
                        {
                            this.RateText = string.Format("{0}% {1}/{2}({3}) [{4}/{5}]",
                                this.Rate,
                                (int)_information["DownloadBlockCount"],
                                ((int)_information["BlockCount"] - (int)_information["ParityBlockCount"]),
                                (int)_information["BlockCount"],
                                (int)_information["Rank"],
                                ((Seed)_information["Seed"]).Rank);
                        }
                    }
                    else
                    {
                        this.RateText = "";
                    }

                    if (_information.Contains("Path")) this.Path = (string)_information["Path"];
                    else this.Path = null;

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
                        _name = value; this.NotifyPropertyChanged("Name");
                    }
                }
            }

            public DownloadState State
            {
                get
                {
                    return _state;
                }
                set
                {
                    if (value != _state)
                    {
                        _state = value; this.NotifyPropertyChanged("State");
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
                        _length = value; this.NotifyPropertyChanged("Length");
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
                        _priority = value; this.NotifyPropertyChanged("Priority");
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
                        _rate = value; this.NotifyPropertyChanged("Rate");
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
                        _rateText = value; this.NotifyPropertyChanged("RateText");
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
                        _path = value; this.NotifyPropertyChanged("Path");
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
                        _value = value; this.NotifyPropertyChanged("Value");
                    }
                }
            }
        }
    }
}
