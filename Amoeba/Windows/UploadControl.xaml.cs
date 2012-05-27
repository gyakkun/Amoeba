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
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollection<UploadListViewItem> _listViewItemCollection = new ObservableCollection<UploadListViewItem>();

        public UploadControl(MainWindow mainWindow, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _listView.ItemsSource = _listViewItemCollection;
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.UploadItemShow), this);
        }

        private void UploadItemShow(object state)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Thread.CurrentThread.IsBackground = true;

            try
            {
                for (; ; )
                {
                    Thread.Sleep(1000 * 3);
                    if (App.SelectTab != "Upload") continue;

                    var uploadingInformation = _amoebaManager.UploadingInformation.ToArray();
                    Dictionary<int, Information> dic = new Dictionary<int, Information>();

                    foreach (var item in uploadingInformation.ToArray())
                    {
                        dic[(int)item["Id"]] = item;
                    }

                    Dictionary<int, UploadListViewItem> dic2 = new Dictionary<int, UploadListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in _listViewItemCollection.ToArray())
                        {
                            dic2[(int)item.Information["Id"]] = item;
                        }
                    }), null);

                    List<UploadListViewItem> removeList = new List<UploadListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in _listViewItemCollection.ToArray())
                        {
                            if (!dic.ContainsKey((int)item.Information["Id"]))
                            {
                                removeList.Add(item);
                            }
                        }
                    }), null);

                    List<UploadListViewItem> newList = new List<UploadListViewItem>();
                    Dictionary<UploadListViewItem, Information> updateDic = new Dictionary<UploadListViewItem, Information>();
                    bool clearFlag = false;
                    var selectItems = new List<UploadListViewItem>();

                    if (removeList.Count > 100)
                    {
                        clearFlag = true;
                        removeList.Clear();
                        updateDic.Clear();

                        foreach (var information in uploadingInformation)
                        {
                            newList.Add(new UploadListViewItem(information));
                        }

                        HashSet<int> hid = new HashSet<int>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            hid.UnionWith(_listView.SelectedItems.OfType<UploadListViewItem>().Select(n => (int)n.Information["Id"]));
                        }), null);

                        foreach (var item in newList)
                        {
                            if (hid.Contains((int)item.Information["Id"]))
                            {
                                selectItems.Add(item);
                            }
                        }
                    }
                    else
                    {
                        foreach (var information in uploadingInformation)
                        {
                            UploadListViewItem item = null;

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
                                newList.Add(new UploadListViewItem(information));
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
                }
            }
            catch (Exception)
            {

            }
        }

        private void _listView_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void _listView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var filePaths = new List<string>();

            foreach (var item in ((string[])e.Data.GetData(DataFormats.FileDrop)).ToList())
            {
                if (File.Exists(item)) filePaths.Add(item);
                else if (Directory.Exists(item)) filePaths.AddRange(Directory.GetFiles(item));
            }

            if (filePaths.Count == 1)
            {
                UploadWindow window = new UploadWindow(filePaths[0], false, _amoebaManager);
                window.Owner = _mainWindow;
                window.ShowDialog();
            }
            else if (filePaths.Count > 1)
            {
                UploadListWindow window = new UploadListWindow(filePaths, false, _amoebaManager);
                window.Owner = _mainWindow;
                window.ShowDialog();
            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _listView.SelectedItems;

            _listViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewCopyInfoMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewResetMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewPriorityMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
         
            if (!_listViewCompleteDeleteMenuItem_IsEnabled) _listViewCompleteDeleteMenuItem.IsEnabled = false;
            else _listViewCompleteDeleteMenuItem.IsEnabled = _listViewItemCollection.Any(n => (UploadState)n.Information["State"] == UploadState.Completed);
        }

        private void _listViewAddMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var uploadFilePaths = dialog.FileNames.ToList();

                if (uploadFilePaths.Count == 1)
                {
                    UploadWindow window = new UploadWindow(uploadFilePaths[0], false, _amoebaManager);
                    window.Owner = _mainWindow;
                    window.ShowDialog();
                }
                else if (uploadFilePaths.Count > 1)
                {
                    UploadListWindow window = new UploadListWindow(uploadFilePaths, false, _amoebaManager);
                    window.Owner = _mainWindow;
                    window.ShowDialog();
                }
            }
        }

        private void _listViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var uploadItems = _listView.SelectedItems;
            if (uploadItems == null) return;

            foreach (var item in uploadItems.Cast<UploadListViewItem>())
            {
                try
                {
                    _amoebaManager.RemoveUpload((int)item.Information["Id"]);
                }
                catch (Exception)
                {

                }
            }
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _listView.SelectedItems;
            if (selectItems == null) return;

            var sb = new StringBuilder();

            foreach (var seed in selectItems.Cast<UploadListViewItem>().Select(n => n.Value))
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

            foreach (var seed in selectItems.Cast<UploadListViewItem>().Select(n => n.Value))
            {
                if (seed == null) continue;

                sb.AppendLine(MessageConverter.ToInfoMessage(seed));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        private void SetPriority(int i)
        {
            var uploadItems = _listView.SelectedItems;
            if (uploadItems == null) return;

            foreach (var item in uploadItems.Cast<UploadListViewItem>())
            {
                try
                {
                    _amoebaManager.SetUploadPriority((int)item.Information["Id"], i);
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
            var uploadItems = _listView.SelectedItems;
            if (uploadItems == null) return;

            foreach (var item in uploadItems.Cast<UploadListViewItem>())
            {
                try
                {
                    _amoebaManager.ResetUpload((int)item.Information["Id"]);
                }
                catch (Exception)
                {

                }
            }
        }

        volatile bool _listViewCompleteDeleteMenuItem_IsEnabled = true;

        private void _listViewCompleteDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _listViewCompleteDeleteMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem(new WaitCallback((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                var uploadingInformation = _amoebaManager.UploadingInformation.ToArray();

                foreach (var item in uploadingInformation)
                {
                    if (item.Contains("State") && UploadState.Completed == (UploadState)item["State"])
                    {
                        try
                        {
                            _amoebaManager.RemoveUpload((int)item["Id"]);
                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                _listViewCompleteDeleteMenuItem_IsEnabled = true;
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

                if (headerClicked != Settings.Instance.UploadControl_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.UploadControl_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                Settings.Instance.UploadControl_LastHeaderClicked = headerClicked;
                Settings.Instance.UploadControl_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.UploadControl_LastHeaderClicked != null)
                {
                    var list = Sort(_listViewItemCollection, Settings.Instance.UploadControl_LastHeaderClicked, Settings.Instance.UploadControl_ListSortDirection).ToList();

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

            if (sortBy == LanguagesManager.Instance.UploadControl_Name)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Name", direction));
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_Length)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Length", direction));
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_Priority)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Priority", direction));
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_Rate)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("State", direction));
                _listView.Items.SortDescriptions.Add(new SortDescription("Rate", direction));
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_State)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("State", direction));
            }
        }

        private IEnumerable<UploadListViewItem> Sort(IEnumerable<UploadListViewItem> collection, string sortBy, ListSortDirection direction)
        {
            List<UploadListViewItem> list = new List<UploadListViewItem>(collection);

            if (sortBy == LanguagesManager.Instance.UploadControl_Name)
            {
                list.Sort(delegate(UploadListViewItem x, UploadListViewItem y)
                {
                    int c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_Length)
            {
                list.Sort(delegate(UploadListViewItem x, UploadListViewItem y)
                {
                    int c = x.Length.CompareTo(y.Length);
                    if (c != 0) return c;
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_Priority)
            {
                list.Sort(delegate(UploadListViewItem x, UploadListViewItem y)
                {
                    int c = x.Priority.CompareTo(y.Priority);
                    if (c != 0) return c;
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_Rate)
            {
                list.Sort(delegate(UploadListViewItem x, UploadListViewItem y)
                {
                    int c = ((int)((UploadState)x.Information["State"])).CompareTo((int)((UploadState)y.Information["State"]));
                    if (c != 0) return c;
                    c = x.Rate.CompareTo(y.Rate);
                    if (c != 0) return c;
                    c = ((int)x.Information["Id"]).CompareTo((int)y.Information["Id"]);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_State)
            {
                list.Sort(delegate(UploadListViewItem x, UploadListViewItem y)
                {
                    int c = ((int)((UploadState)x.Information["State"])).CompareTo((int)((UploadState)y.Information["State"]));
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
