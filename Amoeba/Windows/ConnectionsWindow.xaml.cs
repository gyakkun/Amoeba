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
using System.Windows.Shapes;
using Library.Net.Amoeba;
using Amoeba.Properties;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.IO;
using Library;

namespace Amoeba.Windows
{
    /// <summary>
    /// ConnectionsWindow.xaml の相互作用ロジック
    /// </summary>
    partial class ConnectionsWindow : Window
    {
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;
        private UpnpManager _upnpManager;
        private Ipv4Manager _ipv4Manager;
        private Ipv6Manager _ipv6Manager;

        private Node _myNode;
        private NodeCollection _otherNodes = new NodeCollection();
        private KeywordCollection _keywords = new KeywordCollection();
        private ConnectionFilterCollection _clientFilters = new ConnectionFilterCollection();
        private UriCollection _listenUris = new UriCollection();

        public ConnectionsWindow(AmoebaManager amoebaManager, UpnpManager upnpManager, Ipv4Manager ipv4Manager, Ipv6Manager ipv6Manager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _upnpManager = upnpManager;
            _ipv4Manager = ipv4Manager;
            _ipv6Manager = ipv6Manager;
            _bufferManager = bufferManager;

            using (DeadlockMonitor.Lock(_amoebaManager.ThisLock))
            {
                _myNode = _amoebaManager.BaseNode.DeepClone();
                _otherNodes.AddRange(_amoebaManager.OtherNodes.Select(n => n.DeepClone()));
                _keywords.AddRange(_amoebaManager.SearchKeywords.Select(n => n.DeepClone()));
                _clientFilters.AddRange(_amoebaManager.Filters.Select(n => n.DeepClone()));
                _listenUris.AddRange(_amoebaManager.ListenUris);
            }

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _myNodeTextBoxUpdate();

            _myNodeUrisListView.ItemsSource = _myNode.Uris;
            _otherNodesListView.ItemsSource = _otherNodes;
            _clientFiltersListView.ItemsSource = _clientFilters;
            _serverListenUrisListView.ItemsSource = _listenUris;
            _keywordsListView.ItemsSource = _keywords;
            _miscellaneousDownloadDirectoryTextBox.Text = _amoebaManager.DownloadDirectory;
            _miscellaneousConnectionCountTextBox.Text = _amoebaManager.ConnectionCountLimit.ToString();
            _miscellaneousDownloadingConnectionCountTextBox.Text = _amoebaManager.DownloadingConnectionCountLowerLimit.ToString();
            _miscellaneousUploadingConnectionCountTextBox.Text = _amoebaManager.UploadingConnectionCountLowerLimit.ToString();
            _miscellaneousCacheSizeTextBox.Text = NetworkConverter.ToSizeString(_amoebaManager.Size);
            _miscellaneousAutoUpdateCheckBox.IsChecked = Settings.Instance.Global_AutoUpdate_IsEnabled;
            _extendsUpnpCheckBox.IsChecked = Settings.Instance.Global_Upnp_IsEnabled;
            _extendsIpv4CheckBox.IsChecked = Settings.Instance.Global_Ipv4_IsEnabled;
            _extendsIpv6CheckBox.IsChecked = Settings.Instance.Global_Ipv6_IsEnabled;

            foreach (var item in Enum.GetValues(typeof(ConnectionType)))
            {
                _clientFiltersConnectionTypeComboBox.Items.Add(new ComboBoxItem() { Content = new ConnectionTypeToStringConverter().Convert(item, typeof(ConnectionType), null, null) });
            }

            _clientFiltersConnectionTypeComboBox.SelectedItem = _clientFiltersConnectionTypeComboBox.Items.GetItemAt(0);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_amoebaManager.State == ManagerState.Start)
            {
                if (Settings.Instance.Global_Upnp_IsEnabled)
                {
                    _upnpManager.Start();
                }
                else
                {
                    _upnpManager.Stop();
                }

                if (Settings.Instance.Global_Ipv4_IsEnabled)
                {
                    _ipv4Manager.Start();
                }
                else
                {
                    _ipv4Manager.Stop();
                }

                if (Settings.Instance.Global_Ipv6_IsEnabled)
                {
                    _ipv6Manager.Start();
                }
                else
                {
                    _ipv6Manager.Stop();
                }
            }
        }

        #region MyNode

        private void _myNodeTextBoxUpdate()
        {
            if (_myNode.Uris.Count > 0)
            {
                _myNodeTextBox.Text = AmoebaConverter.ToNodeString(_myNode);
            }
            else
            {
                _myNodeTextBox.Text = "";
            }
        }

        private void _myNodeUrisListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _myNodeUrisListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {
            }
        }

        private void _myNodeUrisListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _myNodeUrisListView.SelectedItem as string;
            if (item == null)
                return;
            try
            {
                _myNodeUriTextBox.Text = item;

                Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                Match match = regex.Match(item);

                if (match.Success)
                {
                    var conboboxItem = _myNodeUriSchemeComboBox.Items.Cast<ComboBoxItem>()
                        .FirstOrDefault(n => (string)n.Content == match.Groups[1].Value);

                    if (conboboxItem != null)
                    {
                        conboboxItem.IsSelected = true;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void _myNodeUriSchemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _myNodeUriSchemeComboBox_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {
            }
        }

        private void _myNodeUriSchemeComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _myNodeUriSchemeComboBox.SelectedItem as ComboBoxItem;
            if (item == null)
                return;
            try
            {
                string scheme = (string)((ComboBoxItem)_myNodeUriSchemeComboBox.SelectedItem).Content;
                Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                Match match = regex.Match(_myNodeUriTextBox.Text);

                if (!match.Success)
                {
                    _myNodeUriTextBox.Text = string.Format("{0}:", scheme);
                }
                else
                {
                    _myNodeUriTextBox.Text = string.Format("{0}:{1}:{2}", scheme, match.Groups[2].Value, match.Groups[3].Value);
                }
            }
            catch (Exception)
            {
            }
        }

        private void _myNodeUriAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_myNodeUriTextBox.Text == "") return;

            try
            {
                var uri = _myNodeUriTextBox.Text;
                if (_myNode.Uris.Any(n => n == uri)) return;

                _myNode.Uris.Add(uri);
                _myNodeUrisListView.Items.Refresh();
            }
            catch (UriFormatException)
            {

            }
            finally
            {
                _myNodeUriTextBox.Text = "";
                _myNodeUriSchemeComboBox_SelectionChanged(this, null);
            }

            var random = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[64];
            random.GetBytes(buffer);
            _myNode.Id = buffer;

            _myNodeTextBoxUpdate();
        }

        private void _myNodeUriEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_myNodeUriTextBox.Text == "") return;

            var item = _myNodeUrisListView.SelectedItem as string;
            if (item == null) return;

            try
            {
                _myNode.Uris[_myNode.Uris.IndexOf(item)] = _myNodeUriTextBox.Text;
                _myNodeUrisListView.Items.Refresh();
            }
            catch (UriFormatException)
            {

            }

            var random = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[64];
            random.GetBytes(buffer);
            _myNode.Id = buffer;

            _myNodeTextBoxUpdate();
        }

        private void _myNodeUriDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _myNodeUrisListView.SelectedIndex;
            foreach (var item in _myNodeUrisListView.SelectedItems.OfType<string>().ToArray())
            {
                _myNode.Uris.Remove(item);
            }
            _myNodeUrisListView.Items.Refresh();
            _myNodeUrisListView.SelectedIndex = selectIndex;

            _myNodeUriTextBox.Text = "";
            _myNodeUriSchemeComboBox_SelectionChanged(this, null);

            var random = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[64];
            random.GetBytes(buffer);
            _myNode.Id = buffer;

            _myNodeTextBoxUpdate();
        }

        #endregion

        #region OtherNodes

        private void _otherNodesUrisTextBoxUpdate()
        {
            var node = _otherNodesListView.SelectedItem as Node;
            if (node == null) return;

            _otherNodesListView_SelectionChanged(this, null);
        }

        private void _otherNodesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _otherNodesListView_PreviewMouseLeftButtonDown(this, null);
        }

        private void _otherNodesListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var node = _otherNodesListView.SelectedItem as Node;
            if (node == null)
                return;

            StringBuilder builder = new StringBuilder();

            foreach (var item in node.Uris)
            {
                builder.Append(item + ", ");
            }

            if (builder.Length <= 2)
                _otherNodesUrisTextBox.Text = "";
            else
                _otherNodesUrisTextBox.Text = builder.ToString().Remove(builder.Length - 2);
        }

        private void _otherNodesAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_otherNodesNodeTextBox.Text == "") return;

            try
            {
                foreach (var item in _otherNodesNodeTextBox.Text.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var otherNode = AmoebaConverter.FromNodeString(item);

                    if (otherNode.Id != null && otherNode.Uris.Count != 0)
                    {
                        _otherNodes.Add(otherNode);
                    }
                }
            }
            catch (Exception)
            {

            }

            _otherNodesNodeTextBox.Text = "";

            _otherNodesListView.Items.Refresh();
        }

        private void _otherNodesListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_otherNodesCopyContextMenuItem != null) _otherNodesCopyContextMenuItem.IsEnabled = (_otherNodesListView.SelectedItems.Count > 0);
        }

        private void _otherNodesCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _otherNodesListView.SelectedItems.OfType<Node>())
            {
                sb.AppendLine(AmoebaConverter.ToNodeString(item));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _otherNodesPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Clipboard.GetNodes())
            {
                _otherNodes.Add(item);
            }

            _otherNodesListView.Items.Refresh();
        }

        #endregion

        #region Client

        private void _clientFiltersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _clientFiltersListView_PreviewMouseLeftButtonDown(this, null);
        }

        private void _clientFiltersListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _clientFiltersListView.SelectedItem as ConnectionFilter;
            if (item == null)
                return;

            if (item.ProxyUri != null)
            {
                _clientFiltersProxyUriTextBox.Text = item.ProxyUri;
            }
            else
            {
                _clientFiltersProxyUriTextBox.Text = "";
            }

            if (item.UriCondition != null)
            {
                _clientFiltersConditionTextBox.Text = item.UriCondition.Value;

                Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                Match match = regex.Match(item.UriCondition.Value);

                if (match.Success)
                {
                    var conboboxItem = _clientFiltersConditionSchemeComboBox.Items.Cast<ComboBoxItem>()
                        .FirstOrDefault(n => (string)n.Content == match.Groups[1].Value);

                    if (conboboxItem != null)
                    {
                        conboboxItem.IsSelected = true;
                    }
                }
            }
            else
            {
                _clientFiltersConditionTextBox.Text = "";
            }

            var connectionTypeConboboxItem = _clientFiltersConnectionTypeComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(n => (string)n.Content == item.ConnectionType.ToString());

            if (connectionTypeConboboxItem != null)
            {
                connectionTypeConboboxItem.IsSelected = true;
            }
        }

        private void _clientFiltersConnectionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _clientFiltersConnectionTypeComboBox_PreviewMouseLeftButtonDown(this, null);
        }

        private void _clientFiltersConnectionTypeComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var connectionTypeText = (string)((ComboBoxItem)_clientFiltersConnectionTypeComboBox.SelectedItem).Content;
            var connectionType = (ConnectionType)new ConnectionTypeToStringConverter().ConvertBack(connectionTypeText, typeof(string), null, null);

            if (connectionType == ConnectionType.None || connectionType == ConnectionType.Tcp)
            {
                _clientFiltersProxyUriTextBox.IsEnabled = false;
            }
            else
            {
                _clientFiltersProxyUriTextBox.IsEnabled = true;

                string scheme = null;
                int port = 0;
                Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                Match match = regex.Match(_clientFiltersProxyUriTextBox.Text);

                if (connectionType == ConnectionType.Socks4Proxy
                    || connectionType == ConnectionType.Socks4aProxy
                    || connectionType == ConnectionType.Socks5Proxy)
                {
                    scheme = "tcp";
                    port = 1080;
                }
                else if (connectionType == ConnectionType.HttpProxy)
                {
                    scheme = "tcp";
                    port = 80;
                }

                if (!match.Success)
                {
                    _clientFiltersProxyUriTextBox.Text = string.Format("{0}:127.0.0.1:{1}", scheme, port);
                }
                else
                {
                    _clientFiltersProxyUriTextBox.Text = string.Format("{0}:{1}:{2}", match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
                }
            }
        }

        private void _clientFiltersConditionSchemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _clientFiltersConditionSchemeComboBox_PreviewMouseLeftButtonDown(this, null);
        }

        private void _clientFiltersConditionSchemeComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string scheme = Regex.Escape((string)((ComboBoxItem)_clientFiltersConditionSchemeComboBox.SelectedItem).Content);
            Regex regex = new Regex(@"(.*?):(.*)");
            Match match = regex.Match(_clientFiltersConditionTextBox.Text);

            if (!match.Success)
            {
                _clientFiltersConditionTextBox.Text = string.Format("{0}:.*", scheme);
            }
            else
            {
                _clientFiltersConditionTextBox.Text = string.Format("{0}:{1}", scheme, match.Groups[2].Value);
            }
        }

        private void _clientFilterAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clientFiltersConditionTextBox.Text == "") return;

            try
            {
                var connectionTypeText = (string)((ComboBoxItem)_clientFiltersConnectionTypeComboBox.SelectedItem).Content;
                var connectionType = (ConnectionType)new ConnectionTypeToStringConverter().ConvertBack(connectionTypeText, typeof(string), null, null);

                string proxyUri = null;
                var uriCondition = new UriCondition() { Value = _clientFiltersConditionTextBox.Text };

                if (connectionType != ConnectionType.None && connectionType != ConnectionType.Tcp)
                {
                    proxyUri = _clientFiltersProxyUriTextBox.Text;
                }

                _clientFilters.Add(new ConnectionFilter()
                {
                    ConnectionType = connectionType,
                    ProxyUri = proxyUri,
                    UriCondition = uriCondition,
                });

                _clientFiltersListView.Items.Refresh();
            }
            catch (ArgumentException)
            {

            }
            catch (UriFormatException)
            {

            }
            finally
            {
                _clientFiltersConditionTextBox.Text = "";
                _clientFiltersConditionSchemeComboBox_SelectionChanged(this, null);
            }
        }

        private void _clientFilterEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clientFiltersConditionTextBox.Text == "") return;

            var item = _clientFiltersListView.SelectedItem as ConnectionFilter;
            if (item == null) return;

            try
            {
                var connectionTypeText = (string)((ComboBoxItem)_clientFiltersConnectionTypeComboBox.SelectedItem).Content;
                var connectionType = (ConnectionType)new ConnectionTypeToStringConverter().ConvertBack(connectionTypeText, typeof(string), null, null);

                string proxyUri = null;
                var uriCondition = new UriCondition() { Value = _clientFiltersConditionTextBox.Text };

                if (connectionType != ConnectionType.None && connectionType != ConnectionType.Tcp)
                {
                    proxyUri = _clientFiltersProxyUriTextBox.Text;
                }

                item.ConnectionType = connectionType;
                item.ProxyUri = proxyUri;
                item.UriCondition = uriCondition;

                _clientFiltersListView.Items.Refresh();
            }
            catch (UriFormatException)
            {

            }
        }

        private void _clientFilterDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _clientFiltersListView.SelectedIndex;
            foreach (var item in _clientFiltersListView.SelectedItems.OfType<ConnectionFilter>().ToArray())
            {
                _clientFilters.Remove(item);
            }
            _clientFiltersListView.Items.Refresh();
            _clientFiltersListView.SelectedIndex = selectIndex;

            _clientFiltersConditionTextBox.Text = "";
            _clientFiltersConditionSchemeComboBox_SelectionChanged(this, null);
        }

        #endregion

        #region Server

        private void _serverListenUrisListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _serverListenUrisListView_PreviewMouseLeftButtonDown(this, null);
        }

        private void _serverListenUrisListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _serverListenUrisListView.SelectedItem as string;
            if (item == null) return;

            _serverListenUriTextBox.Text = item;

            Regex regex = new Regex(@"(.*?):(.*):(\d*)");
            Match match = regex.Match(item);

            if (match.Success)
            {
                var conboboxItem = _serverListenUriSchemeComboBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(n => (string)n.Content == match.Groups[1].Value);

                if (conboboxItem != null)
                {
                    conboboxItem.IsSelected = true;
                }
            }
        }

        private void _serverListenUriSchemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _serverListenUriSchemeComboBox_PreviewMouseLeftButtonDown(this, null);
        }

        private void _serverListenUriSchemeComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _serverListenUriSchemeComboBox.SelectedItem as ComboBoxItem;
            if (item == null) return;

            string scheme = (string)((ComboBoxItem)_serverListenUriSchemeComboBox.SelectedItem).Content;
            Regex regex = new Regex(@"(.*?):(.*):(\d*)");
            Match match = regex.Match(_serverListenUriTextBox.Text);

            if (!match.Success)
            {
                _serverListenUriTextBox.Text = string.Format("{0}:0.0.0.0:{1}", scheme, new Random().Next(1024, 65536));
            }
            else
            {
                _serverListenUriTextBox.Text = string.Format("{0}:{1}:{2}", scheme, match.Groups[2].Value, match.Groups[3].Value);
            }
        }

        private void _serverListenUriAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serverListenUriTextBox.Text == "") return;

            try
            {
                var uri = _serverListenUriTextBox.Text;
                if (_listenUris.Any(n => n == uri)) return;

                _listenUris.Add(uri);

                _serverListenUrisListView.Items.Refresh();
            }
            catch (UriFormatException)
            {

            }
            finally
            {
                _serverListenUriTextBox.Text = "";
                _serverListenUriSchemeComboBox_SelectionChanged(this, null);
            }
        }

        private void _serverListenUriEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serverListenUriTextBox.Text == "") return;

            var item = _serverListenUrisListView.SelectedItem as string;
            if (item == null) return;

            try
            {
                _listenUris[_listenUris.IndexOf(item)] = _serverListenUriTextBox.Text;

                _serverListenUrisListView.Items.Refresh();
            }
            catch (UriFormatException)
            {

            }
        }

        private void _serverListenUriDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _serverListenUrisListView.SelectedIndex;
            foreach (var item in _serverListenUrisListView.SelectedItems.OfType<string>().ToArray())
            {
                _listenUris.Remove(item);
            }
            _serverListenUrisListView.Items.Refresh();
            _serverListenUrisListView.SelectedIndex = selectIndex;

            _serverListenUriTextBox.Text = "";
            _serverListenUriSchemeComboBox_SelectionChanged(this, null);

        }

        #endregion

        #region Keywords

        private void _keywordsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _keywordsListView_PreviewMouseLeftButtonDown(this, null);
        }

        private void _keywordsListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _keywordsListView.SelectedItem as Keyword;
            if (item == null) return;

            _keywordTextBox.Text = item.Value;
        }

        private void _keywordAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keywordTextBox.Text == "") return;

            _keywords.Add(new Keyword() { HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512, Value = _keywordTextBox.Text });

            _keywordsListView.Items.Refresh();

            _keywordTextBox.Text = "";
        }

        private void _keywordEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keywordTextBox.Text == "") return;

            var item = _keywordsListView.SelectedItem as Keyword;
            if (item == null) return;

            item.Value = _keywordTextBox.Text;

            _keywordsListView.Items.Refresh();
        }

        private void _keywordDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _keywordsListView.SelectedIndex;
            foreach (var item in _keywordsListView.SelectedItems.OfType<Keyword>().ToArray())
            {
                _keywords.Remove(item);
            }
            _keywordsListView.Items.Refresh();
            _keywordsListView.SelectedIndex = selectIndex;

            _keywordTextBox.Text = "";
        }

        #endregion

        #region Miscellaneous

        private static int GetStringToInt(string value)
        {
            StringBuilder builder = new StringBuilder("0");

            foreach (var item in value)
            {
                if (Regex.IsMatch(item.ToString(), "[0-9]"))
                {
                    builder.Append(item.ToString());
                }
            }

            int count = 0;

            try
            {
                count = int.Parse(builder.ToString());
            }
            catch (OverflowException)
            {
                count = int.MaxValue;
            }

            return count;
        }

        private void _miscellaneousConnectionCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _miscellaneousConnectionCountTextBox.Text = ConnectionsWindow.GetStringToInt(_miscellaneousConnectionCountTextBox.Text).ToString();
        }

        private void _miscellaneousSearchingConnectionCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _miscellaneousDownloadingConnectionCountTextBox.Text = ConnectionsWindow.GetStringToInt(_miscellaneousDownloadingConnectionCountTextBox.Text).ToString();
        }

        private void _miscellaneousUploadingConnectionCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _miscellaneousUploadingConnectionCountTextBox.Text = ConnectionsWindow.GetStringToInt(_miscellaneousUploadingConnectionCountTextBox.Text).ToString();
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            using (DeadlockMonitor.Lock(_amoebaManager.ThisLock))
            {
                long size = (long)NetworkConverter.FromSizeString(_miscellaneousCacheSizeTextBox.Text);

                if (_amoebaManager.Size != size)
                {
                    _amoebaManager.Resize(size);
                }

                _amoebaManager.BaseNode = _myNode.DeepClone();
                _amoebaManager.SetOtherNodes(_otherNodes.Where(n => n != null && n.Id != null && n.Uris.Count != 0));

                _amoebaManager.SearchKeywords.Clear();
                _amoebaManager.SearchKeywords.AddRange(_keywords);

                int count = int.Parse(_miscellaneousConnectionCountTextBox.Text);
                _amoebaManager.ConnectionCountLimit = Math.Max(Math.Min(count, 50), 1);

                int scount = int.Parse(_miscellaneousDownloadingConnectionCountTextBox.Text);
                _amoebaManager.DownloadingConnectionCountLowerLimit = Math.Max(Math.Min(scount, 100), 1);

                int ucount = int.Parse(_miscellaneousUploadingConnectionCountTextBox.Text);
                _amoebaManager.UploadingConnectionCountLowerLimit = Math.Max(Math.Min(ucount, 100), 1);

                _amoebaManager.Filters.Clear();
                _amoebaManager.Filters.AddRange(_clientFilters.Select(n => n.DeepClone()));

                _amoebaManager.ListenUris.Clear();
                _amoebaManager.ListenUris.AddRange(_listenUris);

                string path = _miscellaneousDownloadDirectoryTextBox.Text;

                foreach (var item in System.IO.Path.GetInvalidPathChars())
                {
                    path = path.Replace(item.ToString(), "-");
                }

                _amoebaManager.DownloadDirectory = path;
            }

            Settings.Instance.Global_AutoUpdate_IsEnabled = _miscellaneousAutoUpdateCheckBox.IsChecked.Value;
            Settings.Instance.Global_Upnp_IsEnabled = _extendsUpnpCheckBox.IsChecked.Value;
            Settings.Instance.Global_Ipv4_IsEnabled = _extendsIpv4CheckBox.IsChecked.Value;
            Settings.Instance.Global_Ipv6_IsEnabled = _extendsIpv6CheckBox.IsChecked.Value;

            this.DialogResult = true;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
