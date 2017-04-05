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
using Prism.Interactivity.InteractionRequest;
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

        private Settings _settings;

        private TaskManager _watchTaskManager;

        public ReactiveProperty<ChatMessageInfo[]> Messages { get; private set; }

        public ReactiveCommand NewMessageCommand { get; private set; }

        public DynamicViewModel Config { get; } = new DynamicViewModel();

        private CollectionViewSource _treeViewSource;

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ChatControlViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            _treeViewSource = new CollectionViewSource();

            {
                var boxInfo = new ChatCategoryInfo() { Name = "Category" };
                boxInfo.CategoryInfos.Add(new ChatCategoryInfo() { Name = "Amoeba" });
                boxInfo.ChatInfos.Add(new ChatInfo() { });

                _treeViewSource.Source = new ChatCategoryViewModel[] { new ChatCategoryViewModel(null, boxInfo) };
            }

            this.Load();
        }

        public void Load()
        {
            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(CrowdControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.Config.SetPairs(_settings.Load("Config", () => new Dictionary<string, object>()));
            }

            {
                this.Messages = new ReactiveProperty<ChatMessageInfo[]>().AddTo(_disposable);

                this.NewMessageCommand = new ReactiveCommand();
                this.NewMessageCommand.Subscribe(() => this.NewMessage());
            }

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();

            {
                this.DragAcceptDescription = new DragAcceptDescription();
                this.DragAcceptDescription.DragOver += this.OnDragOver;
                this.DragAcceptDescription.DragDrop += this.OnDragDrop;
            }
        }

        public void Save()
        {
            _settings.Save("Config", this.Config.GetPairs());
        }

        private void WatchThread(CancellationToken token)
        {
            var tag = new Tag("Amoeba", Sha256.ComputeHash("Amoeba"));

            for (;;)
            {
                if (token.WaitHandle.WaitOne(1000)) return;
                {
                    var infos = new List<ChatMessageInfo>();

                    foreach (var message in _serviceManager.GetChatMessages(tag).Result)
                    {
                        infos.Add(new ChatMessageInfo() { Message = message });
                    }

                    infos.Sort((x, y) => x.Message.CreationTime.CompareTo(y.Message.CreationTime));

                    if (infos.Count != 0)
                    {
                        this.Messages.Value = infos.ToArray();
                    }
                }
            }
        }

        public void CopyLocation()
        {

        }

        public void PasteLocation()
        {
            _serviceManager.SetCrowdLocations(Clipboard.GetLocations());
        }

        public ICollectionView TreeView
        {
            get
            {
                return _treeViewSource.View;
            }
        }

        public DragAcceptDescription DragAcceptDescription { get; private set; }

        private void OnDragOver(DragEventArgs args)
        {
            if (args.AllowedEffects.HasFlag(DragDropEffects.Move))
            {
                args.Effects = DragDropEffects.Move;
            }
        }

        void OnDragDrop(DragEventArgs args)
        {
            var target = this.GetDropDestination((UIElement)args.OriginalSource);
            if (target == null) return;

            if (args.Data.GetDataPresent("Store"))
            {
                var source = args.Data.GetData("Store") as TreeViewModelBase;
                if (source == null) return;

                if (target.GetAncestors().Contains(source)) return;

                if (target.TryAdd(source))
                {
                    source.Parent.TryRemove(source);
                }
            }
        }

        private TreeViewModelBase GetDropDestination(UIElement originalSource)
        {
            var element = originalSource.FindAncestor<TreeViewItem>();
            if (element == null) return null;

            return element.DataContext as TreeViewModelBase;
        }

        private void NewMessage()
        {
            MainWindowMessenger.ShowEvent.GetEvent<PubSubEvent<ChatMessageEditWindowViewModel>>()
                .Publish(new ChatMessageEditWindowViewModel(_serviceManager));
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _watchTaskManager.Stop();
                _watchTaskManager.Dispose();

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
