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
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollectionEx<UploadListViewItem> _listViewItemCollection = new ObservableCollectionEx<UploadListViewItem>();

        private Thread _showUploadItemThread;

        private volatile bool _shareAddIsRunning = false;

        public UploadControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _listView.ItemsSource = _listViewItemCollection;

            _showUploadItemThread = new Thread(this.ShowUploadItem);
            _showUploadItemThread.Priority = ThreadPriority.Highest;
            _showUploadItemThread.IsBackground = true;
            _showUploadItemThread.Name = "UploadControl_ShowUploadItemThread";
            _showUploadItemThread.Start();

            _searchRowDefinition.Height = new GridLength(0);

            LanguagesManager.UsingLanguageChangedEvent += this.LanguagesManager_UsingLanguageChangedEvent;
        }

        private void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {
            _listView.Items.Refresh();
        }

        private void ShowUploadItem()
        {
            try
            {
                for (;;)
                {
                    Thread.Sleep(100);
                    if (_mainWindow.SelectedTab != MainWindowTabType.Upload) continue;

                    Dictionary<int, Information> informaitonDic = new Dictionary<int, Information>();

                    {
                        string[] words = null;

                        {
                            string searchText = null;

                            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                searchText = _searchTextBox.Text;
                            }));

                            if (!string.IsNullOrWhiteSpace(searchText))
                            {
                                words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                            }
                        }

                        foreach (var item in _amoebaManager.UploadingInformation.ToArray())
                        {
                            if (words != null)
                            {
                                var text = ((string)item["Path"] ?? "").ToLower();
                                if (!words.All(n => text.Contains(n))) continue;
                            }

                            informaitonDic[(int)item["Id"]] = item;
                        }
                    }

                    Dictionary<int, UploadListViewItem> listViewItemDic = new Dictionary<int, UploadListViewItem>();
                    List<UploadListViewItem> removeList = new List<UploadListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        foreach (var item in _listViewItemCollection.ToArray())
                        {
                            listViewItemDic[item.Id] = item;

                            if (!informaitonDic.ContainsKey(item.Id))
                            {
                                removeList.Add(item);
                            }
                        }
                    }));

                    List<UploadListViewItem> resultList = new List<UploadListViewItem>();
                    Dictionary<UploadListViewItem, Information> updateDic = new Dictionary<UploadListViewItem, Information>();

                    bool clearFlag = false;
                    var selectItems = new List<UploadListViewItem>();

                    if (removeList.Count > 100)
                    {
                        clearFlag = true;

                        removeList.Clear();
                        updateDic.Clear();

                        foreach (var information in informaitonDic.Values)
                        {
                            resultList.Add(new UploadListViewItem(information));
                        }

                        HashSet<int> hid = new HashSet<int>();

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            hid.UnionWith(_listView.SelectedItems.OfType<UploadListViewItem>().Select(n => n.Id));
                        }));

                        foreach (var item in resultList)
                        {
                            if (hid.Contains(item.Id))
                            {
                                selectItems.Add(item);
                            }
                        }
                    }
                    else
                    {
                        foreach (var information in informaitonDic.Values)
                        {
                            UploadListViewItem item;

                            if (listViewItemDic.TryGetValue((int)information["Id"], out item))
                            {
                                if (!CollectionUtilities.Equals(item.Information, information))
                                {
                                    updateDic[item] = information;
                                }
                            }
                            else
                            {
                                resultList.Add(new UploadListViewItem(information));
                            }
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        bool sortFlag = false;

                        if (resultList.Count != 0) sortFlag = true;
                        if (removeList.Count != 0) sortFlag = true;
                        if (updateDic.Count != 0) sortFlag = true;

                        if (clearFlag) _listViewItemCollection.Clear();

                        foreach (var item in resultList)
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

        private void _listView_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void _listView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var result = ((string[])e.Data.GetData(DataFormats.FileDrop)).ToList();

            ThreadPool.QueueUserWorkItem((object wstate) =>
            {
                if (_shareAddIsRunning) return;
                _shareAddIsRunning = true;

                Thread.CurrentThread.IsBackground = true;

                try
                {
                    var filePaths = new HashSet<string>();

                    foreach (var item in result)
                    {
                        if (File.Exists(item)) filePaths.Add(item);
                        else if (Directory.Exists(item)) filePaths.UnionWith(Directory.GetFiles(item, "*", SearchOption.AllDirectories));
                    }

                    foreach (var informaiton in _amoebaManager.ShareInformation)
                    {
                        filePaths.Remove((string)informaiton["Path"]);
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        if (filePaths.Count == 1)
                        {
                            UploadWindow window = new UploadWindow(filePaths.First(), false, _amoebaManager);
                            window.Owner = _mainWindow;
                            window.ShowDialog();
                        }
                        else if (filePaths.Count > 1)
                        {
                            UploadListWindow window = new UploadListWindow(filePaths, false, _amoebaManager);
                            window.Owner = _mainWindow;
                            window.ShowDialog();
                        }
                    }));
                }
                finally
                {
                    _shareAddIsRunning = false;
                }
            });
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _listView.SelectedItems;

            if (!_listViewDeleteMenuItem_IsEnabled) _listViewDeleteMenuItem.IsEnabled = false;
            else _listViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

            _listViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _listViewCopyInfoMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _listViewResetMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _listViewPriorityMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

            if (!_listViewDeleteCompleteMenuItem_IsEnabled) _listViewDeleteCompleteMenuItem.IsEnabled = false;
            else _listViewDeleteCompleteMenuItem.IsEnabled = _listViewItemCollection.Any(n => n.State == UploadState.Completed);
        }

        private void _listViewAddMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Multiselect = true;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var filePaths = new HashSet<string>(dialog.FileNames);

                ThreadPool.QueueUserWorkItem((object wstate) =>
                {
                    if (_shareAddIsRunning) return;
                    _shareAddIsRunning = true;

                    Thread.CurrentThread.IsBackground = true;

                    try
                    {
                        foreach (var informaiton in _amoebaManager.ShareInformation)
                        {
                            filePaths.Remove((string)informaiton["Path"]);
                        }

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            if (filePaths.Count == 1)
                            {
                                UploadWindow window = new UploadWindow(filePaths.First(), false, _amoebaManager);
                                window.Owner = _mainWindow;
                                window.ShowDialog();
                            }
                            else if (filePaths.Count > 1)
                            {
                                UploadListWindow window = new UploadListWindow(filePaths, false, _amoebaManager);
                                window.Owner = _mainWindow;
                                window.ShowDialog();
                            }
                        }));
                    }
                    finally
                    {
                        _shareAddIsRunning = false;
                    }
                });
            }
        }

        volatile bool _listViewDeleteMenuItem_IsEnabled = true;

        private void _listViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _listView.SelectedItems;
            if (selectItems == null || selectItems.Count == 0) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Upload", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            List<int> ids = new List<int>();

            foreach (var item in selectItems.Cast<UploadListViewItem>())
            {
                ids.Add(item.Id);
            }

            _listViewDeleteMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem((object wstate) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in ids)
                    {
                        _amoebaManager.RemoveUpload(item);
                    }
                }
                catch (Exception)
                {

                }

                _listViewDeleteMenuItem_IsEnabled = true;
            });
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

                sb.AppendLine(AmoebaConverter.ToSeedString(seed));
                sb.AppendLine(MessageConverter.ToInfoMessage(seed));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        #region Priority

        private void SetPriority(int i)
        {
            var uploadItems = _listView.SelectedItems;
            if (uploadItems == null) return;

            foreach (var item in uploadItems.Cast<UploadListViewItem>())
            {
                try
                {
                    _amoebaManager.SetUploadPriority(item.Id, i);
                }
                catch (Exception)
                {

                }
            }
        }

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
                    _amoebaManager.ResetUpload(item.Id);
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

            ThreadPool.QueueUserWorkItem((object wstate) =>
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

                _listViewDeleteCompleteMenuItem_IsEnabled = true;
            });
        }

        #region Sort

        private void Sort()
        {
            _listView.Items.SortDescriptions.Clear();

            if (Settings.Instance.UploadControl_LastHeaderClicked != null)
            {
                var list = this.Sort(_listViewItemCollection, Settings.Instance.UploadControl_LastHeaderClicked, Settings.Instance.UploadControl_ListSortDirection).ToList();

                for (int i = 0; i < list.Count; i++)
                {
                    var o = _listViewItemCollection.IndexOf(list[i]);

                    if (i != o) _listViewItemCollection.Move(o, i);
                }
            }
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
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

            this.Sort(headerClicked, direction);

            Settings.Instance.UploadControl_LastHeaderClicked = headerClicked;
            Settings.Instance.UploadControl_ListSortDirection = direction;
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
                _listView.Items.SortDescriptions.Add(new SortDescription("Rank", direction));
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_Path)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Path", direction));
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_State)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("State", direction));
            }

            _listView.Items.SortDescriptions.Add(new SortDescription("Id", direction));
        }

        private IEnumerable<UploadListViewItem> Sort(IEnumerable<UploadListViewItem> collection, string sortBy, ListSortDirection direction)
        {
            List<UploadListViewItem> list = new List<UploadListViewItem>(collection);

            if (sortBy == LanguagesManager.Instance.UploadControl_Name)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Name.CompareTo(y.Name);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_Length)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Length.CompareTo(y.Length);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_Priority)
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
            else if (sortBy == LanguagesManager.Instance.UploadControl_Rate)
            {
                list.Sort((x, y) =>
                {
                    int c = x.State.CompareTo(y.State);
                    if (c != 0) return c;
                    c = x.Rate.CompareTo(y.Rate);
                    if (c != 0) return c;
                    c = x.Rank.CompareTo(y.Rank);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_Path)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Path.CompareTo(y.Path);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.UploadControl_State)
            {
                list.Sort((x, y) =>
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

            private int _id;
            private Information _information;
            private int _rank;
            private string _name;
            private string _path;
            private UploadState _state = 0;
            private long _length;
            private int _priority;
            private double _rate;
            private string _rateText;
            private Seed _value;

            public UploadListViewItem(Information information)
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
                            && ((UploadState)_information["State"] == UploadState.ComputeHash || (UploadState)_information["State"] == UploadState.Encoding || (UploadState)_information["State"] == UploadState.ParityEncoding)
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
                            && ((UploadState)_information["State"] == UploadState.ComputeHash || (UploadState)_information["State"] == UploadState.Encoding || (UploadState)_information["State"] == UploadState.ParityEncoding))
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
                        _path = value; this.NotifyPropertyChanged("Path");
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

        private void Execute_New(object sender, ExecutedRoutedEventArgs e)
        {
            _listViewAddMenuItem_Click(null, null);
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            _listViewDeleteMenuItem_Click(null, null);
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            _listViewCopyMenuItem_Click(null, null);
        }

        private void Execute_Search(object sender, ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(24);
            _searchTextBox.Focus();
        }

        private void Execute_Close(object sender, ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(0);
            _searchTextBox.Text = "";
        }
    }
}
