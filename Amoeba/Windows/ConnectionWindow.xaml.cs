using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Amoeba.Properties;
using Library;
using Library.Net.Amoeba;
using System.Threading;

namespace Amoeba.Windows
{
    /// <summary>
    /// ConnectionWindow.xaml の相互作用ロジック
    /// </summary>
    partial class ConnectionWindow : Window
    {
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;
        private AutoBaseNodeSettingManager _autoBaseNodeSettingManager;

        private Node _baseNode;
        private NodeCollection _otherNodes = new NodeCollection();
        private ConnectionFilterCollection _clientFilters = new ConnectionFilterCollection();
        private UriCollection _listenUris = new UriCollection();

        public ConnectionWindow(AmoebaManager amoebaManager, AutoBaseNodeSettingManager autoBaseNodeSettingManager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _autoBaseNodeSettingManager = autoBaseNodeSettingManager;
            _bufferManager = bufferManager;

            lock (_amoebaManager.ThisLock)
            {
                _baseNode = _amoebaManager.BaseNode.DeepClone();
                _otherNodes.AddRange(_amoebaManager.OtherNodes.Select(n => n.DeepClone()));
                _clientFilters.AddRange(_amoebaManager.Filters.Select(n => n.DeepClone()));
                _listenUris.AddRange(_amoebaManager.ListenUris);
            }

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _baseNodeTextBoxUpdate();

            _baseNodeUrisListView.ItemsSource = _baseNode.Uris;
            _otherNodesListView.ItemsSource = _otherNodes;
            _clientFiltersListView.ItemsSource = _clientFilters;
            _serverListenUrisListView.ItemsSource = _listenUris;
            _miscellaneousDownloadDirectoryTextBox.Text = _amoebaManager.DownloadDirectory;
            _miscellaneousConnectionCountTextBox.Text = _amoebaManager.ConnectionCountLimit.ToString();
            _miscellaneousDownloadingConnectionCountTextBox.Text = _amoebaManager.DownloadingConnectionCountLowerLimit.ToString();
            _miscellaneousUploadingConnectionCountTextBox.Text = _amoebaManager.UploadingConnectionCountLowerLimit.ToString();
            _miscellaneousCacheSizeTextBox.Text = NetworkConverter.ToSizeString(_amoebaManager.Size);
            _miscellaneousAutoBaseNodeSettingCheckBox.IsChecked = Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled;

            foreach (var item in Enum.GetValues(typeof(ConnectionType)))
            {
                _clientFiltersConnectionTypeComboBox.Items.Add(new ComboBoxItem() { Content = new ConnectionTypeToStringConverter().Convert(item, typeof(ConnectionType), null, null) });
            }

            _clientFiltersConnectionTypeComboBox.SelectedItem = _clientFiltersConnectionTypeComboBox.Items.GetItemAt(1);
        }

        #region BaseNode

        private void _baseNodeTextBoxUpdate()
        {
            if (_baseNode.Uris.Count > 0)
            {
                _baseNodeTextBox.Text = AmoebaConverter.ToNodeString(_baseNode);
            }
            else
            {
                _baseNodeTextBox.Text = "";
            }

            _baseNodeUrisListView_SelectionChanged(this, null);
        }

        private void _baseNodeTextBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _baseNodeTextBox.SelectAll();
        }

        private void _baseNodeUrisListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _baseNodeUrisListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _baseNodeUriUpButton.IsEnabled = false;
                    _baseNodeUriDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _baseNodeUriUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _baseNodeUriUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _baseNode.Uris.Count - 1)
                    {
                        _baseNodeUriDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _baseNodeUriDownButton.IsEnabled = true;
                    }
                }

                _baseNodeUrisListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _baseNodeUrisListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _baseNodeUrisListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _baseNodeUriTextBox.Text = "tcp:";
                ((ComboBoxItem)_baseNodeUriSchemeComboBox.Items[0]).IsSelected = true;

                return;
            }

            var item = _baseNodeUrisListView.SelectedItem as string;
            if (item == null) return;

            try
            {
                _baseNodeUriTextBox.Text = item;

                Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                Match match = regex.Match(item);

                if (match.Success)
                {
                    var conboboxItem = _baseNodeUriSchemeComboBox.Items.Cast<ComboBoxItem>()
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

        private void _baseNodeUriSchemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _baseNodeUriSchemeComboBox_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _baseNodeUriSchemeComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _baseNodeUriSchemeComboBox.SelectedItem as ComboBoxItem;
            if (item == null) return;

            try
            {
                string scheme = (string)((ComboBoxItem)_baseNodeUriSchemeComboBox.SelectedItem).Content;
                Regex regex = new Regex(@"(.*?):(.*)");
                Match match = regex.Match(_baseNodeUriTextBox.Text);

                if (!match.Success)
                {
                    _baseNodeUriTextBox.Text = string.Format("{0}:", scheme);
                }
                else
                {
                    _baseNodeUriTextBox.Text = string.Format("{0}:{1}", scheme, match.Groups[2].Value);
                }
            }
            catch (Exception)
            {

            }
        }

        private void _baseNodeUriUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _baseNodeUrisListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _baseNodeUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            _baseNode.Uris.Remove(item);
            _baseNode.Uris.Insert(selectIndex - 1, item);
            _baseNodeUrisListView.Items.Refresh();

            _baseNodeTextBoxUpdate();
        }

        private void _baseNodeUriDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _baseNodeUrisListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _baseNodeUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            _baseNode.Uris.Remove(item);
            _baseNode.Uris.Insert(selectIndex + 1, item);
            _baseNodeUrisListView.Items.Refresh();

            _baseNodeTextBoxUpdate();
        }

        private void _baseNodeUriAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_baseNodeUriTextBox.Text == "") return;

            var uri = _baseNodeUriTextBox.Text;
            if (!Regex.IsMatch(uri, @"(.*?):(.+)")
                || _baseNode.Uris.Any(n => n == uri)) return;

            _baseNodeUriTextBox.Text = "";

            _baseNode.Uris.Add(uri);
            _baseNodeUrisListView.SelectedIndex = _baseNode.Uris.Count - 1;
            _baseNodeUrisListView.Items.Refresh();
            _baseNodeUrisListView_SelectionChanged(this, null);

            var random = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[64];
            random.GetBytes(buffer);
            _baseNode.Id = buffer;

            _baseNodeTextBoxUpdate();
        }

        private void _baseNodeUriEditButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _baseNodeUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            if (_baseNodeUriTextBox.Text == "") return;

            var uri = _baseNodeUriTextBox.Text;
            if (!Regex.IsMatch(uri, @"(.*?):(.+)")
                || _baseNode.Uris.Any(n => n == uri)) return;

            var item = _baseNodeUrisListView.SelectedItem as string;
            if (item == null) return;

            _baseNode.Uris[_baseNode.Uris.IndexOf(item)] = _baseNodeUriTextBox.Text;
            _baseNodeUrisListView.Items.Refresh();

            _baseNodeUrisListView.SelectedIndex = selectIndex;

            var random = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[64];
            random.GetBytes(buffer);
            _baseNode.Id = buffer;

            _baseNodeTextBoxUpdate();
        }

        private void _baseNodeUriDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _baseNodeUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _baseNodeUrisListView.SelectedItems.OfType<string>().ToArray())
            {
                _baseNode.Uris.Remove(item);
            }

            _baseNodeUriTextBox.Text = "";

            _baseNodeUrisListView.Items.Refresh();
            _baseNodeUrisListView.SelectedIndex = selectIndex;
            _baseNodeUrisListView_SelectionChanged(this, null);

            var random = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[64];
            random.GetBytes(buffer);
            _baseNode.Id = buffer;

            _baseNodeTextBoxUpdate();
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
            {
                _otherNodesUrisTextBox.Text = "";
                return;
            }

            StringBuilder builder = new StringBuilder();

            foreach (var item in node.Uris)
            {
                builder.Append(item + ", ");
            }

            if (builder.Length <= 2) _otherNodesUrisTextBox.Text = "";
            else _otherNodesUrisTextBox.Text = builder.ToString().Remove(builder.Length - 2);
        }

        private void _otherNodesListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _otherNodesListView.SelectedItems;

            _otherNodesCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var nodes = Clipboard.GetNodes();

                _otherNodesPasteMenuItem.IsEnabled = (nodes.Count() > 0) ? true : false;
            }
        }

        private void _otherNodesCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _otherNodesListView.SelectedItems.OfType<Node>())
            {
                sb.AppendLine(AmoebaConverter.ToNodeString(item));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _otherNodesPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Clipboard.GetNodes())
            {
                if (_otherNodes.Contains(item)) continue;

                _otherNodes.Add(item);
            }

            _otherNodesListView.Items.Refresh();
        }

        #endregion

        #region Client

        private void _clientFiltersListViewUpdate()
        {
            _clientFiltersListView_SelectionChanged(this, null);
        }

        private void _clientFiltersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _clientFiltersListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _clientFilterUpButton.IsEnabled = false;
                    _clientFilterDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _clientFilterUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _clientFilterUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _clientFilters.Count - 1)
                    {
                        _clientFilterDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _clientFilterDownButton.IsEnabled = true;
                    }
                }

                _clientFiltersListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _clientFiltersListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _clientFiltersListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _clientFiltersProxyUriTextBox.Text = "";
                _clientFiltersConditionTextBox.Text = "tcp:.*";
                ((ComboBoxItem)_clientFiltersConnectionTypeComboBox.Items[1]).IsSelected = true;
                ((ComboBoxItem)_clientFiltersConditionSchemeComboBox.Items[0]).IsSelected = true;
                return;
            }

            var item = _clientFiltersListView.SelectedItem as ConnectionFilter;
            if (item == null) return;

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

                Regex regex = new Regex(@"(.*?):(.*)");
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
                _clientFiltersProxyUriTextBox.Text = "";
            }
            else
            {
                _clientFiltersProxyUriTextBox.IsEnabled = true;

                if (!string.IsNullOrWhiteSpace(_clientFiltersProxyUriTextBox.Text)) return;

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

        private void _clientFilterUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _clientFiltersListView.SelectedItem as ConnectionFilter;
            if (item == null) return;

            var selectIndex = _clientFiltersListView.SelectedIndex;
            if (selectIndex == -1) return;

            _clientFilters.Remove(item);
            _clientFilters.Insert(selectIndex - 1, item);
            _clientFiltersListView.Items.Refresh();

            _clientFiltersListViewUpdate();
        }

        private void _clientFilterDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _clientFiltersListView.SelectedItem as ConnectionFilter;
            if (item == null) return;

            var selectIndex = _clientFiltersListView.SelectedIndex;
            if (selectIndex == -1) return;

            _clientFilters.Remove(item);
            _clientFilters.Insert(selectIndex + 1, item);
            _clientFiltersListView.Items.Refresh();

            _clientFiltersListViewUpdate();
        }

        private void _clientFilterAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clientFiltersConditionTextBox.Text == "") return;

            try
            {
                var connectionTypeText = (string)((ComboBoxItem)_clientFiltersConnectionTypeComboBox.SelectedItem).Content;
                var connectionType = (ConnectionType)new ConnectionTypeToStringConverter().ConvertBack(connectionTypeText, typeof(string), null, null);

                var uriCondition = new UriCondition() { Value = _clientFiltersConditionTextBox.Text };
                string proxyUri = null;

                if (!string.IsNullOrWhiteSpace(_clientFiltersProxyUriTextBox.Text))
                {
                    proxyUri = _clientFiltersProxyUriTextBox.Text;
                }

                if (connectionType == ConnectionType.Socks4Proxy
                    || connectionType == ConnectionType.Socks4aProxy
                    || connectionType == ConnectionType.Socks5Proxy
                    || connectionType == ConnectionType.HttpProxy)
                {
                    if (proxyUri == null) return;
                }

                var connectionFilter = new ConnectionFilter()
                {
                    ConnectionType = connectionType,
                    ProxyUri = proxyUri,
                    UriCondition = uriCondition,
                };

                if (_clientFilters.Any(n => n == connectionFilter)) return;
                _clientFilters.Add(connectionFilter);

                _clientFiltersConditionTextBox.Text = "";
                _clientFiltersListView.SelectedIndex = _clientFilters.Count - 1;
            }
            catch (Exception)
            {
                return;
            }

            _clientFiltersListView.Items.Refresh();
            _clientFiltersListViewUpdate();
        }

        private void _clientFilterEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clientFiltersConditionTextBox.Text == "") return;

            var item = _clientFiltersListView.SelectedItem as ConnectionFilter;
            if (item == null) return;

            int selectIndex = _clientFiltersListView.SelectedIndex;
            if (selectIndex == -1) return;

            try
            {
                var connectionTypeText = (string)((ComboBoxItem)_clientFiltersConnectionTypeComboBox.SelectedItem).Content;
                var connectionType = (ConnectionType)new ConnectionTypeToStringConverter().ConvertBack(connectionTypeText, typeof(string), null, null);

                string proxyUri = null;
                var uriCondition = new UriCondition() { Value = _clientFiltersConditionTextBox.Text };

                if (!string.IsNullOrWhiteSpace(_clientFiltersProxyUriTextBox.Text))
                {
                    proxyUri = _clientFiltersProxyUriTextBox.Text;
                }

                if (connectionType == ConnectionType.Socks4Proxy
                   || connectionType == ConnectionType.Socks4aProxy
                   || connectionType == ConnectionType.Socks5Proxy
                   || connectionType == ConnectionType.HttpProxy)
                {
                    if (proxyUri == null) return;
                }

                var connectionFilter = new ConnectionFilter()
                {
                    ConnectionType = connectionType,
                    ProxyUri = proxyUri,
                    UriCondition = uriCondition,
                };

                if (_clientFilters.Any(n => n == connectionFilter)) return;

                item.ConnectionType = connectionType;
                item.ProxyUri = proxyUri;
                item.UriCondition = uriCondition;

                _clientFiltersListView.SelectedIndex = selectIndex;
            }
            catch (Exception)
            {
                return;
            }

            _clientFiltersListView.Items.Refresh();
            _clientFiltersListViewUpdate();
        }

        private void _clientFilterDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _clientFiltersListView.SelectedIndex;
            if (selectIndex == -1) return;

            _clientFiltersConditionTextBox.Text = "";

            foreach (var item in _clientFiltersListView.SelectedItems.OfType<ConnectionFilter>().ToArray())
            {
                _clientFilters.Remove(item);
            }

            _clientFiltersListView.Items.Refresh();
            _clientFiltersListView.SelectedIndex = selectIndex;
            _clientFiltersListViewUpdate();
        }

        #endregion

        #region Server

        private void _serverListenUrisListViewUpdate()
        {
            _serverListenUrisListView_SelectionChanged(this, null);
        }

        private void _serverListenUrisListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _serverListenUrisListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _serverListenUriUpButton.IsEnabled = false;
                    _serverListenUriDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _serverListenUriUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _serverListenUriUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _listenUris.Count - 1)
                    {
                        _serverListenUriDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _serverListenUriDownButton.IsEnabled = true;
                    }
                }

                _serverListenUrisListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _serverListenUrisListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _serverListenUrisListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _serverListenUriTextBox.Text = string.Format("tcp:0.0.0.0:{0}", new Random().Next(1024, 65536));
                ((ComboBoxItem)_serverListenUriSchemeComboBox.Items[0]).IsSelected = true;

                return;
            }

            var item = _serverListenUrisListView.SelectedItem as string;
            if (item == null) return;

            _serverListenUriTextBox.Text = item;

            Regex regex = new Regex(@"(.*?):(.*)");
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
            Regex regex = new Regex(@"(.*?):(.*)");
            Match match = regex.Match(_serverListenUriTextBox.Text);

            if (!match.Success)
            {
                _serverListenUriTextBox.Text = string.Format("{0}:0.0.0.0:{1}", scheme, new Random().Next(1024, 65536));
            }
            else
            {
                _serverListenUriTextBox.Text = string.Format("{0}:{1}", scheme, match.Groups[2].Value);
            }
        }

        private void _serverListenUriUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _serverListenUrisListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _serverListenUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            _listenUris.Remove(item);
            _listenUris.Insert(selectIndex - 1, item);
            _serverListenUrisListView.Items.Refresh();

            _serverListenUrisListViewUpdate();
        }

        private void _serverListenUriDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _serverListenUrisListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _serverListenUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            _listenUris.Remove(item);
            _listenUris.Insert(selectIndex + 1, item);
            _serverListenUrisListView.Items.Refresh();

            _serverListenUrisListViewUpdate();
        }

        private void _serverListenUriAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serverListenUriTextBox.Text == "") return;

            var uri = _serverListenUriTextBox.Text;
            if (_listenUris.Any(n => n == uri)) return;
            _listenUris.Add(uri);

            _serverListenUriTextBox.Text = "";
            _serverListenUrisListView.SelectedIndex = _listenUris.Count - 1;

            _serverListenUrisListView.Items.Refresh();
            _serverListenUrisListViewUpdate();
        }

        private void _serverListenUriEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serverListenUriTextBox.Text == "") return;

            int selectIndex = _serverListenUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            var uri = _serverListenUriTextBox.Text;
            if (_listenUris.Any(n => n == uri)) return;

            var item = _serverListenUrisListView.SelectedItem as string;
            if (item == null) return;

            _listenUris[_listenUris.IndexOf(item)] = _serverListenUriTextBox.Text;

            _serverListenUrisListView.SelectedIndex = selectIndex;
            _serverListenUrisListView.Items.Refresh();
            _serverListenUrisListViewUpdate();
        }

        private void _serverListenUriDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _serverListenUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            _serverListenUriTextBox.Text = "";

            foreach (var item in _serverListenUrisListView.SelectedItems.OfType<string>().ToArray())
            {
                _listenUris.Remove(item);
            }

            _serverListenUrisListView.Items.Refresh();
            _serverListenUrisListView.SelectedIndex = selectIndex;
            _serverListenUriSchemeComboBox_SelectionChanged(this, null);
            _serverListenUrisListViewUpdate();
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

        private void _miscellaneousStackPanel_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Expander expander = e.Source as Expander;
            if (expander == null) return;

            foreach (var item in _miscellaneousStackPanel.Children.OfType<Expander>())
            {
                if (expander != item) item.IsExpanded = false;
            }
        }

        private void _miscellaneousConnectionCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_miscellaneousConnectionCountTextBox.Text)) return;

            StringBuilder builder = new StringBuilder("");

            foreach (var item in _miscellaneousConnectionCountTextBox.Text)
            {
                if (Regex.IsMatch(item.ToString(), "[0-9]"))
                {
                    builder.Append(item.ToString());
                }
            }

            var value = builder.ToString();
            if (_miscellaneousConnectionCountTextBox.Text != value) _miscellaneousConnectionCountTextBox.Text = value;
        }

        private void _miscellaneousCacheConnectionCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_miscellaneousDownloadingConnectionCountTextBox.Text)) return;

            StringBuilder builder = new StringBuilder("");

            foreach (var item in _miscellaneousDownloadingConnectionCountTextBox.Text)
            {
                if (Regex.IsMatch(item.ToString(), "[0-9]"))
                {
                    builder.Append(item.ToString());
                }
            }

            var value = builder.ToString();
            if (_miscellaneousDownloadingConnectionCountTextBox.Text != value) _miscellaneousDownloadingConnectionCountTextBox.Text = value;
        }

        private void _miscellaneousUploadingConnectionCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_miscellaneousUploadingConnectionCountTextBox.Text)) return;

            StringBuilder builder = new StringBuilder("");

            foreach (var item in _miscellaneousUploadingConnectionCountTextBox.Text)
            {
                if (Regex.IsMatch(item.ToString(), "[0-9]"))
                {
                    builder.Append(item.ToString());
                }
            }

            var value = builder.ToString();
            if (_miscellaneousUploadingConnectionCountTextBox.Text != value) _miscellaneousUploadingConnectionCountTextBox.Text = value;
        }

        private void _miscellaneousDownloadDirectoryTextBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
                dialog.SelectedPath = _miscellaneousDownloadDirectoryTextBox.Text;
                dialog.ShowNewFolderButton = true;
                dialog.Description = LanguagesManager.Instance.ConnectionWindow_DownloadDirectory;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _miscellaneousDownloadDirectoryTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            bool flag = false;

            lock (_amoebaManager.ThisLock)
            {
                long size = (long)NetworkConverter.FromSizeString("50 GB");

                try
                {
                    size = Math.Abs((long)NetworkConverter.FromSizeString(_miscellaneousCacheSizeTextBox.Text));
                }
                catch (Exception)
                {

                }

                if (_amoebaManager.Size != size)
                {
                    _amoebaManager.Resize(size);
                }

                _amoebaManager.BaseNode = _baseNode.DeepClone();
                _amoebaManager.SetOtherNodes(_otherNodes.Where(n => n != null && n.Id != null && n.Uris.Count != 0));

                int count = ConnectionWindow.GetStringToInt(_miscellaneousConnectionCountTextBox.Text);
                _amoebaManager.ConnectionCountLimit = Math.Max(Math.Min(count, 50), 1);

                int scount = ConnectionWindow.GetStringToInt(_miscellaneousDownloadingConnectionCountTextBox.Text);
                _amoebaManager.DownloadingConnectionCountLowerLimit = Math.Max(Math.Min(scount, 50), 1);

                int ucount = ConnectionWindow.GetStringToInt(_miscellaneousUploadingConnectionCountTextBox.Text);
                _amoebaManager.UploadingConnectionCountLowerLimit = Math.Max(Math.Min(ucount, 50), 1);

                _amoebaManager.Filters.Clear();
                _amoebaManager.Filters.AddRange(_clientFilters.Select(n => n.DeepClone()));

                if (!Collection.Equals(_amoebaManager.ListenUris, _listenUris))
                {
                    _amoebaManager.ListenUris.Clear();
                    _amoebaManager.ListenUris.AddRange(_listenUris);

                    flag = true;
                }

                string path = _miscellaneousDownloadDirectoryTextBox.Text;

                foreach (var item in System.IO.Path.GetInvalidPathChars())
                {
                    path = path.Replace(item.ToString(), "-");
                }

                _amoebaManager.DownloadDirectory = path;
            }

            lock (_autoBaseNodeSettingManager.ThisLock)
            {
                if (flag && _autoBaseNodeSettingManager.State == ManagerState.Start)
                {
                    _autoBaseNodeSettingManager.Restart();
                }
            }

            Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled = _miscellaneousAutoBaseNodeSettingCheckBox.IsChecked.Value;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
