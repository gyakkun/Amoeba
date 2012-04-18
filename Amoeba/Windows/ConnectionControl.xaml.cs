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

            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_SentByteCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_ReceivedByteCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem());

            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_CreateConnectionCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_AcceptConnectionCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem());

            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_NodeCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_SeedCount" });
            _amoebaInfomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "ConnectionControl_CacheSeedCount" });
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

        private void _connectionsInfomationListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_connectionsInfomationListView.GetCurrentIndex(e.GetPosition) == -1)
            {
                _connectionsInfomationListView.SelectedItems.Clear();
            }
        }

        private void _connectionListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_connectionListView.GetCurrentIndex(e.GetPosition) == -1)
            {
                _connectionListView.SelectedItems.Clear();
            }
        }

        private void _connectionListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _connectionListView.SelectedItems;
            if (selectItems == null) return;
            
            _connectionListViewCopyMenuItem.IsEnabled = (selectItems.Count > 0);
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
            try
            {
                for (; ; )
                {
                    var information = _amoebaManager.Information;
                    Dictionary<string, string> dic = new Dictionary<string, string>();

                    dic["ConnectionControl_CreateConnectionCount"] = ((int)information["CreateConnectionCount"]).ToString();
                    dic["ConnectionControl_AcceptConnectionCount"] = ((int)information["AcceptConnectionCount"]).ToString();

                    dic["ConnectionControl_SentByteCount"] = NetworkConverter.ToSizeString(_amoebaManager.SentByteCount);
                    dic["ConnectionControl_ReceivedByteCount"] = NetworkConverter.ToSizeString(_amoebaManager.ReceivedByteCount);

                    dic["ConnectionControl_PullNodesRequestCount"] = ((int)information["PullNodesRequestCount"]).ToString();
                    dic["ConnectionControl_PullNodesCount"] = ((int)information["PullNodesCount"]).ToString();
                    dic["ConnectionControl_PullSeedsLinkCount"] = ((int)information["PullSeedsLinkCount"]).ToString();
                    dic["ConnectionControl_PullSeedsRequestCount"] = ((int)information["PullSeedsRequestCount"]).ToString();
                    dic["ConnectionControl_PullSeedsCount"] = ((int)information["PullSeedsCount"]).ToString();
                    dic["ConnectionControl_PullBlocksLinkCount"] = ((int)information["PullBlocksLinkCount"]).ToString();
                    dic["ConnectionControl_PullBlocksRequestCount"] = ((int)information["PullBlocksRequestCount"]).ToString();
                    dic["ConnectionControl_PullBlockCount"] = ((int)information["PullBlockCount"]).ToString();

                    dic["ConnectionControl_PushNodesRequestCount"] = ((int)information["PushNodesRequestCount"]).ToString();
                    dic["ConnectionControl_PushNodesCount"] = ((int)information["PushNodesCount"]).ToString();
                    dic["ConnectionControl_PushSeedsLinkCount"] = ((int)information["PushSeedsLinkCount"]).ToString();
                    dic["ConnectionControl_PushSeedsRequestCount"] = ((int)information["PushSeedsRequestCount"]).ToString();
                    dic["ConnectionControl_PushSeedsCount"] = ((int)information["PushSeedsCount"]).ToString();
                    dic["ConnectionControl_PushBlocksLinkCount"] = ((int)information["PushBlocksLinkCount"]).ToString();
                    dic["ConnectionControl_PushBlocksRequestCount"] = ((int)information["PushBlocksRequestCount"]).ToString();
                    dic["ConnectionControl_PushBlockCount"] = ((int)information["PushBlockCount"]).ToString();

                    dic["ConnectionControl_RelayBlockCount"] = ((int)information["RelayBlockCount"]).ToString();

                    dic["ConnectionControl_NodeCount"] = ((int)information["OtherNodeCount"]).ToString();
                    dic["ConnectionControl_SeedCount"] = ((int)information["SeedCount"]).ToString();
                    dic["ConnectionControl_CacheSeedCount"] = ((int)information["CacheSeedCount"]).ToString();

                    dic["ConnectionControl_DownloadCount"] = ((int)information["DownloadingCount"]).ToString();
                    dic["ConnectionControl_UploadCount"] = ((int)information["UploadingCount"]).ToString();
                    dic["ConnectionControl_BlockCount"] = ((int)information["BlockCount"]).ToString();

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in dic)
                        {
                            _amoebaInfomationListViewItemCollection.First(n => n.Id == item.Key).Value = item.Value;
                        }
                    }), null);

                    Thread.Sleep(1000 * 10);
                }
            }
            catch (Exception)
            {

            }
        }

        private void ConnectionInfomationShow(object state)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Thread.CurrentThread.IsBackground = true;

            try
            {
                for (; ; )
                {
                    Thread.Sleep(1000 * 3);
                    if (App.SelectTab != "Connection") continue;

                    var connectionInformation = _amoebaManager.ConnectionInformation.ToArray();
                    Dictionary<int, Information> dic = new Dictionary<int, Information>();

                    foreach (var item in connectionInformation.ToArray())
                    {
                        dic[(int)item["Id"]] = item;
                    }

                    Dictionary<int, ConnectionListViewItem> dic2 = new Dictionary<int, ConnectionListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in _connectionListViewItemCollection.ToArray())
                        {
                            dic2[(int)item.Information["Id"]] = item;
                        }
                    }), null);

                    List<ConnectionListViewItem> removeList = new List<ConnectionListViewItem>();
                    Dictionary<ConnectionListViewItem, Information> updateDic = new Dictionary<ConnectionListViewItem, Information>();
                    List<ConnectionListViewItem> newList = new List<ConnectionListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in _connectionListViewItemCollection.ToArray())
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
                            _connectionListViewItemCollection.Clear();
                        }
                    }), null);

                    foreach (var information in connectionInformation)
                    {
                        ConnectionListViewItem item = null;

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
                            newList.Add(new ConnectionListViewItem(information));
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<object>(delegate(object state2)
                    {
                        bool sortFlag = false;

                        if (newList.Count != 0) sortFlag = true;
                        if (removeList.Count != 0) sortFlag = true;
                        if (updateDic.Count != 0) sortFlag = true;

                        foreach (var item in newList)
                        {
                            _connectionListViewItemCollection.Add(item);
                            sortFlag = true;
                        }

                        foreach (var item in removeList)
                        {
                            _connectionListViewItemCollection.Remove(item);
                            sortFlag = true;
                        }

                        foreach (var item in updateDic)
                        {
                            item.Key.Information = item.Value;
                            sortFlag = true;
                        }

                        if (sortFlag && _connectionListViewItemCollection.Count < 10000) this.Sort();
                    }), null);
                }
            }
            catch (Exception)
            {

            }
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
                _connectionListView.SelectedIndex = -1;

                var item = e.OriginalSource as GridViewColumnHeader;
                if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

                string headerClicked = item.Column.Header as string;
                if (headerClicked == null) return;

                ListSortDirection direction;

                if (headerClicked != Settings.Instance.ConnectionControl_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.ConnectionControl_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                Settings.Instance.ConnectionControl_LastHeaderClicked = headerClicked;
                Settings.Instance.ConnectionControl_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.ConnectionControl_LastHeaderClicked != null)
                {
                    var list = new List<ConnectionListViewItem>(_connectionListViewItemCollection);
                    var list2 = Sort(list, Settings.Instance.ConnectionControl_LastHeaderClicked, Settings.Instance.ConnectionControl_ListSortDirection).ToList();

                    for (int i = 0; i < list2.Count; i++)
                    {
                        var o = _connectionListViewItemCollection.IndexOf(list2[i]);

                        if (i != o) _connectionListViewItemCollection.Move(o, i);
                    }
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _connectionListView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.ConnectionControl_Uri)
            {
                _connectionListView.Items.SortDescriptions.Add(new SortDescription("Uri", direction));
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_Priority)
            {
                _connectionListView.Items.SortDescriptions.Add(new SortDescription("Priority", direction));
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_ReceivedByteCount)
            {
                _connectionListView.Items.SortDescriptions.Add(new SortDescription("ReceivedByteCount", direction));
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_SentByteCount)
            {
                _connectionListView.Items.SortDescriptions.Add(new SortDescription("SentByteCount", direction));
            }
        }

        private IEnumerable<ConnectionListViewItem> Sort(IEnumerable<ConnectionListViewItem> collection, string sortBy, ListSortDirection direction)
        {
            List<ConnectionListViewItem> list = new List<ConnectionListViewItem>(collection);

            if (sortBy == LanguagesManager.Instance.ConnectionControl_Name)
            {
                list.Sort(delegate(ConnectionListViewItem x, ConnectionListViewItem y)
                {
                    int c = x.Uri.CompareTo(y.Uri);
                    if (c != 0) return c;
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_Priority)
            {
                list.Sort(delegate(ConnectionListViewItem x, ConnectionListViewItem y)
                {
                    int c = x.Priority.CompareTo(y.Priority);
                    if (c != 0) return c;
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_ReceivedByteCount)
            {
                list.Sort(delegate(ConnectionListViewItem x, ConnectionListViewItem y)
                {
                    int c = ((long)x.Information["ReceivedByteCount"]).CompareTo((long)y.Information["ReceivedByteCount"]);
                    if (c != 0) return c;
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_SentByteCount)
            {
                list.Sort(delegate(ConnectionListViewItem x, ConnectionListViewItem y)
                {
                    int c = ((long)x.Information["SentByteCount"]).CompareTo((long)y.Information["SentByteCount"]);
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
