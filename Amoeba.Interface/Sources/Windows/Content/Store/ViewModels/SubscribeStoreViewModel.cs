using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class SubscribeStoreViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ReactiveProperty<bool> IsUpdated { get; private set; }
        public ReadOnlyReactiveCollection<SubscribeBoxViewModel> Boxes { get; private set; }

        public SubscribeStoreInfo Model { get; private set; }

        public SubscribeStoreViewModel(TreeViewModelBase parent, SubscribeStoreInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ObserveProperty(n => n.AuthorSignature).Select(n => n.ToString()).ToReactiveProperty().AddTo(_disposable);
            this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
            this.IsUpdated = model.ToReactivePropertyAsSynchronized(n => n.IsUpdated).AddTo(_disposable);
            this.Boxes = model.BoxInfos.ToReadOnlyReactiveCollection(n => new SubscribeBoxViewModel(this, n)).AddTo(_disposable);
        }

        public override string DragFormat { get { return "Store"; } }

        public override bool TryAdd(object value)
        {
            return false;
        }

        public override bool TryRemove(object value)
        {
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
