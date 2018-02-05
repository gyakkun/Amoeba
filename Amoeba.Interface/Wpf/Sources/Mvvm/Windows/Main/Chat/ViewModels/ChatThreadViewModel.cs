using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class ChatThreadViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public ReactiveProperty<bool> IsUpdated { get; private set; }
        public ReactiveProperty<int> Count { get; private set; }
        public ReactiveProperty<byte[]> Id { get; private set; }

        public ChatThreadInfo Model { get; private set; }

        public ChatThreadViewModel(TreeViewModelBase parent, ChatThreadInfo model)
            : base(parent)
        {
            this.Model = model;

            this.IsSelected = new ReactiveProperty<bool>().AddTo(_disposable);
            this.Name = model.ObserveProperty(n => n.Tag).Select(n => n.Name).ToReactiveProperty().AddTo(_disposable);
            this.IsUpdated = model.ToReactivePropertyAsSynchronized(n => n.IsUpdated).AddTo(_disposable);
            this.Count = new ReactiveProperty<int>(0).AddTo(_disposable);
            this.Id = model.ObserveProperty(n => n.Tag).Select(n => n.Id).ToReactiveProperty().AddTo(_disposable);
        }

        public override string DragFormat { get { return "Amoeba_Chat"; } }

        public override bool TryAdd(object value)
        {
            return false;
        }

        public override bool TryRemove(object value)
        {
            return false;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                _disposable.Dispose();
            }
        }
    }
}
