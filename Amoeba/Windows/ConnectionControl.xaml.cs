using System;
using System.Collections.Generic;
using System.Linq;
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
using Library.Net.Amoeba;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using Amoeba.Properties;
using System.Reflection;
using Library.Collections;
using Library;
using System.Collections.ObjectModel;

namespace Amoeba.Windows
{
    /// <summary>
    /// ConnectionControl.xaml の相互作用ロジック
    /// </summary>
    partial class ConnectionControl : UserControl
    {
        private AmoebaManager _amoebaManager;

        private ObservableCollection<AmoebaInfomationListViewItem> _amoebaInfomationListViewItemCollection = new ObservableCollection<AmoebaInfomationListViewItem>();
        private ObservableCollection<ConnectionListViewItem> _connectionListViewItemCollection = new ObservableCollection<ConnectionListViewItem>();

        public ConnectionControl(AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _connectionListView.ItemsSource = _connectionListViewItemCollection;

            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_ReceivedByteCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_SentByteCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem());

            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_CreateConnectionCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_AcceptConnectionCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem());

            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_NodeCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_SeedCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_BlockCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_RelayBlockCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_DownloadCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_UploadCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem());

            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PushNodesRequestCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PushNodesCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PushSeedsLinkCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PushSeedsRequestCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PushSeedsCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PushBlocksLinkCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PushBlocksRequestCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PushBlockCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem());

            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PullNodesRequestCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PullNodesCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PullSeedsLinkCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PullSeedsRequestCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PullSeedsCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PullBlocksLinkCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PullBlocksRequestCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_PullBlockCount" });

            _connectionsInfomationListView.ItemsSource = _amoebaInfomationListViewItemCollection;

            ThreadPool.QueueUserWorkItem(new WaitCallback(this.AmoebaInfomationShow), this);
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ConnectionInfomationShow), this);
        }

        private void _connectionListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_connectionListViewCopyMenuItem != null) _connectionListViewCopyMenuItem.IsEnabled = (_connectionListView.SelectedItems.Count > 0);
        }

        private void _connectionListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _connectionListView.SelectedItems;
            if (selectItems == null) return;

            var item = selectItems.OfType<ConnectionListViewItem>().FirstOrDefault();
            if (item == null) return;

            try
            {
                if (item.Information.Contains("Node"))
                {
                    var node = (Node)item.Information["Node"];

                    Clipboard.SetText(AmoebaConverter.ToNodeString(node));
                }
            }
            catch (Exception)
            {

            }
        }

        private void AmoebaInfomationShow(object state)
        {
            for (; ; )
            {
                Thread.Sleep(1000 * 10);

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<object>(delegate(object state2)
                {
                    var information = _amoebaManager.Information;

                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_CreateConnectionCount").Value = ((int)information["CreateConnectionCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_AcceptConnectionCount").Value = ((int)information["AcceptConnectionCount"]).ToString();

                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_ReceivedByteCount").Value = NetworkConverter.ToSizeString(_amoebaManager.ReceivedByteCount);
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_SentByteCount").Value = NetworkConverter.ToSizeString(_amoebaManager.SentByteCount);

                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PullNodesRequestCount").Value = ((int)information["PullNodesRequestCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PullNodesCount").Value = ((int)information["PullNodesCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PullSeedsLinkCount").Value = ((int)information["PullSeedsLinkCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PullSeedsRequestCount").Value = ((int)information["PullSeedsRequestCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PullSeedsCount").Value = ((int)information["PullSeedsCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PullBlocksLinkCount").Value = ((int)information["PullBlocksLinkCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PullBlocksRequestCount").Value = ((int)information["PullBlocksRequestCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PullBlockCount").Value = ((int)information["PullBlockCount"]).ToString();

                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PushNodesRequestCount").Value = ((int)information["PushNodesRequestCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PushNodesCount").Value = ((int)information["PushNodesCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PushSeedsLinkCount").Value = ((int)information["PushSeedsLinkCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PushSeedsRequestCount").Value = ((int)information["PushSeedsRequestCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PushSeedsCount").Value = ((int)information["PushSeedsCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PushBlocksLinkCount").Value = ((int)information["PushBlocksLinkCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PushBlocksRequestCount").Value = ((int)information["PushBlocksRequestCount"]).ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_PushBlockCount").Value = ((int)information["PushBlockCount"]).ToString();

                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_RelayBlockCount").Value = ((int)information["RelayBlockCount"]).ToString();

                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_NodeCount").Value = _amoebaManager.OtherNodes.Count().ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_SeedCount").Value = _amoebaManager.Seeds.Count().ToString();

                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_DownloadCount").Value = _amoebaManager.DownloadingInformation.Count().ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_UploadCount").Value = _amoebaManager.UploadingInformation.Count().ToString();
                    _amoebaInfomationListViewItemCollection.FirstOrDefault(n => n.Id == "ConnectionControl_BlockCount").Value = ((int)information["BlockCount"]).ToString();
                }), null);
            }
        }

        private void ConnectionInfomationShow(object state)
        {
            for (; ; )
            {
                Thread.Sleep(1000 * 1);

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<object>(delegate(object state2)
                {
                    try
                    {
                        var connectionInformation = _amoebaManager.ConnectionInformation.ToArray();

                        foreach (var item in _connectionListViewItemCollection.ToArray())
                        {
                            if (!connectionInformation.Any(n => (int)n["Id"] == (int)item.Information["Id"]))
                            {
                                _connectionListViewItemCollection.Remove(item);
                            }
                        }

                        foreach (var information in connectionInformation)
                        {
                            var item = _connectionListViewItemCollection.FirstOrDefault(n => (int)n.Information["Id"] == (int)information["Id"]);

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
                                if (information.Contains("Priority") && 0 == (int)information["Priority"])
                                {
                                    _amoebaManager.SetDownloadPriority((int)information["Id"], 3);
                                }

                                _connectionListViewItemCollection.Add(new ConnectionListViewItem(information));

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

        #region Sort

        private void Sort()
        {
            this.GridViewColumnHeaderClickedHandler(null, null);
        }

        private string _lastHeaderClicked = LanguagesManager.Instance.ConnectionControl_Uri;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                _connectionListView.SelectedIndex = -1;

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

            if (sortBy == LanguagesManager.Instance.ConnectionControl_Uri)
            {
                propertyName = "Uri";
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_Priority)
            {
                propertyName = "Priority";
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_ReceivedByteCount)
            {
                propertyName = "receivedByteCount";
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_SentByteCount)
            {
                propertyName = "sentByteCount";
            }

            if (propertyName == null) return;

            _connectionListView.Items.SortDescriptions.Clear();
            _connectionListView.Items.SortDescriptions.Add(new SortDescription(propertyName, direction));
        }

        #endregion

        private void _connectionsInfomationListView_Click(object sender, RoutedEventArgs e)
        {
            _connectionsInfomationListView.SelectedIndex = -1;
        }

        private class AmoebaInfomationListViewItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(string info)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
            }

            private string _id = null;
            private string _value = null;

            public string Id
            {
                get
                {
                    return _id;
                }
                set
                {
                    if (value != _id)
                    {
                        _id = value;

                        this.NotifyPropertyChanged("Id");
                        this.NotifyPropertyChanged("Name");
                    }
                }
            }

            public string Name
            {
                get
                {
                    if (_id != null)
                        return LanguagesManager.Instance.Translate(_id);

                    return null;
                }
            }

            public string Value
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

        private class ConnectionListViewItem : INotifyPropertyChanged
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
            private string _uri = null;
            private int _priority = 0;
            private long _receivedByteCount = 0;
            private long _sentByteCount = 0;

            public ConnectionListViewItem(Information information)
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

                    if (_information.Contains("Uri")) this.Uri = (string)_information["Uri"];
                    else this.Uri = null;

                    if (_information.Contains("Priority")) this.Priority = (int)_information["Priority"];
                    else this.Priority = 0;

                    if (_information.Contains("ReceivedByteCount")) this.ReceivedByteCount = (long)_information["ReceivedByteCount"];
                    else this.ReceivedByteCount = 0;

                    if (_information.Contains("SentByteCount")) this.SentByteCount = (long)_information["SentByteCount"];
                    else this.SentByteCount = 0;
                }
            }

            public string Uri
            {
                get
                {
                    return _uri;
                }
                set
                {
                    if (value != _uri)
                    {
                        _uri = value;

                        this.NotifyPropertyChanged("Uri");
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

            public long ReceivedByteCount
            {
                get
                {
                    return _receivedByteCount;
                }
                set
                {
                    if (value != _receivedByteCount)
                    {
                        _receivedByteCount = value;

                        this.NotifyPropertyChanged("ReceivedByteCount");
                    }
                }
            }

            public long SentByteCount
            {
                get
                {
                    return _sentByteCount;
                }
                set
                {
                    if (value != _sentByteCount)
                    {
                        _sentByteCount = value;

                        this.NotifyPropertyChanged("SentByteCount");
                    }
                }
            }
        }
    }
}
