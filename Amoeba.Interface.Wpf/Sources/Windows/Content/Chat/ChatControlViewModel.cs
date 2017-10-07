using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Amoeba.Messages;
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

        private DialogService _dialogService;

        public ReactiveProperty<ChatCategoryViewModel> TabViewModel { get; private set; }
        public ReactiveProperty<TreeViewModelBase> TabSelectedItem { get; private set; }
        public DragAcceptDescription DragAcceptDescription { get; private set; }

        public ReactiveCommand TabClickCommand { get; private set; }

        public ReactiveCommand TabNewCategoryCommand { get; private set; }
        public ReactiveCommand TabEditCommand { get; private set; }
        public ReactiveCommand TabDeleteCommand { get; private set; }
        public ReactiveCommand TabCutCommand { get; private set; }
        public ReactiveCommand TabCopyCommand { get; private set; }
        public ReactiveCommand TabPasteCommand { get; private set; }
        public ReactiveCommand TabTagListCommand { get; private set; }

        public ReactiveCommand TrustFilterCommand { get; private set; }
        public ReactiveProperty<bool> IsTrustFilterEnable { get; private set; }

        public ReactiveCommand NewFilterCommand { get; private set; }
        public ReactiveProperty<bool> IsNewFilterEnable { get; private set; }

        public ReactiveProperty<AvalonEditChatMessagesInfo> Info { get; private set; }
        public ReactiveProperty<string> SelectedText { get; private set; }

        public ReactiveCommand CopyCommand { get; private set; }
        public ReactiveCommand ResponseCommand { get; private set; }
        public ReactiveCommand NewMessageCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ChatControlViewModel(ServiceManager serviceManager, MessageManager messageManager, DialogService dialogService)
        {
            _serviceManager = serviceManager;
            _messageManager = messageManager;
            _dialogService = dialogService;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();

            this.DragAcceptDescription = new DragAcceptDescription() { Effects = DragDropEffects.Move, Format = "Amoeba_Chat" };
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
                IObservable<object> clipboardObservable;
                {
                    var returnObservable = Observable.Return((object)null);
                    var watchObservable = Observable.FromEventPattern<EventHandler, EventArgs>(h => Clipboard.ClipboardChanged += h, h => Clipboard.ClipboardChanged -= h).Select(n => (object)null);
                    clipboardObservable = Observable.Merge(returnObservable, watchObservable);
                }

                this.TabViewModel = new ReactiveProperty<ChatCategoryViewModel>().AddTo(_disposable);

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.Info = new ReactiveProperty<AvalonEditChatMessagesInfo>().AddTo(_disposable);
                this.SelectedText = new ReactiveProperty<string>().AddTo(_disposable);

                this.TabClickCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabClickCommand.Where(n => n == this.TabSelectedItem.Value).Subscribe((_) => this.Refresh()).AddTo(_disposable);

                this.TabNewCategoryCommand = this.TabSelectedItem.Select(n => n is ChatCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabNewCategoryCommand.Subscribe(() => this.TabNewCategory()).AddTo(_disposable);

                this.TabEditCommand = this.TabSelectedItem.Select(n => n is ChatCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabEditCommand.Subscribe(() => this.TabEdit()).AddTo(_disposable);

                this.TabDeleteCommand = this.TabSelectedItem.Select(n => n != this.TabViewModel.Value).ToReactiveCommand().AddTo(_disposable);
                this.TabDeleteCommand.Subscribe(() => this.TabDelete()).AddTo(_disposable);

                this.TabCutCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabCutCommand.Subscribe(() => this.TabCut()).AddTo(_disposable);

                this.TabCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabCopyCommand.Subscribe(() => this.TabCopy()).AddTo(_disposable);

                this.TabPasteCommand = this.TabSelectedItem.Select(n => n is ChatCategoryViewModel)
                    .CombineLatest(clipboardObservable.Select(n => Clipboard.ContainsChatCategoryInfo() || Clipboard.ContainsChatThreadInfo()), (r1, r2) => r1 && r2).ToReactiveCommand().AddTo(_disposable);
                this.TabPasteCommand.Subscribe(() => this.TabPaste()).AddTo(_disposable);

                this.TabTagListCommand = this.TabSelectedItem.Select(n => n is ChatCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabTagListCommand.Subscribe(() => this.TabTagList()).AddTo(_disposable);

                this.TrustFilterCommand = this.TabSelectedItem.Select(n => n is ChatThreadViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TrustFilterCommand.Subscribe(() => this.TrustFilter()).AddTo(_disposable);

                this.IsTrustFilterEnable = new ReactiveProperty<bool>().AddTo(_disposable);

                this.NewFilterCommand = this.TabSelectedItem.Select(n => n is ChatThreadViewModel).ToReactiveCommand().AddTo(_disposable);
                this.NewFilterCommand.Subscribe(() => this.NewFilter()).AddTo(_disposable);

                this.IsNewFilterEnable = new ReactiveProperty<bool>().AddTo(_disposable);

                this.NewMessageCommand = this.TabSelectedItem.Select(n => n is ChatThreadViewModel).ToReactiveCommand().AddTo(_disposable);
                this.NewMessageCommand.Subscribe(() => this.NewMessage()).AddTo(_disposable);

                this.CopyCommand = this.SelectedText.Select(n => !string.IsNullOrEmpty(n)).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);

                this.ResponseCommand = this.SelectedText.Select(n => !string.IsNullOrEmpty(n)).ToReactiveCommand().AddTo(_disposable);
                this.ResponseCommand.Subscribe(() => this.Response()).AddTo(_disposable);
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
                        categoryInfo.ThreadInfos.Add(new ChatThreadInfo() { Tag = new Tag("Random", Sha256.ComputeHash("Random")) });

                        return categoryInfo;
                    });

                    this.TabViewModel.Value = new ChatCategoryViewModel(null, model);
                }
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }
        }

        private void Refresh()
        {
            this.TabSelectChanged(this.TabSelectedItem.Value);
        }

        private void TabSelectChanged(TreeViewModelBase viewModel)
        {
            if (viewModel is ChatCategoryViewModel chatCategoryViewModel)
            {
                this.IsTrustFilterEnable.Value = false;
                this.IsNewFilterEnable.Value = false;
                this.Info.Value = null;
            }
            else if (viewModel is ChatThreadViewModel chatThreadViewModel)
            {
                this.IsTrustFilterEnable.Value = chatThreadViewModel.Model.IsTrustMessageOnly;
                this.IsNewFilterEnable.Value = chatThreadViewModel.Model.IsNewMessageOnly;

                var trustSignatures = new HashSet<Signature>(_messageManager.TrustSignatures);
                var messages = new List<ChatMessageInfo>();

                lock (chatThreadViewModel.Model.Messages.LockObject)
                {
                    foreach (var info in chatThreadViewModel.Model.Messages.ToArray())
                    {
                        if (chatThreadViewModel.Model.IsTrustMessageOnly && !trustSignatures.Contains(info.Message.AuthorSignature)) continue;
                        if (chatThreadViewModel.Model.IsNewMessageOnly && !info.State.HasFlag(ChatMessageState.New)) continue;

                        messages.Add(new ChatMessageInfo() { Message = info.Message, State = info.State });

                        info.State &= ~ChatMessageState.New;
                    }
                }

                chatThreadViewModel.Model.IsUpdated = false;
                chatThreadViewModel.Count.Value = 0;

                this.Info.Value = new AvalonEditChatMessagesInfo(messages, trustSignatures);
            }
        }

        private void WatchThread(CancellationToken token)
        {
            for (; ; )
            {
                var chatThreadViewModels = new List<ChatThreadViewModel>();

                try
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        var chatCategoryViewModels = new List<ChatCategoryViewModel>();
                        chatCategoryViewModels.Add(this.TabViewModel.Value);

                        for (int i = 0; i < chatCategoryViewModels.Count; i++)
                        {
                            chatCategoryViewModels.AddRange(chatCategoryViewModels[i].Categories);
                            chatThreadViewModels.AddRange(chatCategoryViewModels[i].Threads);
                        }
                    }, DispatcherPriority.Background, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                var trustSignatures = new HashSet<Signature>(_messageManager.TrustSignatures);

                foreach (var chatThreadViewModel in chatThreadViewModels)
                {
                    if (token.IsCancellationRequested) return;

                    var newMessages = new HashSet<MulticastMessage<ChatMessage>>(_serviceManager.GetChatMessages(chatThreadViewModel.Model.Tag, CancellationToken.None).Result);

                    lock (chatThreadViewModel.Model.Messages.LockObject)
                    {
                        newMessages.ExceptWith(chatThreadViewModel.Model.Messages.Select(n => n.Message));

                        var messageInfos = new List<ChatMessageInfo>();
                        messageInfos.AddRange(chatThreadViewModel.Model.Messages);

                        foreach (var message in newMessages)
                        {
                            messageInfos.Add(new ChatMessageInfo() { Message = message, State = ChatMessageState.New });
                        }

                        messageInfos.Sort((x, y) => y.Message.CreationTime.CompareTo(x.Message.CreationTime));

                        chatThreadViewModel.Model.Messages.Clear();

                        foreach (var info in messageInfos.ToArray())
                        {
                            if (chatThreadViewModel.Model.IsTrustMessageOnly && !trustSignatures.Contains(info.Message.AuthorSignature)) continue;

                            chatThreadViewModel.Model.Messages.Add(info);
                            if (chatThreadViewModel.Model.Messages.Count >= 1024) break;
                        }
                    }

                    try
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            if (token.IsCancellationRequested) return;

                            int newCount = chatThreadViewModel.Model.Messages.Count(n => n.State.HasFlag(ChatMessageState.New));

                            chatThreadViewModel.Model.IsUpdated = (newCount > 0);
                            chatThreadViewModel.Count.Value = newCount;
                        }, DispatcherPriority.Background, token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
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

            _dialogService.Show(viewModel);
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

            _dialogService.Show(viewModel);
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

            _dialogService.Show(viewModel);
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

        private void TabTagList()
        {
            var tags = new HashSet<Tag>();
            {
                foreach (var profile in _messageManager.GetProfiles())
                {
                    tags.UnionWith(profile.Value.Tags);
                }

                {
                    var chatCategoryViewModels = new List<ChatCategoryViewModel>();
                    chatCategoryViewModels.Add(this.TabViewModel.Value);

                    for (int i = 0; i < chatCategoryViewModels.Count; i++)
                    {
                        chatCategoryViewModels.AddRange(chatCategoryViewModels[i].Categories);
                        tags.ExceptWith(chatCategoryViewModels[i].Threads.Select(n => n.Model.Tag));
                    }
                }
            }

            if (this.TabSelectedItem.Value is ChatCategoryViewModel chatCategoryViewModel)
            {
                var viewModel = new ChatTagListWindowViewModel(tags);
                viewModel.Callback += (tag) =>
                {
                    chatCategoryViewModel.Model.ThreadInfos.Add(new ChatThreadInfo() { Tag = tag });
                };

                _dialogService.Show(viewModel);
            }
        }

        private void TrustFilter()
        {
            if (this.TabSelectedItem.Value is ChatThreadViewModel chatThreadViewModel)
            {
                chatThreadViewModel.Model.IsTrustMessageOnly = !chatThreadViewModel.Model.IsTrustMessageOnly;

                if (chatThreadViewModel.Model.IsTrustMessageOnly)
                {
                    var info = this.Info.Value;
                    var trustSignatures = new HashSet<Signature>(info.TrustSignatures);
                    var messages = info.ChatMessageInfos.Where(n => trustSignatures.Contains(n.Message.AuthorSignature));

                    this.Info.Value = new AvalonEditChatMessagesInfo(messages, trustSignatures);
                }
                else
                {
                    this.Refresh();
                }
            }
        }

        private void NewFilter()
        {
            if (this.TabSelectedItem.Value is ChatThreadViewModel chatThreadViewModel)
            {
                chatThreadViewModel.Model.IsNewMessageOnly = !chatThreadViewModel.Model.IsNewMessageOnly;

                if (chatThreadViewModel.Model.IsNewMessageOnly)
                {
                    var info = this.Info.Value;
                    var messages = info.ChatMessageInfos.Where(n => n.State.HasFlag(ChatMessageState.New));

                    this.Info.Value = new AvalonEditChatMessagesInfo(messages, info.TrustSignatures);
                }
                else
                {
                    this.Refresh();
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

            _dialogService.Show(viewModel);
        }

        private void NewMessage()
        {
            var chatViewModel = this.TabSelectedItem.Value as ChatThreadViewModel;
            if (chatViewModel == null) return;

            var viewModel = new ChatMessageEditWindowViewModel(chatViewModel.Model.Tag, "", _serviceManager, _messageManager, _tokenSource.Token);

            _dialogService.Show(viewModel);
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
