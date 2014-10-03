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
    /// InformationControl.xaml の相互作用ロジック
    /// </summary>
    partial class InformationControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollectionEx<AmoebaInfomationListViewItem> _infomationListViewItemCollection = new ObservableCollectionEx<AmoebaInfomationListViewItem>();
        private ObservableCollectionEx<ConnectionListViewItem> _listViewItemCollection = new ObservableCollectionEx<ConnectionListViewItem>();

        private Thread _showAmoebaInfomationThread;
        private Thread _showConnectionInfomationwThread;

        public InformationControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _listView.ItemsSource = _listViewItemCollection;

#if DEBUG
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_BufferManagerSize" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem());
#endif

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_SentByteCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_ReceivedByteCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem());

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_CreateConnectionCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_AcceptConnectionCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_BlockedConnectionCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem());

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_SurroundingNodeCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_RelayBlockCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem());

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_LockSpace" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_FreeSpace" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_UsingSpace" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem());

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_NodeCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_SeedCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_BlockCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_DownloadCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_UploadCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_ShareCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem());

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_PushNodeCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_PushBlockLinkCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_PushBlockRequestCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_PushBlockCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_PushSeedRequestCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_PushSeedCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem());

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_PullNodeCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_PullBlockLinkCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_PullBlockRequestCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_PullBlockCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_PullSeedRequestCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewItem() { Id = "InformationControl_PullSeedCount" });

            _infomationListView.ItemsSource = _infomationListViewItemCollection;

            _showAmoebaInfomationThread = new Thread(this.ShowAmoebaInfomation);
            _showAmoebaInfomationThread.Priority = ThreadPriority.Highest;
            _showAmoebaInfomationThread.IsBackground = true;
            _showAmoebaInfomationThread.Name = "InformationControl_ShowAmoebaInfomationThread";
            _showAmoebaInfomationThread.Start();

            _showConnectionInfomationwThread = new Thread(this.ShowConnectionInfomation);
            _showConnectionInfomationwThread.Priority = ThreadPriority.Highest;
            _showConnectionInfomationwThread.IsBackground = true;
            _showConnectionInfomationwThread.Name = "InformationControl_ShowConnectionInfomationThread";
            _showConnectionInfomationwThread.Start();
        }

        private void ShowAmoebaInfomation()
        {
            try
            {
                for (; ; )
                {
                    var information = _amoebaManager.Information;
                    var dic = new Dictionary<string, string>();

#if DEBUG
                    dic["InformationControl_BufferManagerSize"] = NetworkConverter.ToSizeString(_bufferManager.Size);
#endif

                    dic["InformationControl_SentByteCount"] = NetworkConverter.ToSizeString(_amoebaManager.SentByteCount);
                    dic["InformationControl_ReceivedByteCount"] = NetworkConverter.ToSizeString(_amoebaManager.ReceivedByteCount);

                    dic["InformationControl_CreateConnectionCount"] = ((long)information["CreateConnectionCount"]).ToString();
                    dic["InformationControl_AcceptConnectionCount"] = ((long)information["AcceptConnectionCount"]).ToString();
                    dic["InformationControl_BlockedConnectionCount"] = ((long)information["BlockedConnectionCount"]).ToString();

                    dic["InformationControl_SurroundingNodeCount"] = ((int)information["SurroundingNodeCount"]).ToString();
                    dic["InformationControl_RelayBlockCount"] = ((long)information["RelayBlockCount"]).ToString();

                    dic["InformationControl_LockSpace"] = NetworkConverter.ToSizeString(((long)information["LockSpace"])).ToString();
                    dic["InformationControl_FreeSpace"] = NetworkConverter.ToSizeString(((long)information["FreeSpace"])).ToString();
                    dic["InformationControl_UsingSpace"] = NetworkConverter.ToSizeString(((long)information["UsingSpace"])).ToString();

                    dic["InformationControl_NodeCount"] = ((int)information["OtherNodeCount"]).ToString();
                    dic["InformationControl_SeedCount"] = ((int)information["SeedCount"]).ToString();
                    dic["InformationControl_BlockCount"] = ((int)information["BlockCount"]).ToString();
                    dic["InformationControl_DownloadCount"] = ((int)information["DownloadingCount"]).ToString();
                    dic["InformationControl_UploadCount"] = ((int)information["UploadingCount"]).ToString();
                    dic["InformationControl_ShareCount"] = ((int)information["ShareCount"]).ToString();

                    dic["InformationControl_PushNodeCount"] = ((long)information["PushNodeCount"]).ToString();
                    dic["InformationControl_PushBlockLinkCount"] = ((long)information["PushBlockLinkCount"]).ToString();
                    dic["InformationControl_PushBlockRequestCount"] = ((long)information["PushBlockRequestCount"]).ToString();
                    dic["InformationControl_PushBlockCount"] = ((long)information["PushBlockCount"]).ToString();
                    dic["InformationControl_PushSeedRequestCount"] = ((long)information["PushSeedRequestCount"]).ToString();
                    dic["InformationControl_PushSeedCount"] = ((long)information["PushSeedCount"]).ToString();

                    dic["InformationControl_PullNodeCount"] = ((long)information["PullNodeCount"]).ToString();
                    dic["InformationControl_PullBlockLinkCount"] = ((long)information["PullBlockLinkCount"]).ToString();
                    dic["InformationControl_PullBlockRequestCount"] = ((long)information["PullBlockRequestCount"]).ToString();
                    dic["InformationControl_PullBlockCount"] = ((long)information["PullBlockCount"]).ToString();
                    dic["InformationControl_PullSeedRequestCount"] = ((long)information["PullSeedRequestCount"]).ToString();
                    dic["InformationControl_PullSeedCount"] = ((long)information["PullSeedCount"]).ToString();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        foreach (var item in dic)
                        {
                            _infomationListViewItemCollection.First(n => n.Id == item.Key).Value = item.Value;
                        }
                    }));

                    Thread.Sleep(1000 * 10);
                }
            }
            catch (Exception)
            {

            }
        }

        private void ShowConnectionInfomation()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(100);
                    if (_mainWindow.SelectedTab != MainWindowTabType.Information) continue;

                    var connectionInformation = _amoebaManager.ConnectionInformation.ToArray();
                    Dictionary<int, Information> dic = new Dictionary<int, Information>();

                    foreach (var item in connectionInformation.ToArray())
                    {
                        dic[(int)item["Id"]] = item;
                    }

                    Dictionary<int, ConnectionListViewItem> dic2 = new Dictionary<int, ConnectionListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        foreach (var item in _listViewItemCollection.ToArray())
                        {
                            dic2[item.Id] = item;
                        }
                    }));

                    List<ConnectionListViewItem> removeList = new List<ConnectionListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        foreach (var item in _listViewItemCollection.ToArray())
                        {
                            if (!dic.ContainsKey(item.Id))
                            {
                                removeList.Add(item);
                            }
                        }
                    }));

                    List<ConnectionListViewItem> newList = new List<ConnectionListViewItem>();
                    Dictionary<ConnectionListViewItem, Information> updateDic = new Dictionary<ConnectionListViewItem, Information>();

                    bool clearFlag = false;
                    var selectItems = new List<ConnectionListViewItem>();

                    if (removeList.Count > 100)
                    {
                        clearFlag = true;
                        removeList.Clear();
                        updateDic.Clear();

                        foreach (var information in connectionInformation)
                        {
                            newList.Add(new ConnectionListViewItem(information));
                        }

                        HashSet<int> hid = new HashSet<int>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            hid.UnionWith(_listView.SelectedItems.OfType<ConnectionListViewItem>().Select(n => n.Id));
                        }));

                        foreach (var item in newList)
                        {
                            if (hid.Contains(item.Id))
                            {
                                selectItems.Add(item);
                            }
                        }
                    }
                    else
                    {
                        foreach (var information in connectionInformation)
                        {
                            ConnectionListViewItem item = null;

                            if (dic2.ContainsKey((int)information["Id"]))
                                item = dic2[(int)information["Id"]];

                            if (item != null)
                            {
                                if (!CollectionUtilities.Equals(item.Information, information))
                                {
                                    updateDic[item] = information;
                                }
                            }
                            else
                            {
                                newList.Add(new ConnectionListViewItem(information));
                            }
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        bool sortFlag = false;

                        if (newList.Count != 0) sortFlag = true;
                        if (removeList.Count != 0) sortFlag = true;
                        if (updateDic.Count != 0) sortFlag = true;

                        if (clearFlag) _listViewItemCollection.Clear();

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

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _listView.SelectedItems;

            _listViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewPasteMenuItem.IsEnabled = Clipboard.ContainsNodes();
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _listView.SelectedItems;
            if (selectItems == null) return;

            var nodes = new List<Node>();

            foreach (var information in selectItems.OfType<ConnectionListViewItem>().Select(n => n.Information))
            {
                if (information.Contains("Node"))
                {
                    nodes.Add((Node)information["Node"]);
                }
            }

            Clipboard.SetNodes(nodes);
        }

        private void _listViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _amoebaManager.SetOtherNodes(Clipboard.GetNodes());
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
                var item = e.OriginalSource as GridViewColumnHeader;
                if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

                string headerClicked = item.Column.Header as string;
                if (headerClicked == null) return;

                ListSortDirection direction;

                if (headerClicked != Settings.Instance.InformationControl_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.InformationControl_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                Settings.Instance.InformationControl_LastHeaderClicked = headerClicked;
                Settings.Instance.InformationControl_ListSortDirection = direction;
            }
            else
            {
                _listView.Items.SortDescriptions.Clear();

                if (Settings.Instance.InformationControl_LastHeaderClicked != null)
                {
                    var list = Sort(_listViewItemCollection, Settings.Instance.InformationControl_LastHeaderClicked, Settings.Instance.InformationControl_ListSortDirection).ToList();

                    for (int i = 0; i < list.Count; i++)
                    {
                        var o = _listViewItemCollection.IndexOf(list[i]);

                        if (i != o) _listViewItemCollection.Move(o, i);
                    }
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.InformationControl_Direction)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Direction", direction));
            }
            else if (sortBy == LanguagesManager.Instance.InformationControl_Uri)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Uri", direction));
            }
            else if (sortBy == LanguagesManager.Instance.InformationControl_Priority)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Priority", direction));
            }
            else if (sortBy == LanguagesManager.Instance.InformationControl_ReceivedByteCount)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("ReceivedByteCount", direction));
            }
            else if (sortBy == LanguagesManager.Instance.InformationControl_SentByteCount)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("SentByteCount", direction));
            }

            _listView.Items.SortDescriptions.Add(new SortDescription("Id", direction));
        }

        private IEnumerable<ConnectionListViewItem> Sort(IEnumerable<ConnectionListViewItem> collection, string sortBy, ListSortDirection direction)
        {
            List<ConnectionListViewItem> list = new List<ConnectionListViewItem>(collection);

            if (sortBy == LanguagesManager.Instance.InformationControl_Direction)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Direction.CompareTo(y.Direction);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.InformationControl_Uri)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Uri.CompareTo(y.Uri);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.InformationControl_Priority)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Priority.CompareTo(y.Priority);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.InformationControl_ReceivedByteCount)
            {
                list.Sort((x, y) =>
                {
                    int c = x.ReceivedByteCount.CompareTo(y.ReceivedByteCount);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.InformationControl_SentByteCount)
            {
                list.Sort((x, y) =>
                {
                    int c = x.SentByteCount.CompareTo(y.SentByteCount);
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

            public AmoebaInfomationListViewItem()
            {
                LanguagesManager.UsingLanguageChangedEvent += this.LanguagesManager_UsingLanguageChangedEvent;
            }

            void LanguagesManager_UsingLanguageChangedEvent(object sender)
            {
                this.NotifyPropertyChanged("Name");
            }

            private string _id;
            private string _value;

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

            private int _id;
            private Information _information;
            private string _uri;
            private long _priority;
            private long _receivedByteCount;
            private long _sentByteCount;
            private ConnectDirection _direction;

            public ConnectionListViewItem(Information information)
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

                    if (_information.Contains("Direction")) this.Direction = (ConnectDirection)_information["Direction"];
                    else this.Direction = 0;

                    if (_information.Contains("Uri")) this.Uri = (string)_information["Uri"];
                    else this.Uri = null;

                    if (_information.Contains("Priority")) this.Priority = (long)_information["Priority"];
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

            public long Priority
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

            public ConnectDirection Direction
            {
                get
                {
                    return _direction;
                }
                set
                {
                    if (value != _direction)
                    {
                        _direction = value;

                        this.NotifyPropertyChanged("Direction");
                    }
                }
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            _listViewCopyMenuItem_Click(null, null);
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            _listViewPasteMenuItem_Click(null, null);
        }
    }
}
