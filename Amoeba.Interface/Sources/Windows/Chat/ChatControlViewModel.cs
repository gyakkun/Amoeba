using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using Omnius.Utilities;
using Omnius.Security;
using Prism.Events;

namespace Amoeba.Interface
{
    class ChatControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;

        private Settings _settings;

        public ReactiveCommand Tab_NewCategoryCommand { get; private set; }
        public ReactiveCommand Tab_NewChatCommand { get; private set; }
        public ReactiveCommand Tab_CopyCommand { get; private set; }
        public ReactiveCommand Tab_PasteCommand { get; private set; }

        public ReactiveCommand NewMessageCommand { get; private set; }

        public ReactiveProperty<TreeViewModelBase> Tab_SelectedItem { get; private set; }

        public ReactiveProperty<ChatMessageInfo[]> Messages { get; private set; }

        public DynamicViewModel Config { get; } = new DynamicViewModel();

        private CollectionViewSource _treeViewSource;
        public DragAcceptDescription DragAcceptDescription { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ChatControlViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();

            this.DragAcceptDescription = new DragAcceptDescription()
            {
                Effects = DragDropEffects.Move,
                Format = "Chat",
            };
            this.DragAcceptDescription.DragDrop += this.DragAcceptDescription_DragDrop;
        }

        private void DragAcceptDescription_DragDrop(DragAcceptEventArgs args)
        {
            var source = args.Source as TreeViewModelBase;
            var dest = args.Destination as TreeViewModelBase;
            if (source == null || dest == null) return;

            if (dest.GetAncestors().Contains(source)) return;

            if (dest.TryAdd(source))
            {
                source.Parent.TryRemove(source);
            }
        }

        public ICollectionView TreeView
        {
            get
            {
                return _treeViewSource.View;
            }
        }

        public void Init()
        {
            {
                this.Tab_SelectedItem = new ReactiveProperty<TreeViewModelBase>();
                this.Tab_SelectedItem.Subscribe((viewModel) => this.SelectChanged(viewModel)).AddTo(_disposable);

                this.Messages = new ReactiveProperty<ChatMessageInfo[]>().AddTo(_disposable);

                this.Tab_NewCategoryCommand = this.Tab_SelectedItem.Select(n => n is ChatCategoryViewModel).ToReactiveCommand();
                this.Tab_NewCategoryCommand.Subscribe(() => this.NewCategory()).AddTo(_disposable);

                this.Tab_NewChatCommand = this.Tab_SelectedItem.Select(n => n is ChatCategoryViewModel).ToReactiveCommand();
                this.Tab_NewChatCommand.Subscribe(() => this.NewChat()).AddTo(_disposable);

                this.NewMessageCommand = this.Tab_SelectedItem.Select(n => n is ChatViewModel).ToReactiveCommand();
                this.NewMessageCommand.Subscribe(() => this.NewMessage()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(ChatControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.Config.SetPairs(_settings.Load("Config", () => new Dictionary<string, object>()));

                {
                    var chatCategoryInfo = _settings.Load("Category", () =>
                    {
                        var categoryInfo = new ChatCategoryInfo() { Name = "Category", IsExpanded = true };
                        categoryInfo.ChatInfos.Add(new ChatInfo() { Tag = new Tag("Amoeba", Sha256.ComputeHash("Amoeba")) });

                        return categoryInfo;
                    });

                    _treeViewSource = new CollectionViewSource();
                    _treeViewSource.Source = new ChatCategoryViewModel[] { new ChatCategoryViewModel(null, chatCategoryInfo) };
                }
            }
        }

        private void SelectChanged(TreeViewModelBase viewModel)
        {
            if (viewModel is ChatCategoryViewModel chatCategoryViewModel)
            {
                this.Messages.Value = Array.Empty<ChatMessageInfo>();
            }
            else if (viewModel is ChatViewModel chatViewModel)
            {
                this.Messages.Value = chatViewModel.Model.Messages.ToArray();
            }
        }

        private void WatchThread(CancellationToken token)
        {
            for (;;)
            {
                var chatInfos = new List<ChatInfo>();

                App.Current.Dispatcher.Invoke(() =>
                {
                    var chatCategoryInfos = new List<ChatCategoryInfo>(((ChatCategoryViewModel[])_treeViewSource.Source).Select(n => n.Model));

                    for (int i = 0; i < chatCategoryInfos.Count; i++)
                    {
                        chatCategoryInfos.AddRange(chatCategoryInfos[i].CategoryInfos);
                        chatInfos.AddRange(chatCategoryInfos[i].ChatInfos);
                    }
                });

                foreach (var chatInfo in chatInfos)
                {
                    if (token.IsCancellationRequested) return;

                    var messages = new HashSet<MulticastMessage<ChatMessage>>(_serviceManager.GetChatMessages(chatInfo.Tag).Result);

                    lock (chatInfo.Messages.LockObject)
                    {
                        messages.ExceptWith(chatInfo.Messages.Select(n => n.Message));

                        foreach (var chatMessageInfo in chatInfo.Messages)
                        {
                            chatMessageInfo.State |= ~ChatMessageState.New;
                        }

                        foreach (var message in messages)
                        {
                            chatInfo.Messages.Add(new ChatMessageInfo() { Message = message, State = ChatMessageState.New });
                        }

                        chatInfo.Messages.Sort((x, y) => x.Message.CreationTime.CompareTo(y.Message.CreationTime));
                    }
                }

                if (token.WaitHandle.WaitOne(1000 * 30)) return;
            }
        }

        private void NewCategory()
        {
            var viewModel = new NameEditWindowViewModel();
            viewModel.OkEvent += (s, e) =>
            {
                var chatCategoryViewModel = this.Tab_SelectedItem.Value as ChatCategoryViewModel;
                if (chatCategoryViewModel == null) return;

                chatCategoryViewModel.Model.CategoryInfos.Add(new ChatCategoryInfo() { Name = e.Name });
            };

            MainWindowMessenger.ShowEvent.GetEvent<PubSubEvent<NameEditWindowViewModel>>()
                .Publish(viewModel);
        }

        private void NewChat()
        {
            var viewModel = new NameEditWindowViewModel();
            viewModel.OkEvent += (s, e) =>
            {
                var chatCategoryViewModel = this.Tab_SelectedItem.Value as ChatCategoryViewModel;
                if (chatCategoryViewModel == null) return;

                var random = new Random();
                var id = random.GetBytes(32);

                chatCategoryViewModel.Model.ChatInfos.Add(new ChatInfo() { Tag = new Tag(e.Name, id) });
            };

            MainWindowMessenger.ShowEvent.GetEvent<PubSubEvent<NameEditWindowViewModel>>()
                .Publish(viewModel);
        }

        private void NewMessage()
        {
            var chatViewModel = this.Tab_SelectedItem.Value as ChatViewModel;
            if (chatViewModel == null) return;

            var viewModel = new ChatMessageEditWindowViewModel(chatViewModel.Model.Tag, _serviceManager);

            MainWindowMessenger.ShowEvent.GetEvent<PubSubEvent<ChatMessageEditWindowViewModel>>()
                .Publish(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _watchTaskManager.Stop();
                _watchTaskManager.Dispose();

                _settings.Save("Config", this.Config.GetPairs());

                _disposable.Dispose();
            }
        }
    }
}
