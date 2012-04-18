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
using System.IO;

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
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.Watch), this);
        }

        private void DownloadItemShow(object state)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Thread.CurrentThread.IsBackground = true;

            try
            {
                for (; ; )
                {
                    Thread.Sleep(1000 * 3);
                    if (App.SelectTab != "Download") continue;

                    var downloadingInformation = _amoebaManager.DownloadingInformation.ToArray();
                    Dictionary<int, Information> dic = new Dictionary<int, Information>();

                    foreach (var item in downloadingInformation.ToArray())
                    {
                        dic[(int)item["Id"]] = item;
                    }

                    Dictionary<int, DownloadListViewItem> dic2 = new Dictionary<int, DownloadListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in _downloadListViewItemCollection.ToArray())
                        {
                            dic2[(int)item.Information["Id"]] = item;
                        }
                    }), null);

                    List<DownloadListViewItem> removeList = new List<DownloadListViewItem>();
                    Dictionary<DownloadListViewItem, Information> updateDic = new Dictionary<DownloadListViewItem, Information>();
                    List<DownloadListViewItem> newList = new List<DownloadListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in _downloadListViewItemCollection.ToArray())
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
                            _downloadListViewItemCollection.Clear();
                        }
                    }), null);

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

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<object>(delegate(object state2)
                    {
                        bool sortFlag = false;

                        if (newList.Count != 0) sortFlag = true;
                        if (removeList.Count != 0) sortFlag = true;
                        if (updateDic.Count != 0) sortFlag = true;

                        foreach (var item in newList)
                        {
                            _downloadListViewItemCollection.Add(item);
                        }

                        foreach (var item in removeList)
                        {
                            _downloadListViewItemCollection.Remove(item);
                        }

                        foreach (var item in updateDic)
                        {
                            item.Key.Information = item.Value;
                        }

                        if (sortFlag && _downloadListViewItemCollection.Count < 10000) this.Sort();
                    }), null);
                }
            }
            catch (Exception)
            {

            }
        }

        private void Watch(object state)
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
                                var item = reader.ReadLine();
                                if (item == null) break;

                                try
                                {
                                    Seed s = AmoebaConverter.FromSeedString(item);
                                    _amoebaManager.Download(s, 0);
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

                        File.Delete(filePath);
                    }
                }
            }
            catch (Exception)
            {

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
            var selectItems = _downloadListView.SelectedItems;
            if (selectItems == null) return;

            _downloadListViewDeleteMenuItem.IsEnabled = (selectItems.Count > 0);
            _downloadListViewCopyMenuItem.IsEnabled = (selectItems.Count > 0);
            _downloadListViewCopyInfoMenuItem.IsEnabled = (selectItems.Count > 0);
            _downloadListViewResetMenuItem.IsEnabled = (selectItems.Count > 0);
            _downloadListViewPriorityMenuItem.IsEnabled = (selectItems.Count > 0);
            _downloadListViewCompleteDeleteMenuItem.IsEnabled = _downloadListViewItemCollection.Any(n => (DownloadState)n.Information["State"] == DownloadState.Completed);

            {
                var seeds = Clipboard.GetSeeds();

                _downloadListViewPasteMenuItem.IsEnabled = (seeds.Count() > 0) ? true : false;
            }
        }

        private void _downloadListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var downloadItems = _downloadListView.SelectedItems;
            if (downloadItems == null) return;

            foreach (var item in downloadItems.Cast<DownloadListViewItem>())
            {
                try
                {
                    _amoebaManager.DownloadRemove((int)item.Information["Id"]);
                }
                catch (Exception)
                {

                }
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
                _amoebaManager.Download(item, 0);
            }
        }

        private void SetPriority(int i)
        {
            var downloadItems = _downloadListView.SelectedItems;
            if (downloadItems == null) return;

            foreach (var item in downloadItems.Cast<DownloadListViewItem>())
            {
                try
                {
                    _amoebaManager.SetDownloadPriority((int)item.Information["Id"], i);
                }
                catch (Exception)
                {

                }
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

        private void _downloadListViewResetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var downloadItems = _downloadListView.SelectedItems;
            if (downloadItems == null) return;

            foreach (var item in downloadItems.Cast<DownloadListViewItem>())
            {
                try
                {
                    _amoebaManager.DownloadRestart((int)item.Information["Id"]);
                }
                catch (Exception)
                {

                }
            }
        }

        private void _downloadListViewCompleteDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                var downloadingInformation = _amoebaManager.DownloadingInformation.ToArray();

                foreach (var item in downloadingInformation)
                {
                    if (item.Contains("State") && DownloadState.Completed == (DownloadState)item["State"])
                    {
                        try
                        {
                            _amoebaManager.DownloadRemove((int)item["Id"]);
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
                _downloadListView.SelectedIndex = -1;

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
                if (Settings.Instance.DownloadControl_LastHeaderClicked != null)
                {
                    var list = new List<DownloadListViewItem>(_downloadListViewItemCollection);
                    var list2 = Sort(list, Settings.Instance.DownloadControl_LastHeaderClicked, Settings.Instance.DownloadControl_ListSortDirection).ToList();

                    for (int i = 0; i < list2.Count; i++)
                    {
                        var o = _downloadListViewItemCollection.IndexOf(list2[i]);

                        if (i != o) _downloadListViewItemCollection.Move(o, i);
                    }
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _downloadListView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.DownloadControl_Name)
            {
                _downloadListView.Items.SortDescriptions.Add(new SortDescription("Name", direction));
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Length)
            {
                _downloadListView.Items.SortDescriptions.Add(new SortDescription("Length", direction));
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Priority)
            {
                _downloadListView.Items.SortDescriptions.Add(new SortDescription("Priority", direction));
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Rate)
            {
                _downloadListView.Items.SortDescriptions.Add(new SortDescription("State", direction));
                _downloadListView.Items.SortDescriptions.Add(new SortDescription("Rate", direction));
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Path)
            {
                _downloadListView.Items.SortDescriptions.Add(new SortDescription("Path", direction));
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_State)
            {
                _downloadListView.Items.SortDescriptions.Add(new SortDescription("State", direction));
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
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Length)
            {
                list.Sort(delegate(DownloadListViewItem x, DownloadListViewItem y)
                {
                    int c = x.Value.Length.CompareTo(y.Value.Length);
                    if (c != 0) return c;
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
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
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Rate)
            {
                list.Sort(delegate(DownloadListViewItem x, DownloadListViewItem y)
                {
                    int c = ((int)((DownloadState)x.Information["State"])).CompareTo((int)((DownloadState)y.Information["State"]));
                    if (c != 0) return c;
                    c = x.Rate.CompareTo(y.Rate);
                    if (c != 0) return c;
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_State)
            {
                list.Sort(delegate(DownloadListViewItem x, DownloadListViewItem y)
                {
                    int c = ((int)((DownloadState)x.Information["State"])).CompareTo((int)((DownloadState)y.Information["State"]));
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
