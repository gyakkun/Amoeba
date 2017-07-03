using System.Reactive.Disposables;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class ChatCategoryViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ReadOnlyReactiveCollection<ChatThreadViewModel> Chats { get; private set; }
        public ReadOnlyReactiveCollection<ChatCategoryViewModel> Categories { get; private set; }

        public ChatCategoryInfo Model { get; private set; }

        public ChatCategoryViewModel(TreeViewModelBase parent, ChatCategoryInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ToReactivePropertyAsSynchronized(n => n.Name).AddTo(_disposable);
            this.IsSelected = model.ToReactivePropertyAsSynchronized(n => n.IsSelected).AddTo(_disposable);
            this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
            this.Chats = model.ThreadInfos.ToReadOnlyReactiveCollection(n => new ChatThreadViewModel(this, n)).AddTo(_disposable);
            this.Categories = model.CategoryInfos.ToReadOnlyReactiveCollection(n => new ChatCategoryViewModel(this, n)).AddTo(_disposable);
        }

        public override string DragFormat { get { return "Chat"; } }

        public override bool TryAdd(object value)
        {
            if (value is ChatCategoryViewModel categoryViewModel)
            {
                this.Model.CategoryInfos.Add(categoryViewModel.Model);
                return true;
            }
            else if (value is ChatThreadViewModel chatViewModel)
            {
                this.Model.ThreadInfos.Add(chatViewModel.Model);
                return true;
            }

            return false;
        }

        public override bool TryRemove(object value)
        {
            if (value is ChatCategoryViewModel categoryViewModel)
            {
                return this.Model.CategoryInfos.Remove(categoryViewModel.Model);
            }
            else if (value is ChatThreadViewModel chatViewModel)
            {
                return this.Model.ThreadInfos.Remove(chatViewModel.Model);
            }

            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _disposable.Dispose();
            }
        }
    }
}
