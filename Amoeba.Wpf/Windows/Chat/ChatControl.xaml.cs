using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
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
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Library;
using Library.Collections;
using Library.Net.Amoeba;
using Library.Security;
using Amoeba;
using Amoeba.Properties;
using System.Security.Cryptography;

namespace Amoeba.Windows
{
    /// <summary>
    /// Interaction logic for ChatControl.xaml
    /// </summary>
    partial class ChatControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;

        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private static Random _random = new Random();

        private Thread _searchThread;
        private Thread _cacheThread;

        private AutoResetEvent _updateEvent = new AutoResetEvent(false);
        private volatile bool _refreshing = false;
        private AutoResetEvent _cacheUpdateEvent = new AutoResetEvent(false);
        private volatile bool _autoUpdate;

        private ChatCategorizeTreeViewModel _treeViewModel;

        private AvalonEditHelper_MulticastMessage _textEditor_Helper = new AvalonEditHelper_MulticastMessage();
        private List<MulticastMessageViewModel> _textEditer_Collection = new List<MulticastMessageViewModel>();

        public ChatControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            _treeViewModel = new ChatCategorizeTreeViewModel(null, Settings.Instance.ChatControl_ChatCategorizeTreeItem);

            InitializeComponent();

            // NewMessage
            {
                var bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.StreamSource = new FileStream(Path.Combine(_serviceManager.Paths["Icons"], @"Tools/NewMessage.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                bitmap.EndInit();
                if (bitmap.CanFreeze) bitmap.Freeze();

                _newMessageButton.Content = new Image() { Source = bitmap, Height = 32, Width = 32 };
            }

            // Trust
            {
                var bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.StreamSource = new FileStream(Path.Combine(_serviceManager.Paths["Icons"], @"Tools/Trust.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                bitmap.EndInit();
                if (bitmap.CanFreeze) bitmap.Freeze();

                _trustToggleButton.Content = new Image() { Source = bitmap, Height = 32, Width = 32 };
            }

            _treeView.Items.Add(_treeViewModel);

            try
            {
                _treeViewModel.IsSelected = true;
            }
            catch (Exception)
            {

            }

            _searchThread = new Thread(new ThreadStart(this.Search));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "ChatControl_SearchThread";
            _searchThread.Start();

            _cacheThread = new Thread(new ThreadStart(this.Cache));
            _cacheThread.Priority = ThreadPriority.Highest;
            _cacheThread.IsBackground = true;
            _cacheThread.Name = "ChatControl_CacheThread";
            _cacheThread.Start();

            _newMessageButton.IsEnabled = false;

            _textEditor_Helper.Setup(_textEditor);
            _textEditor_Helper.ClickEvent += _textEditor_Helper_ClickEvent;

            LanguagesManager.UsingLanguageChangedEvent += this.LanguagesManager_UsingLanguageChangedEvent;
        }

        void _textEditor_Helper_ClickEvent(string uri)
        {
            if (uri.StartsWith("http:") | uri.StartsWith("https:"))
            {
                try
                {
                    Process.Start(uri);
                }
                catch (Exception)
                {
                    return;
                }

                Settings.Instance.Global_UrlHistorys.Add(uri);
            }
            else if (uri.StartsWith("Tag:"))
            {
                var tag = AmoebaConverter.FromTagString(uri);
                if (tag.Id == null || tag.Name == null) return;

                {
                    var chatCategorizeTreeItems = new List<ChatCategorizeTreeItem>();
                    chatCategorizeTreeItems.Add(_treeViewModel.Value);

                    for (int i = 0; i < chatCategorizeTreeItems.Count; i++)
                    {
                        chatCategorizeTreeItems.AddRange(chatCategorizeTreeItems[i].Children);
                        if (chatCategorizeTreeItems[i].ChatTreeItems.Any(n => n.Tag == tag)) return;
                    }
                }

                var selectTreeViewModel = _treeView.SelectedItem as ChatTreeViewModel;
                if (selectTreeViewModel == null) return;

                var parentTreeViewModel = selectTreeViewModel.Parent as ChatCategorizeTreeViewModel;
                if (parentTreeViewModel == null) return;

                var chatTreeItem = new ChatTreeItem(tag);
                parentTreeViewModel.Value.ChatTreeItems.Add(chatTreeItem);

                parentTreeViewModel.Update();
            }
            else if (uri.StartsWith("Seed:"))
            {
                var seed = AmoebaConverter.FromSeedString(uri);

                _amoebaManager.Download(seed, 3);
                Settings.Instance.Global_SeedHistorys.Add(seed);
            }
        }

        private void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {
            this.Update();
        }

        private void Search()
        {
            try
            {
                for (;;)
                {
                    _updateEvent.WaitOne();

                    try
                    {
                        _refreshing = true;

                        TreeViewModelBase tempTreeViewModel = null;

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            tempTreeViewModel = (TreeViewModelBase)_treeView.SelectedItem;
                        }));

                        if (tempTreeViewModel is ChatCategorizeTreeViewModel)
                        {
                            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                if (tempTreeViewModel != _treeView.SelectedItem) return;

                                _newMessageButton.IsEnabled = false;
                                _trustToggleButton.IsEnabled = false;
                                _trustToggleButton.IsChecked = false;

                                _textEditor_Helper.Clear(_textEditor);
                            }));
                        }
                        else if (tempTreeViewModel is ChatTreeViewModel)
                        {
                            var chatTreeViewModel = (ChatTreeViewModel)tempTreeViewModel;

                            var newList = new HashSet<MulticastMessageViewModel>();

                            lock (chatTreeViewModel.Value.ThisLock)
                            {
                                newList.UnionWith(chatTreeViewModel.Value.MulticastMessages
                                    .Select(n => new MulticastMessageViewModel(n.Key, n.Value)));

                                foreach (var pair in chatTreeViewModel.Value.MulticastMessages.ToArray())
                                {
                                    chatTreeViewModel.Value.MulticastMessages[pair.Key] = (pair.Value & ~MulticastMessageState.IsUnread);
                                }
                            }

                            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                if (chatTreeViewModel != _treeView.SelectedItem) return;

                                {
                                    _newMessageButton.IsEnabled = (Settings.Instance.Global_DigitalSignatures.Count != 0);

                                    _trustToggleButton.IsEnabled = true;
                                    _trustToggleButton.IsChecked = chatTreeViewModel.Value.IsTrustEnabled;
                                }

                                {
                                    var sortedList = newList.ToList();
                                    sortedList.Sort((x, y) => x.MulticastMessageItem.CreationTime.CompareTo(y.MulticastMessageItem.CreationTime));

                                    _textEditer_Collection.Clear();
                                    _textEditer_Collection.AddRange(sortedList);

                                    _textEditor_Helper.Set(_textEditor, _textEditer_Collection);
                                }

                                this.Update_TreeView_Color();
                            }));
                        }
                    }
                    finally
                    {
                        _refreshing = false;
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void Cache()
        {
            try
            {
                for (;;)
                {
                    var chatTreeViewModels = new List<ChatTreeViewModel>();

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        var categorizeChatTreeViewModels = new List<ChatCategorizeTreeViewModel>();
                        categorizeChatTreeViewModels.Add(_treeViewModel);

                        for (int i = 0; i < categorizeChatTreeViewModels.Count; i++)
                        {
                            categorizeChatTreeViewModels.AddRange(categorizeChatTreeViewModels[i].Children.OfType<ChatCategorizeTreeViewModel>());
                            chatTreeViewModels.AddRange(categorizeChatTreeViewModels[i].Children.OfType<ChatTreeViewModel>());
                        }
                    }));

                    foreach (var treeViewModel in chatTreeViewModels)
                    {
                        var limit = Settings.Instance.Global_Limit;

                        // MulticastMessage
                        lock (treeViewModel.Value.ThisLock)
                        {
                            var results = this.GetMulticastMessages(
                                treeViewModel.Value.Tag,
                                treeViewModel.Value.MulticastMessages.ToArray(),
                                treeViewModel.Value.IsTrustEnabled,
                                limit);

                            treeViewModel.Value.MulticastMessages.Clear();

                            foreach (var item in results)
                            {
                                treeViewModel.Value.MulticastMessages.Add(item.Key, item.Value);
                            }
                        }

                        this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            treeViewModel.Update();
                        }));
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        if (_autoUpdate)
                        {
                            _autoUpdate = false;
                            this.Update();
                        }
                        else
                        {
                            this.Update_TreeView_Color();
                        }
                    }));

                    _cacheUpdateEvent.WaitOne(1000 * 30);
                }
            }
            catch (Exception)
            {

            }
        }

        private IEnumerable<KeyValuePair<MulticastMessageItem, MulticastMessageState>> GetMulticastMessages(
            Tag tag,
            IEnumerable<KeyValuePair<MulticastMessageItem, MulticastMessageState>> collections,
            bool trust,
            int limit)
        {
            var dic = new Dictionary<MulticastMessageItem, MulticastMessageState>();

            foreach (var pair in collections)
            {
                dic.Add(pair.Key, pair.Value);
            }

            {
                foreach (var info in _amoebaManager.GetMulticastMessages(tag, limit))
                {
                    var item = new MulticastMessageItem();
                    item.Tag = (Tag)info["Tag"];
                    item.CreationTime = (DateTime)info["CreationTime"];
                    item.Signature = (string)info["Signature"];
                    {
                        var message = (Message)info["Value"];
                        item.Comment = message.Comment;
                    }

                    if (trust && !Inspect.ContainTrustSignature(item.Signature)) continue;
                    if (dic.ContainsKey(item)) continue;

                    dic.Add(item, MulticastMessageState.IsUnread);
                }
            }

            {
                var sortedList = dic.Where(n => !n.Value.HasFlag(MulticastMessageState.IsLocked)).Select(n => n.Key).ToList();
                sortedList.Sort((x, y) => x.CreationTime.CompareTo(y.CreationTime));

                foreach (var item in sortedList.Take(sortedList.Count - 1024))
                {
                    dic.Remove(item);
                }
            }

            {
                var ddd = new Dictionary<string, List<MulticastMessageItem>>();
                foreach (var p in dic.Keys)
                {
                    List<MulticastMessageItem> list;
                    if (!ddd.TryGetValue(p.Signature, out list))
                    {
                        list = new List<MulticastMessageItem>();
                        ddd[p.Signature] = list;
                    }

                    list.Add(p);
                    list.Sort((x, y) => x.CreationTime.CompareTo(y.CreationTime));
                }
            }

            return dic;
        }

        private void Update()
        {
            this.Update_TreeView_Color();
            _updateEvent.Set();
        }

        private void Update_Cache()
        {
            _autoUpdate = true;
            _cacheUpdateEvent.Set();
        }

        private void Update_TreeView_Color()
        {
            var selectTreeViewModel = _treeView.SelectedItem as TreeViewModelBase;

            {
                var items = new List<TreeViewModelBase>();
                items.Add(_treeViewModel);

                for (int i = 0; i < items.Count; i++)
                {
                    foreach (var item in items[i].Children)
                    {
                        items.Add(item);
                    }
                }

                var hitItems = new HashSet<TreeViewModelBase>();

                foreach (var item in items.OfType<ChatTreeViewModel>()
                    .Where(n => n.Value.MulticastMessages.Any(m => m.Value.HasFlag(MulticastMessageState.IsUnread))))
                {
                    hitItems.UnionWith(item.GetAncestors());
                }

                foreach (var item in items)
                {
                    if (item is ChatCategorizeTreeViewModel)
                    {
                        ((ChatCategorizeTreeViewModel)item).IsHit = hitItems.Contains(item);
                    }
                    else if (item is ChatTreeViewModel)
                    {
                        ((ChatTreeViewModel)item).IsHit = hitItems.Contains(item);
                    }
                }
            }
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

        private Point _startPoint = new Point(-1, -1);

        private void _treeView_PreviewDragOver(object sender, DragEventArgs e)
        {
            Point position = MouseUtils.GetMousePosition(_treeView);

            if (position.Y < 50)
            {
                var peer = ItemsControlAutomationPeer.CreatePeerForElement(_treeView);
                var scrollProvider = peer.GetPattern(PatternInterface.Scroll) as IScrollProvider;

                try
                {
                    scrollProvider.Scroll(System.Windows.Automation.ScrollAmount.NoAmount, System.Windows.Automation.ScrollAmount.SmallDecrement);
                }
                catch (Exception)
                {

                }
            }
            else if ((_treeView.ActualHeight - position.Y) < 50)
            {
                var peer = ItemsControlAutomationPeer.CreatePeerForElement(_treeView);
                var scrollProvider = peer.GetPattern(PatternInterface.Scroll) as IScrollProvider;

                try
                {
                    scrollProvider.Scroll(System.Windows.Automation.ScrollAmount.NoAmount, System.Windows.Automation.ScrollAmount.SmallIncrement);
                }
                catch (Exception)
                {

                }
            }
        }

        private void _treeView_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && e.RightButton == System.Windows.Input.MouseButtonState.Released)
            {
                if (_startPoint.X == -1 && _startPoint.Y == -1) return;

                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (_treeViewModel == _treeView.SelectedItem) return;

                    var data = new DataObject("TreeViewItem", _treeView.SelectedItem);
                    DragDrop.DoDragDrop(_treeView, data, DragDropEffects.Move);
                }
            }
        }

        private void _treeView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("TreeViewItem"))
            {
                var sourceItem = (TreeViewModelBase)e.Data.GetData("TreeViewItem");

                if (sourceItem is ChatCategorizeTreeViewModel)
                {
                    var destinationItem = this.GetDropDestination((UIElement)e.OriginalSource);

                    if (destinationItem is ChatCategorizeTreeViewModel)
                    {
                        var s = (ChatCategorizeTreeViewModel)sourceItem;
                        var d = (ChatCategorizeTreeViewModel)destinationItem;

                        if (d.Value.Children.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (d.GetAncestors().Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = s.Parent;

                        if (parentItem is ChatCategorizeTreeViewModel)
                        {
                            var p = (ChatCategorizeTreeViewModel)parentItem;

                            var tItems = p.Value.Children.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            p.Value.Children.Clear();
                            p.Value.Children.AddRange(tItems);

                            p.Update();
                        }

                        d.IsSelected = true;
                        d.Value.Children.Add(s.Value);
                        d.Update();
                    }
                }
                else if (sourceItem is ChatTreeViewModel)
                {
                    var destinationItem = this.GetDropDestination((UIElement)e.OriginalSource);

                    if (destinationItem is ChatCategorizeTreeViewModel)
                    {
                        var s = (ChatTreeViewModel)sourceItem;
                        var d = (ChatCategorizeTreeViewModel)destinationItem;

                        if (d.Value.ChatTreeItems.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (d.GetAncestors().Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = s.Parent;

                        if (parentItem is ChatCategorizeTreeViewModel)
                        {
                            var p = (ChatCategorizeTreeViewModel)parentItem;

                            var tItems = p.Value.ChatTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            p.Value.ChatTreeItems.Clear();
                            p.Value.ChatTreeItems.AddRange(tItems);

                            p.Update();
                        }

                        d.IsSelected = true;
                        d.Value.ChatTreeItems.Add(s.Value);
                        d.Update();
                    }
                }

                this.Update_Cache();
            }
        }

        private TreeViewModelBase GetDropDestination(UIElement originalSource)
        {
            var element = originalSource.FindAncestor<TreeViewItem>();
            if (element == null) return null;

            return (TreeViewModelBase)_treeView.SearchItemFromElement(element) as TreeViewModelBase;
        }

        private void _treeView_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var element = ((UIElement)e.OriginalSource).FindAncestor<TreeViewItem>();
            if (element == null) return;

            var item = _treeView.SearchItemFromElement(element) as TreeViewModelBase;
            if (item == null)
            {
                _startPoint = new Point(-1, -1);

                return;
            }

            if (item.IsSelected == true)
            {
                _startPoint = e.GetPosition(null);
                _treeView_SelectedItemChanged(null, null);
            }
            else
            {
                _startPoint = new Point(-1, -1);
            }
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.Update();
        }

        private void _chatCategorizeTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var treeViewItem = sender as TreeViewItem;
            if (treeViewItem == null) return;

            var treeViewModel = (TreeViewModelBase)_treeView.SearchItemFromElement((DependencyObject)treeViewItem);
            if (_treeView.SelectedItem != treeViewModel) return;

            var contextMenu = treeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;
            _startPoint = new Point(-1, -1);

            MenuItem chatCategorizeTreeViewItemDeleteMenuItem = contextMenu.GetItem<MenuItem>("_chatCategorizeTreeViewItemDeleteMenuItem");
            MenuItem chatCategorizeTreeViewItemCutMenuItem = contextMenu.GetItem<MenuItem>("_chatCategorizeTreeViewItemCutMenuItem");
            MenuItem chatCategorizeTreeViewItemPasteMenuItem = contextMenu.GetItem<MenuItem>("_chatCategorizeTreeViewItemPasteMenuItem");

            chatCategorizeTreeViewItemDeleteMenuItem.IsEnabled = (_treeViewModel != treeViewModel);
            chatCategorizeTreeViewItemCutMenuItem.IsEnabled = (_treeViewModel != treeViewModel);
            chatCategorizeTreeViewItemPasteMenuItem.IsEnabled = Clipboard.ContainsChatCategorizeTreeItems() || Clipboard.ContainsChatTreeItems() || Clipboard.ContainsTags();
        }

        private void _chatCategorizeTreeViewItemNewTagMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            var window = new NameWindow();
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                byte[] id = new byte[32];
                {
                    using (var random = RandomNumberGenerator.Create())
                    {
                        random.GetBytes(id);
                    }
                }

                var tag = new Tag(window.Text, id);

                selectTreeViewModel.Value.ChatTreeItems.Add(new ChatTreeItem(tag));

                selectTreeViewModel.Update();
            }

            this.Update();
        }

        private void _chatCategorizeTreeViewItemNewCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            var window = new NameWindow();
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewModel.Value.Children.Add(new ChatCategorizeTreeItem() { Name = window.Text });

                selectTreeViewModel.Update();
            }

            this.Update();
        }

        private void _chatCategorizeTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            var window = new NameWindow(selectTreeViewModel.Value.Name);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewModel.Value.Name = window.Text;

                selectTreeViewModel.Update();
            }

            this.Update();
        }

        private void _chatCategorizeTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatCategorizeTreeViewModel;
            if (selectTreeViewModel == null || selectTreeViewModel == _treeViewModel) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Tag", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parent = (ChatCategorizeTreeViewModel)selectTreeViewModel.Parent;

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewModel.Value);
            parent.Update();

            this.Update();
        }

        private void _chatCategorizeTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatCategorizeTreeViewModel;
            if (selectTreeViewModel == null || selectTreeViewModel == _treeViewModel) return;

            Clipboard.SetChatCategorizeTreeItems(new ChatCategorizeTreeItem[] { selectTreeViewModel.Value });

            var parent = (ChatCategorizeTreeViewModel)selectTreeViewModel.Parent;

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewModel.Value);
            parent.Update();

            this.Update();
        }

        private void _chatCategorizeTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            Clipboard.SetChatCategorizeTreeItems(new ChatCategorizeTreeItem[] { selectTreeViewModel.Value });
        }

        private void _chatCategorizeTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            var chatTreeViewModels = new List<ChatTreeViewModel>();

            {
                var categorizeChatTreeViewModels = new List<ChatCategorizeTreeViewModel>();
                categorizeChatTreeViewModels.Add(selectTreeViewModel);

                for (int i = 0; i < categorizeChatTreeViewModels.Count; i++)
                {
                    categorizeChatTreeViewModels.AddRange(categorizeChatTreeViewModels[i].Children.OfType<ChatCategorizeTreeViewModel>());
                    chatTreeViewModels.AddRange(categorizeChatTreeViewModels[i].Children.OfType<ChatTreeViewModel>());
                }
            }

            var sb = new StringBuilder();

            foreach (var item in chatTreeViewModels)
            {
                sb.AppendLine(AmoebaConverter.ToTagString(item.Value.Tag));
                sb.AppendLine(MessageConverter.ToInfoMessage(item.Value.Tag));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _chatCategorizeTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            foreach (var item in Clipboard.GetChatCategorizeTreeItems())
            {
                selectTreeViewModel.Value.Children.Add(item);
            }

            foreach (var item in Clipboard.GetChatTreeItems())
            {
                if (selectTreeViewModel.Value.ChatTreeItems.Any(n => n.Tag == item.Tag)) continue;

                selectTreeViewModel.Value.ChatTreeItems.Add(item);
            }

            foreach (var tag in Clipboard.GetTags())
            {
                if (selectTreeViewModel.Value.ChatTreeItems.Any(n => n.Tag == tag)) continue;

                var chatTreeItem = new ChatTreeItem(tag);

                selectTreeViewModel.Value.ChatTreeItems.Add(chatTreeItem);
            }

            selectTreeViewModel.Update();

            this.Update_Cache();
        }

        private void _chatCategorizeTreeViewItemTrustOnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            var chatTreeViewModels = new List<ChatTreeViewModel>();

            var categorizeChatTreeViewModels = new List<ChatCategorizeTreeViewModel>();
            categorizeChatTreeViewModels.Add(selectTreeViewModel);

            for (int i = 0; i < categorizeChatTreeViewModels.Count; i++)
            {
                categorizeChatTreeViewModels.AddRange(categorizeChatTreeViewModels[i].Children.OfType<ChatCategorizeTreeViewModel>());
                chatTreeViewModels.AddRange(categorizeChatTreeViewModels[i].Children.OfType<ChatTreeViewModel>());
            }

            foreach (var item in chatTreeViewModels)
            {
                item.Value.IsTrustEnabled = true;
                item.Update();
            }

            this.Update_Cache();
        }

        private void _chatCategorizeTreeViewItemTrustOffMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            var chatTreeViewModels = new List<ChatTreeViewModel>();

            var categorizeChatTreeViewModels = new List<ChatCategorizeTreeViewModel>();
            categorizeChatTreeViewModels.Add(selectTreeViewModel);

            for (int i = 0; i < categorizeChatTreeViewModels.Count; i++)
            {
                categorizeChatTreeViewModels.AddRange(categorizeChatTreeViewModels[i].Children.OfType<ChatCategorizeTreeViewModel>());
                chatTreeViewModels.AddRange(categorizeChatTreeViewModels[i].Children.OfType<ChatTreeViewModel>());
            }

            foreach (var item in chatTreeViewModels)
            {
                item.Value.IsTrustEnabled = false;
                item.Update();
            }

            this.Update_Cache();
        }

        private void _chatCategorizeTreeViewItemMarkAllMessagesReadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatCategorizeTreeViewModel;
            if (selectTreeViewModel == null) return;

            var chatTreeViewModels = new List<ChatTreeViewModel>();

            var categorizeChatTreeViewModels = new List<ChatCategorizeTreeViewModel>();
            categorizeChatTreeViewModels.Add(selectTreeViewModel);

            for (int i = 0; i < categorizeChatTreeViewModels.Count; i++)
            {
                categorizeChatTreeViewModels.AddRange(categorizeChatTreeViewModels[i].Children.OfType<ChatCategorizeTreeViewModel>());
                chatTreeViewModels.AddRange(categorizeChatTreeViewModels[i].Children.OfType<ChatTreeViewModel>());
            }

            foreach (var item in chatTreeViewModels)
            {
                lock (item.Value.ThisLock)
                {
                    foreach (var pair in item.Value.MulticastMessages.ToArray())
                    {
                        item.Value.MulticastMessages[pair.Key] = pair.Value & ~MulticastMessageState.IsUnread;
                    }
                }
            }

            this.Update_Cache();
        }

        private void _chatTreeItemTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _chatTreeItemTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatTreeViewModel;
            if (selectTreeViewModel == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Tag", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var searchTreeViewModel = selectTreeViewModel.GetAncestors().OfType<ChatCategorizeTreeViewModel>().LastOrDefault() as ChatCategorizeTreeViewModel;
            if (searchTreeViewModel == null) return;

            searchTreeViewModel.IsSelected = true;

            searchTreeViewModel.Value.ChatTreeItems.Remove(selectTreeViewModel.Value);
            searchTreeViewModel.Update();

            this.Update();
        }

        private void _chatTreeItemTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatTreeViewModel;
            if (selectTreeViewModel == null) return;

            Clipboard.SetChatTreeItems(new ChatTreeItem[] { selectTreeViewModel.Value });

            var searchTreeViewModel = selectTreeViewModel.GetAncestors().OfType<ChatCategorizeTreeViewModel>().LastOrDefault() as ChatCategorizeTreeViewModel;
            if (searchTreeViewModel == null) return;

            searchTreeViewModel.IsSelected = true;

            searchTreeViewModel.Value.ChatTreeItems.Remove(selectTreeViewModel.Value);
            searchTreeViewModel.Update();

            this.Update();
        }

        private void _chatTreeItemTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatTreeViewModel;
            if (selectTreeViewModel == null) return;

            Clipboard.SetChatTreeItems(new ChatTreeItem[] { selectTreeViewModel.Value });
        }

        private void _chatTreeItemTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatTreeViewModel;
            if (selectTreeViewModel == null) return;

            var sb = new StringBuilder();

            sb.AppendLine(AmoebaConverter.ToTagString(selectTreeViewModel.Value.Tag));
            sb.AppendLine(MessageConverter.ToInfoMessage(selectTreeViewModel.Value.Tag));

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        #region Tools

        private void _newMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatTreeViewModel;
            if (selectTreeViewModel == null) return;

            var window = new MulticastMessageEditWindow(
                selectTreeViewModel.Value.Tag,
                "",
                _amoebaManager);

            window.Owner = _mainWindow;
            window.Show();
        }

        private void _trustToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatTreeViewModel;
            if (selectTreeViewModel == null) return;

            selectTreeViewModel.Value.IsTrustEnabled = _trustToggleButton.IsChecked.Value;

            if (selectTreeViewModel.Value.IsTrustEnabled)
            {
                foreach (var pair in selectTreeViewModel.Value.MulticastMessages.ToArray())
                {
                    if (Inspect.ContainTrustSignature(pair.Key.Signature)) continue;
                    selectTreeViewModel.Value.MulticastMessages.Remove(pair.Key);
                }
            }

            selectTreeViewModel.Update();

            this.Update_Cache();
        }

        #endregion

        #region _textEditor

        private void _textEditorCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_textEditor.SelectedText);
        }

        private void _textEditorLockThisMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatTreeViewModel;
            if (selectTreeViewModel == null) return;

            var selectedItems = new List<MulticastMessageItem>();
            {
                foreach (var index in _textEditor_Helper.SelectIndexes(_textEditor))
                {
                    selectedItems.Add(_textEditer_Collection[index].MulticastMessageItem);
                }
            }

            foreach (var item in selectedItems)
            {
                selectTreeViewModel.Value.MulticastMessages[item] |= MulticastMessageState.IsLocked;
            }
        }

        private void _textEditorUnlockThisMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatTreeViewModel;
            if (selectTreeViewModel == null) return;

            var selectedItems = new List<MulticastMessageItem>();
            {
                foreach (var index in _textEditor_Helper.SelectIndexes(_textEditor))
                {
                    selectedItems.Add(_textEditer_Collection[index].MulticastMessageItem);
                }
            }

            foreach (var item in selectedItems)
            {
                selectTreeViewModel.Value.MulticastMessages[item] &= ~MulticastMessageState.IsLocked;
            }
        }

        private void _textEditorLockAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatTreeViewModel;
            if (selectTreeViewModel == null) return;

            foreach (var item in selectTreeViewModel.Value.MulticastMessages.Keys.ToArray())
            {
                selectTreeViewModel.Value.MulticastMessages[item] |= MulticastMessageState.IsLocked;
            }
        }

        private void _textEditorUnlockAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatTreeViewModel;
            if (selectTreeViewModel == null) return;

            foreach (var item in selectTreeViewModel.Value.MulticastMessages.Keys.ToArray())
            {
                selectTreeViewModel.Value.MulticastMessages[item] &= ~MulticastMessageState.IsLocked;
            }
        }

        private void _textEditorResponseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewModel = _treeView.SelectedItem as ChatTreeViewModel;
            if (selectTreeViewModel == null) return;

            var comment = new StringBuilder();
            foreach (var line in _textEditor.SelectedText
                .Trim('\r', '\n')
                .Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                comment.AppendLine(string.Format(">> {0}", line));
            }

            comment.AppendLine();

            var window = new MulticastMessageEditWindow(
                selectTreeViewModel.Value.Tag,
                comment.ToString(),
                _amoebaManager);

            window.Owner = _mainWindow;
            window.Show();
        }

        #endregion

        #region Limit

        private void _limitTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int limit;

            if (int.TryParse(_limitTextBox.Text, out limit))
            {
                Settings.Instance.Global_Limit = Math.Min(Math.Max(limit, 0), 256);
            }

            this.Update();
        }

        private void _limitUpButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.Global_Limit = Math.Min(Settings.Instance.Global_Limit + 1, 256);
            _limitTextBox.Text = Settings.Instance.Global_Limit.ToString();

            this.Update();
        }

        private void _limitDownButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.Global_Limit = Math.Max(Settings.Instance.Global_Limit - 1, 0);
            _limitTextBox.Text = Settings.Instance.Global_Limit.ToString();

            this.Update();
        }

        #endregion

        private void Execute_New(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is ChatCategorizeTreeViewModel)
            {
                _chatCategorizeTreeViewItemNewCategoryMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChatTreeViewModel)
            {

            }
        }

        private void Execute_Delete(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is ChatCategorizeTreeViewModel)
            {
                _chatCategorizeTreeViewItemDeleteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChatTreeViewModel)
            {
                _chatTreeItemTreeViewItemDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is ChatCategorizeTreeViewModel)
            {
                _chatCategorizeTreeViewItemCopyMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChatTreeViewModel)
            {
                _chatTreeItemTreeViewItemCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is ChatCategorizeTreeViewModel)
            {
                _chatCategorizeTreeViewItemCutMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChatTreeViewModel)
            {
                _chatTreeItemTreeViewItemCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is ChatCategorizeTreeViewModel)
            {
                _chatCategorizeTreeViewItemPasteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChatTreeViewModel)
            {

            }
        }
    }

    class MulticastMessageViewModel : IEquatable<MulticastMessageViewModel>
    {
        public MulticastMessageViewModel(MulticastMessageItem multicastMessageItem, MulticastMessageState state)
        {
            this.MulticastMessageItem = multicastMessageItem;
            this.State = state;
        }

        public MulticastMessageItem MulticastMessageItem { get; private set; }
        public MulticastMessageState State { get; private set; }

        public override int GetHashCode()
        {
            if (this.MulticastMessageItem == null) return 0;
            else return this.MulticastMessageItem.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is MulticastMessageViewModel)) return false;

            return this.Equals((MulticastMessageViewModel)obj);
        }

        public bool Equals(MulticastMessageViewModel other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.MulticastMessageItem != other.MulticastMessageItem
                || this.State != other.State)
            {
                return false;
            }

            return true;
        }
    }
}
