using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class ChatControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private MessageManager _messageManager;
        private TaskManager _watchTaskManager;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private Settings _settings;

        public ReactiveProperty<ChatCategoryViewModel> TabViewModel { get; private set; }
        public ReactiveProperty<TreeViewModelBase> TabSelectedItem { get; private set; }
        public DragAcceptDescription DragAcceptDescription { get; private set; }

        public ReactiveCommand TabClickCommand { get; private set; }

        public ReactiveCommand TabNewCategoryCommand { get; private set; }
        public ReactiveCommand TabNewChatCommand { get; private set; }
        public ReactiveCommand TabEditCommand { get; private set; }
        public ReactiveCommand TabDeleteCommand { get; private set; }
        public ReactiveCommand TabCutCommand { get; private set; }
        public ReactiveCommand TabCopyCommand { get; private set; }
        public ReactiveCommand TabPasteCommand { get; private set; }

        public ReactiveProperty<AvalonEditChatMessagesInfo> Info { get; private set; }
        public ReactiveProperty<string> SelectedText { get; private set; }

        public ReactiveCommand CopyCommand { get; private set; }
        public ReactiveCommand ResponseCommand { get; private set; }
        public ReactiveCommand NewMessageCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ChatControlViewModel(ServiceManager serviceManager, MessageManager messageManager)
        {
            _serviceManager = serviceManager;
            _messageManager = messageManager;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();

            this.DragAcceptDescription = new DragAcceptDescription() { Effects = DragDropEffects.Move, Format = "Chat" };
            this.DragAcceptDescription.DragDrop += this.DragAcceptDescription_DragDrop;
        }

        private void DragAcceptDescription_DragDrop(DragAcceptEventArgs args)
        {
            var src = args.Source as TreeViewModelBase;
            var dest = args.Destination as TreeViewModelBase;
            if (src == null || dest == null) return;

            if (dest.GetAncestors().Contains(src)) return;

            if (dest.TryAdd(src))
            {
                src.Parent.TryRemove(src);
            }
        }

        private void Init()
        {
            {
                this.TabViewModel = new ReactiveProperty<ChatCategoryViewModel>().AddTo(_disposable);

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.Info = new ReactiveProperty<AvalonEditChatMessagesInfo>().AddTo(_disposable);
                this.SelectedText = new ReactiveProperty<string>().AddTo(_disposable);

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

                this.TabCutCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabCutCommand.Subscribe(() => this.TabCut()).AddTo(_disposable);

                this.TabCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabCopyCommand.Subscribe(() => this.TabCopy()).AddTo(_disposable);

                this.TabPasteCommand = this.TabSelectedItem.Select(n => n is ChatCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabPasteCommand.Subscribe(() => this.TabPaste()).AddTo(_disposable);

                this.CopyCommand = this.SelectedText.Select(n => !string.IsNullOrEmpty(n)).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);

                this.ResponseCommand = this.SelectedText.Select(n => !string.IsNullOrEmpty(n)).ToReactiveCommand().AddTo(_disposable);
                this.ResponseCommand.Subscribe(() => this.Response()).AddTo(_disposable);

                this.NewMessageCommand = this.TabSelectedItem.Select(n => n is ChatThreadViewModel).ToReactiveCommand().AddTo(_disposable);
                this.NewMessageCommand.Subscribe(() => this.NewMessage()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(ChatControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));

                {
                    var model = _settings.Load("ChatCategoryInfo", () =>
                    {
                        var categoryInfo = new ChatCategoryInfo() { Name = "Category", IsExpanded = true };
                        categoryInfo.ThreadInfos.Add(new ChatThreadInfo() { Tag = new Tag("Amoeba", Sha256.ComputeHash("Amoeba")) });

                        return categoryInfo;
                    });

                    this.TabViewModel.Value = new ChatCategoryViewModel(null, model);
                }
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }
        }

        private void TabSelectChanged(TreeViewModelBase viewModel)
        {
            if (viewModel is ChatCategoryViewModel chatCategoryViewModel)
            {
                this.Info.Value = null;
            }
            else if (viewModel is ChatThreadViewModel chatViewModel)
            {
                this.Info.Value = new AvalonEditChatMessagesInfo(chatViewModel.Model.Messages, _messageManager.TrustSignatures);
            }
        }

        private void WatchThread(CancellationToken token)
        {
            for (;;)
            {
                var chatThreadInfos = new List<ChatThreadInfo>();

                try
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        var chatCategoryInfos = new List<ChatCategoryInfo>();
                        chatCategoryInfos.Add(this.TabViewModel.Value.Model);

                        for (int i = 0; i < chatCategoryInfos.Count; i++)
                        {
                            chatCategoryInfos.AddRange(chatCategoryInfos[i].CategoryInfos);
                            chatThreadInfos.AddRange(chatCategoryInfos[i].ThreadInfos);
                        }
                    }, DispatcherPriority.Background, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                foreach (var chatInfo in chatThreadInfos)
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

            Messenger.Instance.GetEvent<NameEditWindowShowEvent>()
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

                chatCategoryViewModel.Model.ThreadInfos.Add(new ChatThreadInfo() { Tag = new Tag(name, id) });
            };

            Messenger.Instance.GetEvent<NameEditWindowShowEvent>()
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

            Messenger.Instance.GetEvent<NameEditWindowShowEvent>()
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
                else if (this.TabSelectedItem.Value is ChatThreadViewModel chatViewModel)
                {
                    chatViewModel.Parent.TryRemove(chatViewModel);
                }
            };

            Messenger.Instance.GetEvent<ConfirmWindowShowEvent>()
                .Publish(viewModel);
        }

        private void TabCut()
        {
            if (this.TabSelectedItem.Value is ChatCategoryViewModel chatCategoryViewModel)
            {
                if (chatCategoryViewModel.Parent == null) return;
                chatCategoryViewModel.Parent.TryRemove(chatCategoryViewModel);
                Clipboard.SetChatCategoryInfos(new ChatCategoryInfo[] { chatCategoryViewModel.Model });
            }
            else if (this.TabSelectedItem.Value is ChatThreadViewModel chatViewModel)
            {
                chatViewModel.Parent.TryRemove(chatViewModel);
                Clipboard.SetChatThreadInfos(new ChatThreadInfo[] { chatViewModel.Model });
            }
        }

        private void TabCopy()
        {
            if (this.TabSelectedItem.Value is ChatCategoryViewModel chatCategoryViewModel)
            {
                Clipboard.SetChatCategoryInfos(new ChatCategoryInfo[] { chatCategoryViewModel.Model });
            }
            else if (this.TabSelectedItem.Value is ChatThreadViewModel chatViewModel)
            {
                Clipboard.SetChatThreadInfos(new ChatThreadInfo[] { chatViewModel.Model });
            }
        }

        private void TabPaste()
        {
            if (this.TabSelectedItem.Value is ChatCategoryViewModel chatCategoryViewModel)
            {
                chatCategoryViewModel.Model.CategoryInfos.AddRange(Clipboard.GetChatCategoryInfos());
                chatCategoryViewModel.Model.ThreadInfos.AddRange(Clipboard.GetChatThreadInfos());

                foreach (var tag in Clipboard.GetTags())
                {
                    if (chatCategoryViewModel.Model.ThreadInfos.Any(n => n.Tag == tag)) continue;
                    chatCategoryViewModel.Model.ThreadInfos.Add(new ChatThreadInfo() { Tag = tag });
                }
            }
        }

        private void Copy()
        {
            Clipboard.SetText(this.SelectedText.Value);
        }

        private void Response()
        {
            var chatViewModel = this.TabSelectedItem.Value as ChatThreadViewModel;
            if (chatViewModel == null) return;

            var sb = new StringBuilder();

            foreach (string line in this.SelectedText.Value.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
            {
                sb.AppendLine(">> " + line);
            }

            var viewModel = new ChatMessageEditWindowViewModel(chatViewModel.Model.Tag, sb.ToString(), _serviceManager, _messageManager, _tokenSource.Token);

            Messenger.Instance.GetEvent<ChatMessageEditWindowShowEvent>()
                .Publish(viewModel);
        }

        private void NewMessage()
        {
            var chatViewModel = this.TabSelectedItem.Value as ChatThreadViewModel;
            if (chatViewModel == null) return;

            var viewModel = new ChatMessageEditWindowViewModel(chatViewModel.Model.Tag, "", _serviceManager, _messageManager, _tokenSource.Token);

            Messenger.Instance.GetEvent<ChatMessageEditWindowShowEvent>()
                .Publish(viewModel);
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
                _settings.Save("ChatCategoryInfo", this.TabViewModel.Value.Model);
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                Backup.Instance.SaveEvent -= this.Save;

                _tokenSource.Cancel();
                _tokenSource.Dispose();

                _watchTaskManager.Stop();
                _watchTaskManager.Dispose();

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
