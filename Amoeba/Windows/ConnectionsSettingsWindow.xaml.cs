using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

namespace Amoeba.Windows
{
    /// <summary>
    /// ConnectionsSettingsWindow.xaml の相互作用ロジック
    /// </summary>
    partial class ConnectionsSettingsWindow : Window
    {
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;
        private AutoBaseNodeSettingManager _autoBaseNodeSettingManager;

        private Node _baseNode;
        private NodeCollection _otherNodes = new NodeCollection();
        private ConnectionFilterCollection _clientFilters = new ConnectionFilterCollection();
        private UriCollection _listenUris = new UriCollection();

        public ConnectionsSettingsWindow(AmoebaManager amoebaManager, AutoBaseNodeSettingManager autoBaseNodeSettingManager, BufferManager bufferManager)
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

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            _baseNodeTextBoxUpdate();

            _baseNodeUrisListView.ItemsSource = _baseNode.Uris;
            _otherNodesListView.ItemsSource = _otherNodes;
            _clientFiltersListView.ItemsSource = _clientFilters;
            _serverListenUrisListView.ItemsSource = _listenUris;
            _dataDownloadDirectoryTextBox.Text = _amoebaManager.DownloadDirectory;
            _bandwidthConnectionCountTextBox.Text = _amoebaManager.ConnectionCountLimit.ToString();
            _dataCacheSizeTextBox.Text = NetworkConverter.ToSizeString(_amoebaManager.Size);
            _eventAutoBaseNodeSettingCheckBox.IsChecked = Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled;

            foreach (var item in Enum.GetValues(typeof(ConnectionType)).Cast<ConnectionType>())
            {
                _clientFiltersConnectionTypeComboBox.Items.Add(item);
            }

            _clientFiltersConnectionTypeComboBox.SelectedItem = ConnectionType.Tcp;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _baseNodeTreeViewItem.IsSelected = true;
        }

        #region BaseNode

        private void _baseNodeUriTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _baseNodeUriAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _baseNodeUrisListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _baseNodeUrisListView.SelectedItems;

            _baseNodeUrisListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _baseNodeUrisListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _baseNodeUrisListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var line = Clipboard.GetText().Split('\r', '\n');

                if (line.Length != 0)
                {
                    Regex regex = new Regex(@"(.+?):(.+)");

                    _baseNodeUrisListViewPasteMenuItem.IsEnabled = regex.IsMatch(line[0]);
                }
            }
        }

        private void _baseNodeUrisListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _baseNodeUriDeleteButton_Click(null, null);
        }

        private void _baseNodeUrisListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _baseNodeUrisListView.SelectedItems.OfType<string>())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _baseNodeUrisListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _baseNodeUrisListViewCopyMenuItem_Click(null, null);
            _baseNodeUriDeleteButton_Click(null, null);
        }

        private void _baseNodeUrisListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var uri in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    if (!Regex.IsMatch(uri, @"^(.+?):(.+)$") || _baseNode.Uris.Any(n => n == uri)) continue;
                    _baseNode.Uris.Add(uri);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _baseNodeUriTextBox.Text = "";
            _baseNodeUrisListView.SelectedIndex = _baseNode.Uris.Count - 1;

            _baseNodeUrisListView.Items.Refresh();
            _baseNodeUrisListView_SelectionChanged(this, null);

            var random = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[64];
            random.GetBytes(buffer);
            _baseNode.Id = buffer;

            _baseNodeTextBoxUpdate();
        }

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

                Regex regex = new Regex(@"^(.+?):(.*)$");
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
                Regex regex = new Regex(@"^(.+?):(.*)$");
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

            if (!Regex.IsMatch(uri, @"^(.+?):(.+)$") || _baseNode.Uris.Any(n => n == uri)) return;
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

            if (!Regex.IsMatch(uri, @"^(.+?):(.+)$") || _baseNode.Uris.Any(n => n == uri)) return;

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

        #endregion

        #region Client

        private void _clientFiltersProxyUriTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _clientFilterAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _clientFiltersConditionTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _clientFilterAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _clientFiltersListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _clientFiltersListView.SelectedItems;

            _clientFiltersListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _clientFiltersListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _clientFiltersListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var line = Clipboard.GetText().Split('\r', '\n');

                if (line.Length != 0)
                {
                    Regex regex = new Regex("^(.+?) \"(.*?)\" \"(.+)\"$");

                    _clientFiltersListViewPasteMenuItem.IsEnabled = regex.IsMatch(line[0]);
                }
            }
        }

        private void _clientFiltersListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _clientFilterDeleteButton_Click(null, null);
        }

        private void _clientFiltersListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _clientFiltersListView.SelectedItems.OfType<ConnectionFilter>())
            {
                sb.AppendLine(string.Format("{0} \"{1}\" \"{2}\"", item.ConnectionType, item.ProxyUri, item.UriCondition.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _clientFiltersListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _clientFiltersListViewCopyMenuItem_Click(null, null);
            _clientFilterDeleteButton_Click(null, null);
        }

        private void _clientFiltersListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex("^(.+?) \"(.*?)\" \"(.+)\"$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var connectionType = (ConnectionType)Enum.Parse(typeof(ConnectionType), match.Groups[1].Value);
                    var uriCondition = new UriCondition() { Value = match.Groups[3].Value };

                    string proxyUri = null;

                    if (!string.IsNullOrWhiteSpace(match.Groups[2].Value))
                    {
                        proxyUri = match.Groups[2].Value;
                        if (!Regex.IsMatch(proxyUri, @"^(.+?):(.+)$")) continue;
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

                    if (_clientFilters.Any(n => n == connectionFilter)) continue;
                    _clientFilters.Add(connectionFilter);
                }
                catch (Exception)
                {
                    return;
                }
            }

            _clientFiltersConditionTextBox.Text = "";
            _clientFiltersListView.SelectedIndex = _clientFilters.Count - 1;

            _clientFiltersListView.Items.Refresh();
            _clientFiltersListViewUpdate();
        }

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
                _clientFiltersConnectionTypeComboBox.SelectedItem = ConnectionType.Tcp;
                _clientFiltersConditionSchemeComboBox.SelectedIndex = 0;
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

                Regex regex = new Regex(@"(.+?):(.*)");
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

            _clientFiltersConnectionTypeComboBox.SelectedItem = item.ConnectionType;
        }

        private void _clientFiltersConnectionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _clientFiltersConnectionTypeComboBox_PreviewMouseLeftButtonDown(this, null);
        }

        private void _clientFiltersConnectionTypeComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var connectionType = (ConnectionType)_clientFiltersConnectionTypeComboBox.SelectedItem;

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
                Regex regex = new Regex(@"(.+?):(.+):(\d*)");
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
            Regex regex = new Regex(@"(.+?):(.+)");
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
                var connectionType = (ConnectionType)_clientFiltersConnectionTypeComboBox.SelectedItem;

                var uriCondition = new UriCondition() { Value = _clientFiltersConditionTextBox.Text };
                string proxyUri = null;

                if (!string.IsNullOrWhiteSpace(_clientFiltersProxyUriTextBox.Text))
                {
                    proxyUri = _clientFiltersProxyUriTextBox.Text;
                    if (!Regex.IsMatch(proxyUri, @"^(.+?):(.+)$")) return;
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
                var connectionType = (ConnectionType)_clientFiltersConnectionTypeComboBox.SelectedItem;

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

        private void _serverListenUriTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _serverListenUriAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _serverListenUrisListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _serverListenUrisListView.SelectedItems;

            _serverListenUrisListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _serverListenUrisListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _serverListenUrisListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var line = Clipboard.GetText().Split('\r', '\n');

                if (line.Length != 0)
                {
                    Regex regex = new Regex(@"(.+?):(.+)");

                    _serverListenUrisListViewPasteMenuItem.IsEnabled = regex.IsMatch(line[0]);
                }
            }
        }

        private void _serverListenUrisListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _serverListenUriDeleteButton_Click(null, null);
        }

        private void _serverListenUrisListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _serverListenUrisListView.SelectedItems.OfType<string>())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _serverListenUrisListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _serverListenUrisListViewCopyMenuItem_Click(null, null);
            _serverListenUriDeleteButton_Click(null, null);
        }

        private void _serverListenUrisListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"([\+-]) (.*)");

            foreach (var uri in Clipboard.GetText().Split('\r', '\n'))
            {
                if (!Regex.IsMatch(uri, @"^(.+?):(.+)$") || _listenUris.Any(n => n == uri)) continue;
                _listenUris.Add(uri);
            }

            _serverListenUriTextBox.Text = "";
            _serverListenUrisListView.SelectedIndex = _listenUris.Count - 1;

            _serverListenUrisListView.Items.Refresh();
            _serverListenUrisListViewUpdate();
        }

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

            Regex regex = new Regex(@"(.+?):(.*)");
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
            Regex regex = new Regex(@"(.+?):(.*)");
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

            if (!Regex.IsMatch(uri, @"^(.+?):(.+)$") || _listenUris.Any(n => n == uri)) return;
            _listenUris.Add(uri);

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

            if (!Regex.IsMatch(uri, @"^(.+?):(.+)$") || _listenUris.Any(n => n == uri)) return;

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

        #region Data

        private void _dataDownloadDirectoryTextBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
                dialog.SelectedPath = _dataDownloadDirectoryTextBox.Text;
                dialog.ShowNewFolderButton = true;
                dialog.Description = LanguagesManager.Instance.ConnectionsSettingsWindow_DownloadDirectory;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _dataDownloadDirectoryTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        #endregion

        #region Bandwidth

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

        private void _bandwidthConnectionCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_bandwidthConnectionCountTextBox.Text)) return;

            StringBuilder builder = new StringBuilder("");

            foreach (var item in _bandwidthConnectionCountTextBox.Text)
            {
                if (Regex.IsMatch(item.ToString(), "[0-9]"))
                {
                    builder.Append(item.ToString());
                }
            }

            var value = builder.ToString();
            if (_bandwidthConnectionCountTextBox.Text != value) _bandwidthConnectionCountTextBox.Text = value;
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
                    size = Math.Abs((long)NetworkConverter.FromSizeString(_dataCacheSizeTextBox.Text));
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

                int count = ConnectionsSettingsWindow.GetStringToInt(_bandwidthConnectionCountTextBox.Text);
                _amoebaManager.ConnectionCountLimit = Math.Max(Math.Min(count, 50), 1);

                _amoebaManager.Filters.Clear();
                _amoebaManager.Filters.AddRange(_clientFilters.Select(n => n.DeepClone()));

                if (!Collection.Equals(_amoebaManager.ListenUris, _listenUris))
                {
                    _amoebaManager.ListenUris.Clear();
                    _amoebaManager.ListenUris.AddRange(_listenUris);

                    flag = true;
                }

                string path = _dataDownloadDirectoryTextBox.Text;

                foreach (var item in System.IO.Path.GetInvalidPathChars())
                {
                    path = path.Replace(item.ToString(), "-");
                }

                _amoebaManager.DownloadDirectory = path;
            }

            if (flag && _eventAutoBaseNodeSettingCheckBox.IsChecked.Value
                && _autoBaseNodeSettingManager.State == ManagerState.Start)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
                {
                    _autoBaseNodeSettingManager.Update();
                }));
            }

            Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled = _eventAutoBaseNodeSettingCheckBox.IsChecked.Value;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_baseNodeTreeViewItem.IsSelected)
            {
                _baseNodeUrisListViewDeleteMenuItem_Click(null, null);
            }
            else if (_otherNodesTreeViewItem.IsSelected)
            {

            }
            else if (_clientTreeViewItem.IsSelected)
            {
                _clientFiltersListViewDeleteMenuItem_Click(null, null);
            }
            else if (_serverTreeViewItem.IsSelected)
            {
                _serverListenUrisListViewDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_baseNodeTreeViewItem.IsSelected)
            {
                _baseNodeUrisListViewCopyMenuItem_Click(null, null);
            }
            else if (_otherNodesTreeViewItem.IsSelected)
            {
                _otherNodesCopyMenuItem_Click(null, null);
            }
            else if (_clientTreeViewItem.IsSelected)
            {
                _clientFiltersListViewCopyMenuItem_Click(null, null);
            }
            else if (_serverTreeViewItem.IsSelected)
            {
                _serverListenUrisListViewCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_baseNodeTreeViewItem.IsSelected)
            {
                _baseNodeUrisListViewCutMenuItem_Click(null, null);
            }
            else if (_otherNodesTreeViewItem.IsSelected)
            {

            }
            else if (_clientTreeViewItem.IsSelected)
            {
                _clientFiltersListViewCutMenuItem_Click(null, null);
            }
            else if (_serverTreeViewItem.IsSelected)
            {
                _serverListenUrisListViewCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            if (_baseNodeTreeViewItem.IsSelected)
            {
                _baseNodeUrisListViewPasteMenuItem_Click(null, null);
            }
            else if (_otherNodesTreeViewItem.IsSelected)
            {
                _otherNodesPasteMenuItem_Click(null, null);
            }
            else if (_clientTreeViewItem.IsSelected)
            {
                _clientFiltersListViewPasteMenuItem_Click(null, null);
            }
            else if (_serverTreeViewItem.IsSelected)
            {
                _serverListenUrisListViewPasteMenuItem_Click(null, null);
            }
        }
    }
}
