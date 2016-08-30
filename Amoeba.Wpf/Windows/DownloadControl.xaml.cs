using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;

        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollectionEx<DownloadListViewModel> _listViewModelCollection = new ObservableCollectionEx<DownloadListViewModel>();
        private object _listLock = new object();

        private Thread _showDownloadItemThread;
        private Thread _watchThread;

        public DownloadControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            InitializeComponent();

            _listView.ItemsSource = _listViewModelCollection;

            _showDownloadItemThread = new Thread(this.ShowDownloadItem);
            _showDownloadItemThread.Priority = ThreadPriority.Highest;
            _showDownloadItemThread.IsBackground = true;
            _showDownloadItemThread.Name = "DownloadControl_ShowDownloadItemThread";
            _showDownloadItemThread.Start();

            _watchThread = new Thread(this.Watch);
            _watchThread.Priority = ThreadPriority.Highest;
            _watchThread.IsBackground = true;
            _watchThread.Name = "DownloadControl_WatchThread";
            _watchThread.Start();

            _searchRowDefinition.Height = new GridLength(0);

            LanguagesManager.UsingLanguageChangedEvent += this.LanguagesManager_UsingLanguageChangedEvent;
        }

        private void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {
            _listView.Items.Refresh();
        }

        private void ShowDownloadItem()
        {
            try
            {
                for (;;)
                {
                    var informaitonDic = new Dictionary<int, Information>();

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

                        foreach (var item in _amoebaManager.DownloadingInformation.ToArray())
                        {
                            if (words != null)
                            {
                                var text = ((string)item["Path"] ?? "").ToLower();
                                if (!words.All(n => text.Contains(n))) continue;
                            }

                            informaitonDic[(int)item["Id"]] = item;
                        }
                    }

                    var listViewModelDic = new Dictionary<int, DownloadListViewModel>();
                    var removeList = new List<DownloadListViewModel>();

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        foreach (var item in _listViewModelCollection.ToArray())
                        {
                            listViewModelDic[item.Id] = item;

                            if (!informaitonDic.ContainsKey(item.Id))
                            {
                                removeList.Add(item);
                            }
                        }
                    }));

                    var resultList = new List<DownloadListViewModel>();
                    var updateDic = new Dictionary<DownloadListViewModel, Information>();

                    bool clearFlag = false;
                    var selectItems = new List<DownloadListViewModel>();

                    if (removeList.Count > 100)
                    {
                        clearFlag = true;

                        removeList.Clear();
                        updateDic.Clear();

                        foreach (var information in informaitonDic.Values)
                        {
                            resultList.Add(new DownloadListViewModel(information));
                        }

                        var hid = new HashSet<int>();

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            hid.UnionWith(_listView.SelectedItems.OfType<DownloadListViewModel>().Select(n => n.Id));
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
                            DownloadListViewModel item;

                            if (listViewModelDic.TryGetValue((int)information["Id"], out item))
                            {
                                if (!CollectionUtils.Equals(item.Information, information))
                                {
                                    updateDic[item] = information;
                                }
                            }
                            else
                            {
                                resultList.Add(new DownloadListViewModel(information));
                            }
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        bool sortFlag = false;

                        if (resultList.Count != 0) sortFlag = true;
                        if (removeList.Count != 0) sortFlag = true;
                        if (updateDic.Count != 0) sortFlag = true;

                        if (clearFlag) _listViewModelCollection.Clear();

                        foreach (var item in resultList)
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

        private void Watch()
        {
            try
            {
                for (;;)
                {
                    Thread.Sleep(1000 * 3);

                    if (!Directory.Exists(_serviceManager.Paths["Input"])) continue;

                    foreach (var filePath in Directory.GetFiles(_serviceManager.Paths["Input"]))
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
            catch (Exception e)
            {
                Log.Error(e);
            }
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
            else _listViewDeleteCompleteMenuItem.IsEnabled = _listViewModelCollection.Any(n => n.State == DownloadState.Completed);

            _listViewPasteMenuItem.IsEnabled = Clipboard.ContainsSeeds();
        }

        volatile bool _listViewDeleteMenuItem_IsEnabled = true;

        private void _listViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _listView.SelectedItems;
            if (selectItems == null || selectItems.Count == 0) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Download", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var ids = new List<int>();

            foreach (var item in selectItems.Cast<DownloadListViewModel>())
            {
                ids.Add(item.Id);
            }

            _listViewDeleteMenuItem_IsEnabled = false;

            Task.Run(() =>
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
            });
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _listView.SelectedItems;
            if (selectItems == null) return;

            var sb = new StringBuilder();

            foreach (var seed in selectItems.Cast<DownloadListViewModel>().Select(n => n.Value))
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

            foreach (var seed in selectItems.Cast<DownloadListViewModel>().Select(n => n.Value))
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
            var list = new HashSet<Seed>(Clipboard.GetSeeds());

            Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    foreach (var item in list)
                    {
                        _amoebaManager.Download(item, 3);
                    }
                }
                catch (Exception)
                {

                }
            });
        }

        #region Priority

        private void SetPriority(int i)
        {
            var downloadItems = _listView.SelectedItems;
            if (downloadItems == null) return;

            foreach (var item in downloadItems.Cast<DownloadListViewModel>())
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

            foreach (var item in downloadItems.Cast<DownloadListViewModel>())
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

            Task.Run(() =>
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
            });
        }

        #region Sort

        private void Sort()
        {
            _listView.Items.SortDescriptions.Clear();

            if (Settings.Instance.DownloadControl_LastHeaderClicked != null)
            {
                var list = Sort(_listViewModelCollection, Settings.Instance.DownloadControl_LastHeaderClicked, Settings.Instance.DownloadControl_ListSortDirection).ToList();

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

            this.Sort(headerClicked, direction);

            Settings.Instance.DownloadControl_LastHeaderClicked = headerClicked;
            Settings.Instance.DownloadControl_ListSortDirection = direction;
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listView.Items.SortDescriptions.Clear();

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
                _listView.Items.SortDescriptions.Add(new SortDescription("Rate", direction));
                _listView.Items.SortDescriptions.Add(new SortDescription("Rank", direction));
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Path)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Path", direction));
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_CreationTime)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("CreationTime", direction));
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_State)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("State", direction));
            }

            _listView.Items.SortDescriptions.Add(new SortDescription("Id", direction));
        }

        private IEnumerable<DownloadListViewModel> Sort(IEnumerable<DownloadListViewModel> collection, string sortBy, ListSortDirection direction)
        {
            var list = new List<DownloadListViewModel>(collection);

            if (sortBy == LanguagesManager.Instance.DownloadControl_Name)
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
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Length)
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
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Priority)
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
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Rate)
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
            else if (sortBy == LanguagesManager.Instance.DownloadControl_Path)
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
            else if (sortBy == LanguagesManager.Instance.DownloadControl_CreationTime)
            {
                list.Sort((x, y) =>
                {
                    int c = x.CreationTime.CompareTo(y.CreationTime);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.DownloadControl_State)
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

        private class DownloadListViewModel : INotifyPropertyChanged
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
            private DateTime _creationTime;
            private DownloadState _state = 0;
            private long _length;
            private int _priority;
            private double _rate;
            private string _rateText;
            private Seed _value;

            public DownloadListViewModel(Information information)
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

                    if (_information.Contains("CreationTime")) this.CreationTime = (DateTime)_information["CreationTime"];
                    else this.CreationTime = DateTime.MinValue;

                    if (_information.Contains("State")) this.State = (DownloadState)_information["State"];
                    else this.State = 0;

                    if (_information.Contains("Priority")) this.Priority = (int)_information["Priority"];
                    else this.Priority = 0;

                    if (_information.Contains("State"))
                    {
                        if (_information.Contains("DownloadBlockCount") && _information.Contains("BlockCount") && _information.Contains("ParityBlockCount")
                            && ((DownloadState)_information["State"] == DownloadState.Downloading || (DownloadState)_information["State"] == DownloadState.Completed || (DownloadState)_information["State"] == DownloadState.Error))
                        {
                            this.Rate = Math.Round(((double)(int)_information["DownloadBlockCount"] / (double)((int)_information["BlockCount"] - (int)_information["ParityBlockCount"])) * 100, 2);
                        }
                        else if (_information.Contains("DecodeLength") && _information.Contains("DecodeOffset")
                            && ((DownloadState)_information["State"] == DownloadState.ParityDecoding || (DownloadState)_information["State"] == DownloadState.Decoding)
                            && (long)_information["DecodeOffset"] != 0)
                        {
                            this.Rate = Math.Round(((double)(long)_information["DecodeOffset"] / (double)(long)_information["DecodeLength"]) * 100, 2);
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
                        if (_information.Contains("DownloadBlockCount") && _information.Contains("BlockCount") && _information.Contains("ParityBlockCount") && _information.Contains("Rank") && _information.Contains("Seed")
                            && ((DownloadState)_information["State"] == DownloadState.Downloading || (DownloadState)_information["State"] == DownloadState.Completed || (DownloadState)_information["State"] == DownloadState.Error))
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
                        else if (_information.Contains("DecodeLength") && _information.Contains("DecodeOffset") && _information.Contains("Rank") && _information.Contains("Seed")
                            && ((DownloadState)_information["State"] == DownloadState.ParityDecoding || (DownloadState)_information["State"] == DownloadState.Decoding))
                        {
                            this.RateText = string.Format("{0}% {1}/{2} [{3}/{4}]",
                                this.Rate,
                                NetworkConverter.ToSizeString((long)_information["DecodeOffset"]),
                                NetworkConverter.ToSizeString((long)_information["DecodeLength"]),
                                (int)_information["Rank"],
                                ((Seed)_information["Seed"]).Rank);
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

                        this.NotifyPropertyChanged(nameof(this.Rank));
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

                        this.NotifyPropertyChanged(nameof(this.Name));
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

                        this.NotifyPropertyChanged(nameof(this.Path));
                    }
                }
            }

            public DateTime CreationTime
            {
                get
                {
                    return _creationTime;
                }
                set
                {
                    if (value != _creationTime)
                    {
                        _creationTime = value;

                        this.NotifyPropertyChanged(nameof(this.CreationTime));
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

                        this.NotifyPropertyChanged(nameof(this.State));
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

                        this.NotifyPropertyChanged(nameof(this.Length));
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

                        this.NotifyPropertyChanged(nameof(this.Priority));
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

                        this.NotifyPropertyChanged(nameof(this.Rate));
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

                        this.NotifyPropertyChanged(nameof(this.RateText));
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

                        this.NotifyPropertyChanged(nameof(this.Value));
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
