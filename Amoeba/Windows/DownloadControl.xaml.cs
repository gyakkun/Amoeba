using System;
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
    /// DownloadControl.xaml の相互作用ロジック
    /// </summary>
    partial class DownloadControl : UserControl
    {
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollection<DownloadListViewItem> _listViewItemCollection = new ObservableCollection<DownloadListViewItem>();
        private object _listLock = new object();

        private Thread _showDownloadItemThread;
        private Thread _watchThread;

        public DownloadControl(MainWindow mainWindow, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            InitializeComponent();

            _listView.ItemsSource = _listViewItemCollection;

            _showDownloadItemThread = new Thread(new ThreadStart(this.ShowDownloadItem));
            _showDownloadItemThread.Priority = ThreadPriority.Highest;
            _showDownloadItemThread.IsBackground = true;
            _showDownloadItemThread.Name = "ShowDownloadItemThread";
            _showDownloadItemThread.Start();

            _watchThread = new Thread(new ThreadStart(this.Watch));
            _watchThread.Priority = ThreadPriority.Highest;
            _watchThread.IsBackground = true;
            _watchThread.Name = "WatchThread";
            _watchThread.Start();

            LanguagesManager.UsingLanguageChangedEvent += new UsingLanguageChangedEventHandler(this.LanguagesManager_UsingLanguageChangedEvent);
        }

        private void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {
            _listView.Items.Refresh();
        }

        private void ShowDownloadItem()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(100);
                    if (App.SelectTab != "Download") continue;
                    
                    var downloadingInformation = _amoebaManager.DownloadingInformation.ToArray();
                    Dictionary<int, Information> dic = new Dictionary<int, Information>();

                    foreach (var item in downloadingInformation.ToArray())
                    {
                        dic[(int)item["Id"]] = item;
                    }

                    Dictionary<int, DownloadListViewItem> dic2 = new Dictionary<int, DownloadListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in _listViewItemCollection.ToArray())
                        {
                            dic2[item.Id] = item;
                        }
                    }), null);

                    List<DownloadListViewItem> removeList = new List<DownloadListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in _listViewItemCollection.ToArray())
                        {
                            if (!dic.ContainsKey(item.Id))
                            {
                                removeList.Add(item);
                            }
                        }
                    }), null);

                    List<DownloadListViewItem> newList = new List<DownloadListViewItem>();
                    Dictionary<DownloadListViewItem, Information> updateDic = new Dictionary<DownloadListViewItem, Information>();
                    bool clearFlag = false;
                    var selectItems = new List<DownloadListViewItem>();

                    if (removeList.Count > 100)
                    {
                        clearFlag = true;
                        removeList.Clear();
                        updateDic.Clear();

                        foreach (var information in downloadingInformation)
                        {
                            newList.Add(new DownloadListViewItem(information));
                        }

                        HashSet<int> hid = new HashSet<int>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            hid.UnionWith(_listView.SelectedItems.OfType<DownloadListViewItem>().Select(n => n.Id));
                        }), null);

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
                        foreach (var information in downloadingInformation)
                        {
                            DownloadListViewItem item = null;

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
                                newList.Add(new DownloadListViewItem(information));
                            }
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
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

                        if (sortFlag && _listViewItemCollection.Count < 3000) this.Sort();
                    }), null);

                    Thread.Sleep(1000 * 3);
                }
            }
            catch (Exception)
            {

            }
        }

        private void Watch()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(1000 * 3);

                    if (!Directory.Exists(App.DirectoryPaths["Input"])) continue;

                    foreach (var filePath in Directory.GetFiles(App.DirectoryPaths["Input"]))
                    {
                        if (!System.IO.Path.GetFileName(filePath).StartsWith("seed") || !filePath.EndsWith(".txt")) continue;

                        try
                        {
                            using (FileStream stream = new FileStream(filePath, FileMode.Open))
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                try
                                {
                                    var seed = AmoebaConverter.FromSeedString(reader.ReadLine());
                                    if (!seed.VerifyCertificate()) seed.CreateCertificate(null);

                                    var path = reader.ReadLine();

                                    _amoebaManager.Download(seed, path, 3);
                                }
                                catch (Exception)
                                {

                                }
                            }
                        }
                        catch (IOException)
                        {
                            continue;
                        }
                        catch (Exception)
                        {

                        }

                        try
                        {
                            File.Delete(filePath);
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _listView.SelectedItems;

            if (!_listViewDeleteMenuItem_IsEnabled) _listViewDeleteMenuItem.IsEnabled = false;
            else _listViewDeleteMenuItem.IsEnabled = (_listViewItemCollection.Count > 0);

            _listViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewCopyInfoMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewResetMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewPriorityMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            
            if (!_listViewDeleteCompleteMenuItem_IsEnabled) _listViewDeleteCompleteMenuItem.IsEnabled = false;
            else _listViewDeleteCompleteMenuItem.IsEnabled = _listViewItemCollection.Any(n => n.State == DownloadState.Completed);

            {
                var seeds = Clipboard.GetSeeds();

                _listViewPasteMenuItem.IsEnabled = (seeds.Count() > 0) ? true : false;
            }
        }

        volatile bool _listViewDeleteMenuItem_IsEnabled = true;

        private void _listViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _listView.SelectedItems;
            if (selectItems == null || selectItems.Count == 0) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Download", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            List<int> ids = new List<int>();

            foreach (var item in selectItems.Cast<DownloadListViewItem>())
            {
                ids.Add(item.Id);
            }
            
            _listViewDeleteMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in ids)
                    {
                        _amoebaManager.RemoveDownload(item);
                    }
                }
                catch (Exception)
                {

                }

                _listViewDeleteMenuItem_IsEnabled = true;
            }));
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _listView.SelectedItems;
            if (selectItems == null) return;

            var sb = new StringBuilder();

            foreach (var seed in selectItems.Cast<DownloadListViewItem>().Select(n => n.Value))
            {
                if (seed == null) continue;

                sb.AppendLine(AmoebaConverter.ToSeedString(seed));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _listViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _listView.SelectedItems;
            if (selectItems == null) return;

            var sb = new StringBuilder();

            foreach (var seed in selectItems.Cast<DownloadListViewItem>().Select(n=>n.Value))
            {
                if (seed == null) continue;

                sb.AppendLine(AmoebaConverter.ToSeedString(seed));
                sb.AppendLine(MessageConverter.ToInfoMessage(seed));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        private void _listViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Clipboard.GetSeeds())
            {
                _amoebaManager.Download(item, 3);
            }
        }

        private void SetPriority(int i)
        {
            var downloadItems = _listView.SelectedItems;
            if (downloadItems == null) return;

            foreach (var item in downloadItems.Cast<DownloadListViewItem>())
            {
                try
                {
                    _amoebaManager.SetDownloadPriority(item.Id, i);
                }
                catch (Exception)
                {

                }
            }
        }

        #region Priority

        private void _listViewPriority0MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(0);
        }

        private void _listViewPriority1MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(1);
        }

        private void _listViewPriority2MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(2);
        }

        private void _listViewPriority3MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(3);
        }

        private void _listViewPriority4MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(4);
        }

        private void _listViewPriority5MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(5);
        }

        private void _listViewPriority6MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.SetPriority(6);
        }

        #endregion

        private void _listViewResetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var downloadItems = _listView.SelectedItems;
            if (downloadItems == null) return;

            foreach (var item in downloadItems.Cast<DownloadListViewItem>())
            {
                try
                {
                    _amoebaManager.ResetDownload(item.Id);
                }
                catch (Exception)
                {

                }
            }
        }

        volatile bool _listViewDeleteCompleteMenuItem_IsEnabled = true;

        private void _listViewDeleteCompleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _listViewDeleteCompleteMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                var downloadingInformation = _amoebaManager.DownloadingInformation.ToArray();

                foreach (var item in downloadingInformation)
                {
                    if (item.Contains("State") && DownloadState.Completed == (DownloadState)item["State"])
                    {
                        try
                        {
                            _amoebaManager.RemoveDownload((int)item["Id"]);
                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                _listViewDeleteCompleteMenuItem_IsEnabled = true;
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
                var item = e.OriginalSource as GridViewColumnHeader;
                if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

                string headerClicked = item.Column.Header as string;
                if (headerClicked == null) return;

                ListSortDirection direction;

                if (headerClicked != Settings.Instance.DownloadControl_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.DownloadControl_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                Settings.Instance.DownloadControl_LastHeaderClicked = headerClicked;
                Settings.Instance.DownloadControl_ListSortDirection = direction;
            }
            else
            {
                _listView.Items.SortDescriptions.Clear();
                
                if (Settings.Instance.DownloadControl_LastHeaderClicked != null)
                {
                    var list = Sort(_listViewItemCollection, Settings.Instance.DownloadControl_LastHeaderClicked, Settings.Instance.DownloadControl_ListSortDirection).ToList();

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
            _listView.Items.SortDescriptions.Add(new SortDescription("Id", direction));

            if (sortBy == LanguagesManager.Instance.DownloadControl_Name)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Name", direction));
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Length)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Length", direction));
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Priority)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Priority", direction));
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Rate)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("State", direction));
                _listView.Items.SortDescriptions.Add(new SortDescription("Rank", direction));
                _listView.Items.SortDescriptions.Add(new SortDescription("Rate", direction));
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Path)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Path", direction));
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_State)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("State", direction));
            }
        }

        private IEnumerable<DownloadListViewItem> Sort(IEnumerable<DownloadListViewItem> collection, string sortBy, ListSortDirection direction)
        {
            List<DownloadListViewItem> list = new List<DownloadListViewItem>(collection);

            if (sortBy == LanguagesManager.Instance.DownloadControl_Name)
            {
                list.Sort(delegate(DownloadListViewItem x, DownloadListViewItem y)
                {
                    int c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Length)
            {
                list.Sort(delegate(DownloadListViewItem x, DownloadListViewItem y)
                {
                    int c = x.Length.CompareTo(y.Length);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Priority)
            {
                list.Sort(delegate(DownloadListViewItem x, DownloadListViewItem y)
                {
                    int c = x.Priority.CompareTo(y.Priority);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Rate)
            {
                list.Sort(delegate(DownloadListViewItem x, DownloadListViewItem y)
                {
                    int c = x.State.CompareTo(y.State);
                    if (c != 0) return c;
                    c = x.Rank.CompareTo(y.Rank);
                    if (c != 0) return c;
                    c = x.Rate.CompareTo(y.Rate);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Path)
            {
                list.Sort(delegate(DownloadListViewItem x, DownloadListViewItem y)
                {
                    int c = x.Path.CompareTo(y.Path);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_State)
            {
                list.Sort(delegate(DownloadListViewItem x, DownloadListViewItem y)
                {
                    int c = x.State.CompareTo(y.State);
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

            private int _id;
            private Information _information;
            private int _rank = 0;
            private string _name = null;
            private string _path = null;
            private DownloadState _state = 0;
            private long _length = 0;
            private int _priority = 0;
            private double _rate = 0;
            private string _rateText = null;
            private Seed _value = null;

            public DownloadListViewItem(Information information)
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

                    if (_information.Contains("Rank")) this.Rank = (int)_information["Rank"];
                    else this.Rank = 0;

                    if (_information.Contains("Name")) this.Name = (string)_information["Name"];
                    else this.Name = null;

                    if (_information.Contains("Path")) this.Path = (string)_information["Path"];
                    else this.Path = null;

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

                    if (_information.Contains("Seed")) this.Value = (Seed)_information["Seed"];
                    else this.Value = null;
                }
            }

            public int Rank
            {
                get
                {
                    return _rank;
                }
                set
                {
                    if (value != _rank)
                    {
                        _rank = value; 
                        
                        this.NotifyPropertyChanged("Rank");
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

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            _listViewDeleteMenuItem_Click(null, null);
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
