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
using Prism.Interactivity.InteractionRequest;

namespace Amoeba.Interface
{
    class ChatControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;

        private Settings _settings;

        public ReactiveProperty<ChatCategoryViewModel> TabViewModel { get; private set; }
        public ReactiveProperty<TreeViewModelBase> TabSelectedItem { get; private set; }
        public DragAcceptDescription DragAcceptDescription { get; private set; }

        public ReactiveCommand TabClickCommand { get; private set; }

        public ReactiveCommand TabNewCategoryCommand { get; private set; }
        public ReactiveCommand TabNewChatCommand { get; private set; }
        public ReactiveCommand TabEditCommand { get; private set; }
        public ReactiveCommand TabDeleteCommand { get; private set; }
        public ReactiveCommand TabCopyCommand { get; private set; }
        public ReactiveCommand TabPasteCommand { get; private set; }

        public ReactiveProperty<ChatMessageInfo[]> Messages { get; private set; }

        public ReactiveCommand NewMessageCommand { get; private set; }

        public DynamicViewModel Config { get; } = new DynamicViewModel();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ChatControlViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();

            this.DragAcceptDescription = new DragAcceptDescription() { Effects = DragDropEffects.Move, Format = "Chat" };
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

        public void Init()
        {
            {
                this.TabViewModel = new ReactiveProperty<ChatCategoryViewModel>().AddTo(_disposable);

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.Messages = new ReactiveProperty<ChatMessageInfo[]>().AddTo(_disposable);

                this.TabClickCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabClickCommand.Subscribe(() => this.TabSelectChanged(this.TabSelectedItem.Value)).AddTo(_disposable);

                this.TabNewCategoryCommand = this.TabSelectedItem.Select(n => n is ChatCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabNewCategoryCommand.Subscribe(() => this.TabNewCategory()).AddTo(_disposable);

                this.TabNewChatCommand = this.TabSelectedItem.Select(n => n is ChatCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabNewChatCommand.Subscribe(() => this.TabNewChat()).AddTo(_disposable);

                this.TabEditCommand = this.TabSelectedItem.Select(n => n is ChatCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabEditCommand.Subscribe(() => this.TabEdit()).AddTo(_disposable);

                this.TabDeleteCommand = this.TabSelectedItem.Select(n => n != this.TabViewModel.Value).ToReactiveCommand().AddTo(_disposable);
                this.TabDeleteCommand.Subscribe(() => this.TabDelete()).AddTo(_disposable);

                this.TabCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabCopyCommand.Subscribe(() => this.TabCopy()).AddTo(_disposable);

                this.TabPasteCommand = this.TabSelectedItem.Select(n => n is ChatCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabPasteCommand.Subscribe(() => this.TabPaste()).AddTo(_disposable);

                this.NewMessageCommand = this.TabSelectedItem.Select(n => n is ChatViewModel).ToReactiveCommand().AddTo(_disposable);
                this.NewMessageCommand.Subscribe(() => this.NewMessage()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(ChatControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.Config.SetPairs(_settings.Load("Config", () => new Dictionary<string, object>()));

                {
                    var model = _settings.Load("Tab", () =>
                    {
                        var categoryInfo = new ChatCategoryInfo() { Name = "Category", IsExpanded = true };
                        categoryInfo.ChatInfos.Add(new ChatInfo() { Tag = new Tag("Amoeba", Sha256.ComputeHash("Amoeba")) });

                        return categoryInfo;
                    });

                    this.TabViewModel.Value = new ChatCategoryViewModel(null, model);
                }
            }
        }

        private void TabSelectChanged(TreeViewModelBase viewModel)
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
                    if (token.IsCancellationRequested) return;

                    var chatCategoryInfos = new List<ChatCategoryInfo>();
                    chatCategoryInfos.Add(this.TabViewModel.Value.Model);

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

        private void TabNewCategory()
        {
            var viewModel = new NameEditWindowViewModel("");
            viewModel.Callback += (name) =>
            {
                var chatCategoryViewModel = this.TabSelectedItem.Value as ChatCategoryViewModel;
                if (chatCategoryViewModel == null) return;

                chatCategoryViewModel.Model.CategoryInfos.Add(new ChatCategoryInfo() { Name = name });
            };

            Messenger.Instance.GetEvent<NameEditWindowViewModelShowEvent>()
                .Publish(viewModel);
        }

        private void TabNewChat()
        {
            var viewModel = new NameEditWindowViewModel("");
            viewModel.Callback += (name) =>
            {
                var chatCategoryViewModel = this.TabSelectedItem.Value as ChatCategoryViewModel;
                if (chatCategoryViewModel == null) return;

                var random = new Random();
                var id = random.GetBytes(32);

                chatCategoryViewModel.Model.ChatInfos.Add(new ChatInfo() { Tag = new Tag(name, id) });
            };

            Messenger.Instance.GetEvent<NameEditWindowViewModelShowEvent>()
                .Publish(viewModel);
        }

        private void TabEdit()
        {
            var viewModel = new NameEditWindowViewModel(this.TabSelectedItem.Value.Name.Value);
            viewModel.Callback += (name) =>
            {
                var chatCategoryViewModel = this.TabSelectedItem.Value as ChatCategoryViewModel;
                if (chatCategoryViewModel == null) return;

                chatCategoryViewModel.Model.Name = name;
            };

            Messenger.Instance.GetEvent<NameEditWindowViewModelShowEvent>()
                .Publish(viewModel);
        }

        private void TabDelete()
        {
            var viewModel = new ConfirmWindowViewModel(ConfirmWindowType.Delete);
            viewModel.Callback += () =>
            {
                if (this.TabSelectedItem.Value is ChatCategoryViewModel chatCategoryViewModel)
                {
                    if (chatCategoryViewModel.Parent == null) return;
                    chatCategoryViewModel.Parent.TryRemove(chatCategoryViewModel);
                }
                else if (this.TabSelectedItem.Value is ChatViewModel chatViewModel)
                {
                    chatViewModel.Parent.TryRemove(chatViewModel);
                }
            };

            Messenger.Instance.GetEvent<ConfirmWindowViewModelShowEvent>()
                .Publish(viewModel);
        }

        private void TabCopy()
        {
            if (this.TabSelectedItem.Value is ChatCategoryViewModel chatCategoryViewModel)
            {
                Clipboard.SetChatCategoryInfos(new ChatCategoryInfo[] { chatCategoryViewModel.Model });
            }
            else if (this.TabSelectedItem.Value is ChatViewModel chatViewModel)
            {
                Clipboard.SetChatInfos(new ChatInfo[] { chatViewModel.Model });
            }
        }

        private void TabPaste()
        {
            if (this.TabSelectedItem.Value is ChatCategoryViewModel chatCategoryViewModel)
            {
                chatCategoryViewModel.Model.CategoryInfos.AddRange(Clipboard.GetChatCategoryInfos());
                chatCategoryViewModel.Model.ChatInfos.AddRange(Clipboard.GetChatInfos());

                foreach (var tag in Clipboard.GetTags())
                {
                    if (chatCategoryViewModel.Model.ChatInfos.Any(n => n.Tag == tag)) continue;
                    chatCategoryViewModel.Model.ChatInfos.Add(new ChatInfo() { Tag = tag });
                }
            }
        }

        private void NewMessage()
        {
            var chatViewModel = this.TabSelectedItem.Value as ChatViewModel;
            if (chatViewModel == null) return;

            var viewModel = new ChatMessageEditWindowViewModel(chatViewModel.Model.Tag, _serviceManager);

            Messenger.Instance.GetEvent<ChatMessageEditWindowShowEvent>()
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
                _settings.Save("Tab", this.TabViewModel.Value.Model);

                _disposable.Dispose();
            }
        }
    }
}
