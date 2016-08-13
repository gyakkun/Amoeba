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

        private ObservableCollectionEx<AmoebaInfomationListViewModel> _infomationListViewItemCollection = new ObservableCollectionEx<AmoebaInfomationListViewModel>();
        private ObservableCollectionEx<ConnectionListViewModel> _listViewModelCollection = new ObservableCollectionEx<ConnectionListViewModel>();

        private Thread _showAmoebaInfomationThread;
        private Thread _showConnectionInfomationwThread;

        public InformationControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _listView.ItemsSource = _listViewModelCollection;

#if DEBUG
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_BufferManagerSize" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel());
#endif

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_SentByteCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_ReceivedByteCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel());

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_CreateConnectionCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_AcceptConnectionCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_BlockedConnectionCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel());

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_SurroundingNodeCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_RelayBlockCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel());

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_LockSpace" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_FreeSpace" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_UsingSpace" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel());

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_NodeCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_BlockCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_SeedCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_DownloadCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_UploadCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_ShareCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel());

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_PushNodeCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_PushBlockLinkCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_PushBlockRequestCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_PushBlockCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_PushSeedRequestCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_PushSeedCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel());

            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_PullNodeCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_PullBlockLinkCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_PullBlockRequestCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_PullBlockCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_PullSeedRequestCount" });
            _infomationListViewItemCollection.Add(new AmoebaInfomationListViewModel() { Id = "InformationControl_PullSeedCount" });

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
                for (;;)
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
                    dic["InformationControl_BlockCount"] = ((int)information["BlockCount"]).ToString();
                    dic["InformationControl_SeedCount"] = ((int)information["SeedCount"]).ToString();
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

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
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
                for (;;)
                {
                    var connectionInformation = _amoebaManager.ConnectionInformation.ToArray();
                    var dic = new Dictionary<int, Information>();

                    foreach (var item in connectionInformation.ToArray())
                    {
                        dic[(int)item["Id"]] = item;
                    }

                    var dic2 = new Dictionary<int, ConnectionListViewModel>();

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        foreach (var item in _listViewModelCollection.ToArray())
                        {
                            dic2[item.Id] = item;
                        }
                    }));

                    var removeList = new List<ConnectionListViewModel>();

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        foreach (var item in _listViewModelCollection.ToArray())
                        {
                            if (!dic.ContainsKey(item.Id))
                            {
                                removeList.Add(item);
                            }
                        }
                    }));

                    var newList = new List<ConnectionListViewModel>();
                    var updateDic = new Dictionary<ConnectionListViewModel, Information>();

                    bool clearFlag = false;
                    var selectItems = new List<ConnectionListViewModel>();

                    if (removeList.Count > 100)
                    {
                        clearFlag = true;
                        removeList.Clear();
                        updateDic.Clear();

                        foreach (var information in connectionInformation)
                        {
                            newList.Add(new ConnectionListViewModel(information));
                        }

                        var hid = new HashSet<int>();

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            hid.UnionWith(_listView.SelectedItems.OfType<ConnectionListViewModel>().Select(n => n.Id));
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
                            ConnectionListViewModel item = null;

                            if (dic2.ContainsKey((int)information["Id"]))
                                item = dic2[(int)information["Id"]];

                            if (item != null)
                            {
                                if (!CollectionUtils.Equals(item.Information, information))
                                {
                                    updateDic[item] = information;
                                }
                            }
                            else
                            {
                                newList.Add(new ConnectionListViewModel(information));
                            }
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        bool sortFlag = false;

                        if (newList.Count != 0) sortFlag = true;
                        if (removeList.Count != 0) sortFlag = true;
                        if (updateDic.Count != 0) sortFlag = true;

                        if (clearFlag) _listViewModelCollection.Clear();

                        foreach (var item in newList)
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

        private void _infomationListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _infomationListView.SelectedItems;

            _infomationListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
        }

        private void _infomationListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _infomationListView.SelectedItems;
            if (selectItems == null) return;

            var sb = new StringBuilder();

            foreach (var item in selectItems.OfType<AmoebaInfomationListViewModel>())
            {
                if (item.Id == null) sb.AppendLine();
                else sb.AppendLine(string.Format("{0}: {1}", item.Name, item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _listView.SelectedItems;

            _listViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _listViewPasteMenuItem.IsEnabled = Clipboard.ContainsNodes();
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _listView.SelectedItems;
            if (selectItems == null) return;

            var nodes = new List<Node>();

            foreach (var information in selectItems.OfType<ConnectionListViewModel>().Select(n => n.Information))
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
            _listView.Items.SortDescriptions.Clear();

            if (Settings.Instance.InformationControl_LastHeaderClicked != null)
            {
                var list = this.Sort(_listViewModelCollection, Settings.Instance.InformationControl_LastHeaderClicked, Settings.Instance.InformationControl_ListSortDirection).ToList();

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

            this.Sort(headerClicked, direction);

            Settings.Instance.InformationControl_LastHeaderClicked = headerClicked;
            Settings.Instance.InformationControl_ListSortDirection = direction;
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

        private IEnumerable<ConnectionListViewModel> Sort(IEnumerable<ConnectionListViewModel> collection, string sortBy, ListSortDirection direction)
        {
            var list = new List<ConnectionListViewModel>(collection);

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

        private class AmoebaInfomationListViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(string info)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
            }

            public AmoebaInfomationListViewModel()
            {
                LanguagesManager.UsingLanguageChangedEvent += this.LanguagesManager_UsingLanguageChangedEvent;
            }

            void LanguagesManager_UsingLanguageChangedEvent(object sender)
            {
                this.NotifyPropertyChanged(nameof(this.Name));
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

                        this.NotifyPropertyChanged(nameof(this.Id));
                        this.NotifyPropertyChanged(nameof(this.Name));
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

                        this.NotifyPropertyChanged(nameof(this.Value));
                    }
                }
            }
        }

        private class ConnectionListViewModel : INotifyPropertyChanged
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

            public ConnectionListViewModel(Information information)
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

                        this.NotifyPropertyChanged(nameof(this.Uri));
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

                        this.NotifyPropertyChanged(nameof(this.Priority));
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

                        this.NotifyPropertyChanged(nameof(this.ReceivedByteCount));
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

                        this.NotifyPropertyChanged(nameof(this.SentByteCount));
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

                        this.NotifyPropertyChanged(nameof(this.Direction));
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
