using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml;
using Amoeba;
using Amoeba.Properties;
using Library;
using Library.Collections;
using Library.Io;
using Library.Net.Amoeba;
using Library.Security;
using A = Library.Net.Amoeba;

namespace Amoeba.Windows
{
    /// <summary>
    /// Interaction logic for LinkControl.xaml
    /// </summary>
    partial class LinkControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;

        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollectionEx<string> _trustSignatureCollection = new ObservableCollectionEx<string>();
        private ObservableCollectionEx<string> _untrustSignatureCollection = new ObservableCollectionEx<string>();

        private static Random _random = new Random();

        private Thread _watchThread;

        public LinkControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            InitializeComponent();

            _trustSignatureListView.ItemsSource = _trustSignatureCollection;
            _untrustSignatureListView.ItemsSource = _untrustSignatureCollection;

            this.Sort();

            _watchThread = new Thread(this.WatchThread);
            _watchThread.Priority = ThreadPriority.Highest;
            _watchThread.IsBackground = true;
            _watchThread.Name = "LinkControl_WatchThread";
            _watchThread.Start();

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                if (e.OriginalSource != _mainWindow._tabControl) return;

                if (_mainWindow.SelectedTab == MainWindowTabType.Link)
                {
                    this.Update();
                }
            };

            _searchRowDefinition.Height = new GridLength(0);

            this.Update();
        }

        private void Sort()
        {
            _trustSignatureListView.Items.SortDescriptions.Clear();
            _trustSignatureListView.Items.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));
            _untrustSignatureListView.Items.SortDescriptions.Clear();
            _untrustSignatureListView.Items.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));
        }

        private void WatchThread()
        {
            try
            {
                var stopwatch = new Stopwatch();

                for (;;)
                {
                    if (!stopwatch.IsRunning || stopwatch.Elapsed.TotalSeconds >= 60)
                    {
                        stopwatch.Restart();

                        this.Refresh_LinkItems();
                    }

                    Thread.Sleep(1000);
                }
            }
            catch (Exception e)
            {

            }
        }

        private void Refresh_LinkItems()
        {
            var higherLinkItems = new HashSet<LinkItem>();
            var generalLinkItems = new HashSet<LinkItem>();

            foreach (var leaderSignature in Settings.Instance.Global_TrustSignatures.ToArray())
            {
                var targetLinkItems = new List<LinkItem>();

                var targetSignatures = new HashSet<string>();
                var checkedSignatures = new HashSet<string>();

                targetSignatures.Add(leaderSignature);

                for (int i = 0; i < 32; i++)
                {
                    var linkItems = this.GetLinkItems(targetSignatures).ToList();
                    if (linkItems.Count == 0) break;

                    checkedSignatures.UnionWith(targetSignatures);
                    checkedSignatures.UnionWith(linkItems.SelectMany(n => n.DeleteSignatures));

                    targetSignatures.Clear();
                    targetSignatures.UnionWith(linkItems.SelectMany(n => n.TrustSignatures).Where(n => !checkedSignatures.Contains(n)));

                    targetLinkItems.AddRange(linkItems);

                    if (targetLinkItems.Count > 32 * 1024) goto End;
                }

                End:;

                higherLinkItems.UnionWith(targetLinkItems.Take(32));
                generalLinkItems.UnionWith(targetLinkItems.Take(32 * 1024));
            }

            lock (Settings.Instance.ThisLock)
            {
                lock (Settings.Instance.Cache_LinkItems.ThisLock)
                {
                    Settings.Instance.Cache_LinkItems.Clear();

                    foreach (var linkItem in generalLinkItems)
                    {
                        Settings.Instance.Cache_LinkItems.Add(linkItem.Signature, linkItem);
                    }
                }
            }
        }

        private IEnumerable<LinkItem> GetLinkItems(IEnumerable<string> trustSignatures)
        {
            var linkItems = new List<LinkItem>();

            foreach (var trustSignature in trustSignatures)
            {
                LinkItem linkItem = null;

                {
                    var link = _amoebaManager.GetLink(trustSignature);

                    if (link != null)
                    {
                        linkItem = new LinkItem();
                        linkItem.Signature = trustSignature;
                        linkItem.TrustSignatures.AddRange(link.TrustSignatures);
                        linkItem.DeleteSignatures.AddRange(link.DeleteSignatures);
                    }
                }

                if (linkItem == null)
                {
                    Settings.Instance.Cache_LinkItems.TryGetValue(trustSignature, out linkItem);
                }

                if (linkItem == null)
                {
                    linkItem = new LinkItem();
                    linkItem.Signature = trustSignature;
                }

                linkItems.Add(linkItem);
            }

            return linkItems;
        }

        private static string GetNormalizedPath(string path)
        {
            string filePath = path;

            foreach (char ic in System.IO.Path.GetInvalidFileNameChars())
            {
                filePath = filePath.Replace(ic.ToString(), "-");
            }
            foreach (char ic in System.IO.Path.GetInvalidPathChars())
            {
                filePath = filePath.Replace(ic.ToString(), "-");
            }

            return filePath;
        }

        public void Update()
        {
            _treeView.Items.Clear();

            _trustSignatureCollection.Clear();
            _untrustSignatureCollection.Clear();

            string[] words = null;

            {
                string searchText = _searchTextBox.Text;

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            foreach (var leaderSignature in Settings.Instance.Global_TrustSignatures.ToArray())
            {
                var item = this.GetSignatureTreeViewItem(leaderSignature);

                if (words != null)
                {
                    foreach (var word in words)
                    {
                        if (item == null) break;
                        item = item.Search(n => n.LinkItem.Signature.Contains(word, StringComparison.CurrentCultureIgnoreCase));
                    }
                }

                if (item == null) continue;

                _treeView.Items.Add(new SignatureTreeViewModel(null, item));
            }

            foreach (var path in Settings.Instance.LinkControl_ExpandedPaths.ToArray())
            {
                if (path.Count == 0) goto End;

                var treeViewModel = _treeView.Items.OfType<SignatureTreeViewModel>()
                    .FirstOrDefault(n => n.Value.LinkItem.Signature == path[0]);
                if (treeViewModel == null) goto End;

                foreach (var name in path.Skip(1))
                {
                    treeViewModel = treeViewModel.Children.OfType<SignatureTreeViewModel>()
                        .FirstOrDefault(n => n.Value.LinkItem.Signature == name);
                    if (treeViewModel == null) goto End;
                }

                treeViewModel.IsExpanded = true;
                continue;

                End:;

                if (words == null)
                {
                    Settings.Instance.LinkControl_ExpandedPaths.Remove(path);
                }
            }
        }

        private void Update_Title()
        {
            if (_mainWindow.SelectedTab == MainWindowTabType.Link)
            {
                if (_treeView.SelectedItem is SignatureTreeViewModel)
                {
                    var selectTreeViewItem = (SignatureTreeViewModel)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Amoeba {0} - {1}", _serviceManager.AmoebaVersion, selectTreeViewItem.Value.LinkItem.Signature);
                }
            }
        }

        private SignatureTreeItem GetSignatureTreeViewItem(string leaderSignature)
        {
            var signatureTreeItems = new List<SignatureTreeItem>();
            var workSignatureTreeItems = new List<SignatureTreeItem>();

            var checkedSignatures = new HashSet<string>();
            var workCheckedSignatures = new HashSet<string>();

            {
                LinkItem leaderLinkItem;
                if (!Settings.Instance.Cache_LinkItems.TryGetValue(leaderSignature, out leaderLinkItem)) return null;

                signatureTreeItems.Add(new SignatureTreeItem(leaderLinkItem));
                checkedSignatures.Add(leaderSignature);
            }

            {
                int index = 0;

                for (;;)
                {
                    for (; index < signatureTreeItems.Count && index < 32 * 1024; index++)
                    {
                        var sortedList = signatureTreeItems[index].LinkItem.TrustSignatures.ToList();
                        sortedList.Sort();

                        foreach (var trustSignature in sortedList)
                        {
                            if (checkedSignatures.Contains(trustSignature)) continue;

                            LinkItem tempLinkItem;
                            if (!Settings.Instance.Cache_LinkItems.TryGetValue(trustSignature, out tempLinkItem)) continue;

                            var tempTreeItem = new SignatureTreeItem(tempLinkItem);
                            signatureTreeItems[index].Children.Add(tempTreeItem);

                            workSignatureTreeItems.Add(tempTreeItem);
                            workCheckedSignatures.Add(trustSignature);
                        }
                    }

                    if (workSignatureTreeItems.Count == 0) break;

                    signatureTreeItems.AddRange(workSignatureTreeItems);
                    workSignatureTreeItems.Clear();

                    checkedSignatures.UnionWith(workCheckedSignatures);
                    workCheckedSignatures.Clear();
                }
            }

            return signatureTreeItems[0];
        }

        #region _treeView

        private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private void TreeViewItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = ((UIElement)e.OriginalSource).FindAncestor<TreeViewItem>();
            if (element == null) return;

            var item = _treeView.SearchItemFromElement(element) as TreeViewModelBase;
            if (item == null) return;

            item.IsSelected = true;

            e.Handled = true;
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SignatureTreeViewModel;
            if (selectTreeViewItem == null) return;

            _trustSignatureCollection.Clear();
            _trustSignatureCollection.AddRange(selectTreeViewItem.Value.LinkItem.TrustSignatures);

            _untrustSignatureCollection.Clear();
            _untrustSignatureCollection.AddRange(selectTreeViewItem.Value.LinkItem.DeleteSignatures);

            this.Update_Title();
        }

        private void _treeView_Expanded(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            var treeViewModel = (TreeViewModelBase)_treeView.SearchItemFromElement((DependencyObject)treeViewItem);

            var path = new Route();

            foreach (var item in treeViewModel.GetAncestors())
            {
                if (item is SignatureTreeViewModel) path.Add(((SignatureTreeViewModel)item).Value.LinkItem.Signature);
            }

            Settings.Instance.LinkControl_ExpandedPaths.Add(path);
        }

        private void _treeView_Collapsed(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem == null) return;

            var treeViewModel = (TreeViewModelBase)_treeView.SearchItemFromElement((DependencyObject)treeViewItem);

            var path = new Route();

            foreach (var item in treeViewModel.GetAncestors())
            {
                if (item is SignatureTreeViewModel) path.Add(((SignatureTreeViewModel)item).Value.LinkItem.Signature);
            }

            Settings.Instance.LinkControl_ExpandedPaths.Remove(path);
        }

        private void _treeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _treeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var signatureTreeViewItem = _treeView.SelectedItem as SignatureTreeViewModel;
            if (signatureTreeViewItem == null) return;

            Clipboard.SetText(signatureTreeViewItem.Value.LinkItem.Signature);
        }

        #endregion

        #region _trustSignature

        private void _trustSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _trustSignatureListView.SelectedItems;

            _trustSignatureListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
        }

        private void _trustSignatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _trustSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        #region _untrustSignature

        private void _untrustSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _untrustSignatureListView.SelectedItems;

            _untrustSignatureListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
        }

        private void _untrustSignatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _untrustSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        private void _searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.Update();
            }
        }

        private void Execute_Copy(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            _treeViewItemCopyMenuItem_Click(sender, e);
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

            this.Update();
        }
    }
}
