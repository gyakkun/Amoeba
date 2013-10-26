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
    /// CoreOptionsWindow.xaml の相互作用ロジック
    /// </summary>
    partial class CoreOptionsWindow : Window
    {
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;
        private AutoBaseNodeSettingManager _autoBaseNodeSettingManager;
        private TransfarLimitManager _transferLimitManager;

        private byte[] _baseNode_Id;
        private ObservableCollectionEx<string> _baseNode_Uris;
        private ObservableCollectionEx<Node> _otherNodes;
        private ObservableCollectionEx<ConnectionFilter> _clientFilters;
        private ObservableCollectionEx<string> _serverListenUris;

        public CoreOptionsWindow(AmoebaManager amoebaManager, AutoBaseNodeSettingManager autoBaseNodeSettingManager, TransfarLimitManager transfarLimitManager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _autoBaseNodeSettingManager = autoBaseNodeSettingManager;
            _transferLimitManager = transfarLimitManager;
            _bufferManager = bufferManager;

            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            foreach (var item in Enum.GetValues(typeof(ConnectionType)).Cast<ConnectionType>())
            {
                _clientFiltersConnectionTypeComboBox.Items.Add(item);
            }

            foreach (var item in Enum.GetValues(typeof(TransferLimitType)).Cast<TransferLimitType>())
            {
                _transferLimitTypeComboBox.Items.Add(item);
            }

            foreach (var u in new string[] { "Byte", "KB", "MB", "GB", "TB" })
            {
                _dataCacheSizeComboBox.Items.Add(u);
            }

            _dataCacheSizeComboBox.SelectedItem = Settings.Instance.CoreOptionsWindow_DataCacheSize_Unit;

            foreach (var u in new string[] { "Byte", "KB", "MB", "GB", })
            {
                _bandwidthLimitComboBox.Items.Add(u);
            }

            _bandwidthLimitComboBox.SelectedItem = Settings.Instance.CoreOptionsWindow_BandwidthLimit_Unit;

            lock (_amoebaManager.ThisLock)
            {
                var baseNode = _amoebaManager.BaseNode;

                _baseNode_Id = baseNode.Id;
                _baseNode_Uris = new ObservableCollectionEx<string>(baseNode.Uris);
                _otherNodes = new ObservableCollectionEx<Node>(_amoebaManager.OtherNodes.Select(n => n.DeepClone()));
                _clientFilters = new ObservableCollectionEx<ConnectionFilter>(_amoebaManager.Filters.Select(n => n.DeepClone()));
                _serverListenUris = new ObservableCollectionEx<string>(_amoebaManager.ListenUris);

                try
                {
                    _dataCacheSizeTextBox.Text = NetworkConverter.ToSizeString(_amoebaManager.Size, Settings.Instance.CoreOptionsWindow_DataCacheSize_Unit);
                }
                catch (Exception)
                {
                    _dataCacheSizeTextBox.Text = "";
                }

                _dataDownloadDirectoryTextBox.Text = _amoebaManager.DownloadDirectory;

                _bandwidthConnectionCountTextBox.Text = _amoebaManager.ConnectionCountLimit.ToString();

                try
                {
                    _bandwidthLimitTextBox.Text = NetworkConverter.ToSizeString(_amoebaManager.BandWidthLimit, Settings.Instance.CoreOptionsWindow_BandwidthLimit_Unit);
                }
                catch (Exception)
                {
                    _bandwidthLimitTextBox.Text = "";
                }
            }

            _baseNodeUrisListView.ItemsSource = _baseNode_Uris;
            _otherNodesListView.ItemsSource = _otherNodes;
            _clientFiltersListView.ItemsSource = _clientFilters;
            _serverListenUrisListView.ItemsSource = _serverListenUris;

            _baseNodeUpdate();
            _otherNodesUpdate();
            _clientFiltersListViewUpdate();
            _serverListenUrisUpdate();

            lock (_transferLimitManager.ThisLock)
            {
                _transferLimitSpanTextBox.Text = _transferLimitManager.TransferLimit.Span.ToString();
                _transferLimitSizeTextBox.Text = NetworkConverter.ToSizeString(_transferLimitManager.TransferLimit.Size);
                _transferLimitTypeComboBox.SelectedItem = _transferLimitManager.TransferLimit.Type;

                _transferInfoUploadedLabel.Content = NetworkConverter.ToSizeString(_transferLimitManager.TotalUploadSize);
                _transferInfoDownloadedLabel.Content = NetworkConverter.ToSizeString(_transferLimitManager.TotalDownloadSize);
                _transferInfoTotalLabel.Content = NetworkConverter.ToSizeString(_transferLimitManager.TotalUploadSize + _transferLimitManager.TotalDownloadSize);
            }

            _eventAutoBaseNodeSettingCheckBox.IsChecked = Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled;
        }

        protected override void OnInitialized(EventArgs e)
        {
            WindowPosition.Move(this);

            base.OnInitialized(e);
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
                if (_baseNodeUrisListView.SelectedIndex == -1)
                {
                    _baseNodeUriAddButton_Click(null, null);
                }
                else
                {
                    _baseNodeUriEditButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

        private void _baseNodeUpdate()
        {
            if (_baseNode_Uris.Count > 0)
            {
                var node = new Node(_baseNode_Id, _baseNode_Uris);

                _baseNodeTextBox.Text = AmoebaConverter.ToNodeString(node);
            }
            else
            {
                _baseNodeTextBox.Text = "";
            }

            _baseNodeUrisListView_SelectionChanged(this, null);
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

                    if (selectIndex == _baseNode_Uris.Count - 1)
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

        private void _baseNodeTextBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _baseNodeTextBox.SelectAll();
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

        private void _baseNodeUrisListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _baseNodeUrisListViewCopyMenuItem_Click(null, null);
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

        private void _baseNodeUrisListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var uri in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    if (!Regex.IsMatch(uri, @"^(.+?):(.+)$") || _baseNode_Uris.Any(n => n == uri)) continue;
                    _baseNode_Uris.Add(uri);
                }
                catch (Exception)
                {

                }
            }

            var random = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[64];
            random.GetBytes(buffer);
            _baseNode_Id = buffer;

            _baseNodeUpdate();
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

            _baseNode_Uris.Move(selectIndex, selectIndex - 1);

            _baseNodeUpdate();
        }

        private void _baseNodeUriDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _baseNodeUrisListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _baseNodeUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            _baseNode_Uris.Move(selectIndex, selectIndex + 1);

            _baseNodeUpdate();
        }

        private void _baseNodeUriAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_baseNodeUriTextBox.Text)) return;

            var uri = _baseNodeUriTextBox.Text;

            if (!Regex.IsMatch(uri, @"^(.+?):(.+)$") || _baseNode_Uris.Any(n => n == uri)) return;
            _baseNode_Uris.Add(uri);

            var random = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[64];
            random.GetBytes(buffer);
            _baseNode_Id = buffer;

            _baseNodeUpdate();
        }

        private void _baseNodeUriEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_baseNodeUriTextBox.Text)) return;

            int selectIndex = _baseNodeUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            var uri = _baseNodeUriTextBox.Text;

            if (!Regex.IsMatch(uri, @"^(.+?):(.+)$") || _baseNode_Uris.Any(n => n == uri)) return;
            _baseNode_Uris.Set(selectIndex, uri);

            _baseNodeUrisListView.SelectedIndex = selectIndex;

            var random = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[64];
            random.GetBytes(buffer);
            _baseNode_Id = buffer;

            _baseNodeUpdate();
        }

        private void _baseNodeUriDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _baseNodeUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _baseNodeUrisListView.SelectedItems.OfType<string>().ToArray())
            {
                _baseNode_Uris.Remove(item);
            }

            var random = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[64];
            random.GetBytes(buffer);
            _baseNode_Id = buffer;

            _baseNodeUpdate();
        }

        #endregion

        #region OtherNodes

        private void _otherNodesUpdate()
        {
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

            _otherNodesUpdate();
        }

        #endregion

        #region Client

        private void _clientFiltersProxyUriTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_clientFiltersListView.SelectedIndex == -1)
                {
                    _clientFilterAddButton_Click(null, null);
                }
                else
                {
                    _clientFilterEditButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

        private void _clientFiltersConditionTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_clientFiltersListView.SelectedIndex == -1)
                {
                    _clientFilterAddButton_Click(null, null);
                }
                else
                {
                    _clientFilterEditButton_Click(null, null);
                }

                e.Handled = true;
            }
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
                _clientFiltersOptionTextBox.Text = "";

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

            if (item.Option != null)
            {
                _clientFiltersOptionTextBox.Text = item.Option;
            }
            else
            {
                _clientFiltersOptionTextBox.Text = "";
            }

            _clientFiltersConnectionTypeComboBox.SelectedItem = item.ConnectionType;
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

        private void _clientFiltersListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _clientFiltersListViewCopyMenuItem_Click(null, null);
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

                }
            }

            _clientFiltersListViewUpdate();
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

            _clientFilters.Move(selectIndex, selectIndex - 1);

            _clientFiltersListViewUpdate();
        }

        private void _clientFilterDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _clientFiltersListView.SelectedItem as ConnectionFilter;
            if (item == null) return;

            var selectIndex = _clientFiltersListView.SelectedIndex;
            if (selectIndex == -1) return;

            _clientFilters.Move(selectIndex, selectIndex + 1);

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
                string option = null;

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

                if (!string.IsNullOrWhiteSpace(_clientFiltersOptionTextBox.Text))
                {
                    option = _clientFiltersOptionTextBox.Text;
                }

                var connectionFilter = new ConnectionFilter()
                {
                    ConnectionType = connectionType,
                    ProxyUri = proxyUri,
                    UriCondition = uriCondition,
                    Option = option,
                };

                if (_clientFilters.Any(n => n == connectionFilter)) return;
                _clientFilters.Add(connectionFilter);
            }
            catch (Exception)
            {

            }

            _clientFiltersListViewUpdate();
        }

        private void _clientFilterEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clientFiltersConditionTextBox.Text == "") return;

            int selectIndex = _clientFiltersListView.SelectedIndex;
            if (selectIndex == -1) return;

            try
            {
                var connectionType = (ConnectionType)_clientFiltersConnectionTypeComboBox.SelectedItem;

                string proxyUri = null;
                var uriCondition = new UriCondition() { Value = _clientFiltersConditionTextBox.Text };
                string option = null;

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

                if (!string.IsNullOrWhiteSpace(_clientFiltersOptionTextBox.Text))
                {
                    option = _clientFiltersOptionTextBox.Text;
                }

                var connectionFilter = new ConnectionFilter()
                {
                    ConnectionType = connectionType,
                    ProxyUri = proxyUri,
                    UriCondition = uriCondition,
                    Option = option,
                };

                if (_clientFilters.Any(n => n == connectionFilter)) return;
                _clientFilters.Set(selectIndex, connectionFilter);

                _clientFiltersListView.SelectedIndex = selectIndex;
            }
            catch (Exception)
            {
                return;
            }

            _clientFiltersListViewUpdate();
        }

        private void _clientFilterDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _clientFiltersListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _clientFiltersListView.SelectedItems.OfType<ConnectionFilter>().ToArray())
            {
                _clientFilters.Remove(item);
            }

            _clientFiltersListViewUpdate();
        }

        #endregion

        #region Server

        private void _serverListenUriTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_serverListenUrisListView.SelectedIndex == -1)
                {
                    _serverListenUriAddButton_Click(null, null);
                }
                else
                {
                    _serverListenUriEditButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

        private void _serverListenUrisUpdate()
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

                    if (selectIndex == _serverListenUris.Count - 1)
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

        private void _serverListenUrisListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _serverListenUrisListViewCopyMenuItem_Click(null, null);
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

        private void _serverListenUrisListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"([\+-]) (.*)");

            foreach (var uri in Clipboard.GetText().Split('\r', '\n'))
            {
                if (!Regex.IsMatch(uri, @"^(.+?):(.+)$") || _serverListenUris.Any(n => n == uri)) continue;
                _serverListenUris.Add(uri);
            }

            _serverListenUrisUpdate();
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

            _serverListenUris.Move(selectIndex, selectIndex - 1);

            _serverListenUrisUpdate();
        }

        private void _serverListenUriDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _serverListenUrisListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _serverListenUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            _serverListenUris.Move(selectIndex, selectIndex + 1);

            _serverListenUrisUpdate();
        }

        private void _serverListenUriAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serverListenUriTextBox.Text == "") return;

            var uri = _serverListenUriTextBox.Text;

            if (!Regex.IsMatch(uri, @"^(.+?):(.+)$") || _serverListenUris.Any(n => n == uri)) return;
            _serverListenUris.Add(uri);

            _serverListenUrisUpdate();
        }

        private void _serverListenUriEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serverListenUriTextBox.Text == "") return;

            int selectIndex = _serverListenUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            var uri = _serverListenUriTextBox.Text;

            if (!Regex.IsMatch(uri, @"^(.+?):(.+)$") || _serverListenUris.Any(n => n == uri)) return;
            _serverListenUris.Set(selectIndex, uri);

            _serverListenUrisListView.SelectedIndex = selectIndex;

            _serverListenUrisUpdate();
        }

        private void _serverListenUriDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _serverListenUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _serverListenUrisListView.SelectedItems.OfType<string>().ToArray())
            {
                _serverListenUris.Remove(item);
            }

            _serverListenUrisUpdate();
        }

        #endregion

        #region Data

        private void _dataCacheSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_dataCacheSizeTextBox.Text)) return;

            StringBuilder builder = new StringBuilder("");

            foreach (var item in _dataCacheSizeTextBox.Text)
            {
                if (Regex.IsMatch(item.ToString(), @"[0-9\.]"))
                {
                    builder.Append(item.ToString());
                }
            }

            var value = builder.ToString();
            if (_dataCacheSizeTextBox.Text != value) _dataCacheSizeTextBox.Text = value;
        }

        private void _dataCacheSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1 || e.RemovedItems.Count != 1) return;

            var newItem = (string)e.AddedItems[0];
            var oldItem = (string)e.RemovedItems[0];

            try
            {
                var size = (long)NetworkConverter.FromSizeString(_dataCacheSizeTextBox.Text + oldItem);
                _dataCacheSizeTextBox.Text = NetworkConverter.ToSizeString(size, newItem);
            }
            catch (Exception)
            {
                var size = long.MaxValue;
                _dataCacheSizeTextBox.Text = NetworkConverter.ToSizeString(size, newItem);
            }
        }

        private void _dataDownloadDirectoryTextBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
                dialog.SelectedPath = _dataDownloadDirectoryTextBox.Text;
                dialog.ShowNewFolderButton = true;
                dialog.Description = LanguagesManager.Instance.CoreOptionsWindow_DownloadDirectory;

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

        private void _bandwidthLimitTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_bandwidthLimitTextBox.Text)) return;

            StringBuilder builder = new StringBuilder("");

            foreach (var item in _bandwidthLimitTextBox.Text)
            {
                if (Regex.IsMatch(item.ToString(), @"[0-9\.]"))
                {
                    builder.Append(item.ToString());
                }
            }

            var value = builder.ToString();
            if (_bandwidthLimitTextBox.Text != value) _bandwidthLimitTextBox.Text = value;
        }

        private void _bandwidthLimitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1 || e.RemovedItems.Count != 1) return;

            var newItem = (string)e.AddedItems[0];
            var oldItem = (string)e.RemovedItems[0];

            try
            {
                var size = (long)NetworkConverter.FromSizeString(_bandwidthLimitTextBox.Text + oldItem);
                _bandwidthLimitTextBox.Text = NetworkConverter.ToSizeString(size, newItem);
            }
            catch (Exception)
            {
                var size = long.MaxValue;
                _bandwidthLimitTextBox.Text = NetworkConverter.ToSizeString(size, newItem);
            }
        }

        #endregion

        #region Transfer

        private void _transferLimitTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _transferLimitSpanTextBox.IsEnabled = (TransferLimitType)_transferLimitTypeComboBox.SelectedItem != TransferLimitType.None;
            _transferLimitSizeTextBox.IsEnabled = (TransferLimitType)_transferLimitTypeComboBox.SelectedItem != TransferLimitType.None;
        }

        private void _transferLimitSpanTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_transferLimitSpanTextBox.Text)) return;

            StringBuilder builder = new StringBuilder("");

            foreach (var item in _transferLimitSpanTextBox.Text)
            {
                if (Regex.IsMatch(item.ToString(), "[0-9]"))
                {
                    builder.Append(item.ToString());
                }
            }

            var value = builder.ToString();
            if (_transferLimitSpanTextBox.Text != value) _transferLimitSpanTextBox.Text = value;
        }

        private void _resetButton_Click(object sender, RoutedEventArgs e)
        {
            _transferLimitManager.Reset();

            _transferInfoUploadedLabel.Content = NetworkConverter.ToSizeString(_transferLimitManager.TotalUploadSize);
            _transferInfoDownloadedLabel.Content = NetworkConverter.ToSizeString(_transferLimitManager.TotalDownloadSize);
            _transferInfoTotalLabel.Content = NetworkConverter.ToSizeString(_transferLimitManager.TotalUploadSize + _transferLimitManager.TotalDownloadSize);
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
                    size = (long)NetworkConverter.FromSizeString(_dataCacheSizeTextBox.Text + (string)_dataCacheSizeComboBox.SelectedItem);
                }
                catch (Exception)
                {
                    size = long.MaxValue;
                }

#if !DEBUG
                size = Math.Max((long)NetworkConverter.FromSizeString("50 GB"), size);
#endif

                if (_amoebaManager.Size != size)
                {
                    if (((long)_amoebaManager.Information["UsingSpace"]) > size)
                    {
                        if (MessageBox.Show(this, LanguagesManager.Instance.CoreOptionsWindow_CacheResize_Message, "Connections Settings", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
                        {
                            _amoebaManager.Resize(size);
                        }
                    }
                    else
                    {
                        _amoebaManager.Resize(size);
                    }
                }

                _amoebaManager.SetBaseNode(new Node(_baseNode_Id, _baseNode_Uris));
                _amoebaManager.SetOtherNodes(_otherNodes.Where(n => n != null && n.Id != null && n.Uris.Count() != 0));

                int count = CoreOptionsWindow.GetStringToInt(_bandwidthConnectionCountTextBox.Text);
                _amoebaManager.ConnectionCountLimit = Math.Max(Math.Min(count, 100), 12);

                int bandwidthLimit = (int)NetworkConverter.FromSizeString("0");

                try
                {
                    bandwidthLimit = (int)NetworkConverter.FromSizeString(_bandwidthLimitTextBox.Text + (string)_bandwidthLimitComboBox.SelectedItem);
                }
                catch (Exception)
                {
                    bandwidthLimit = int.MaxValue;
                }

                _amoebaManager.BandWidthLimit = bandwidthLimit;

                _amoebaManager.Filters.Clear();
                _amoebaManager.Filters.AddRange(_clientFilters.Select(n => n.DeepClone()));

                if (!Collection.Equals(_amoebaManager.ListenUris, _serverListenUris))
                {
                    _amoebaManager.ListenUris.Clear();
                    _amoebaManager.ListenUris.AddRange(_serverListenUris);

                    flag = true;
                }

                string path = _dataDownloadDirectoryTextBox.Text;

                foreach (var item in System.IO.Path.GetInvalidPathChars())
                {
                    path = path.Replace(item.ToString(), "-");
                }

                _amoebaManager.DownloadDirectory = path;
            }

            lock (_transferLimitManager.ThisLock)
            {
                lock (_transferLimitManager.TransferLimit.ThisLock)
                {
                    _transferLimitManager.TransferLimit.Type = (TransferLimitType)_transferLimitTypeComboBox.SelectedItem;

                    int day = CoreOptionsWindow.GetStringToInt(_transferLimitSpanTextBox.Text);
                    _transferLimitManager.TransferLimit.Span = Math.Max(Math.Min(day, 31), 1);

                    long size = (long)NetworkConverter.FromSizeString("32 GB");

                    try
                    {
                        size = Math.Abs((long)NetworkConverter.FromSizeString(_transferLimitSizeTextBox.Text));
                    }
                    catch (Exception)
                    {

                    }

                    _transferLimitManager.TransferLimit.Size = size;
                }
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
            Settings.Instance.CoreOptionsWindow_DataCacheSize_Unit = (string)_dataCacheSizeComboBox.SelectedItem;
            Settings.Instance.CoreOptionsWindow_BandwidthLimit_Unit = (string)_bandwidthLimitComboBox.SelectedItem;
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
