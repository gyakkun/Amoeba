using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class RelationSignatureViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ReadOnlyReactiveCollection<RelationSignatureViewModel> Children { get; private set; }

        public RelationSignatureInfo Model { get; private set; }

        public RelationSignatureViewModel(TreeViewModelBase parent, RelationSignatureInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ObserveProperty(n => n.Signature).Select(n => n.ToString()).ToReactiveProperty().AddTo(_disposable);
            this.IsExpanded = new ReactiveProperty<bool>(false).AddTo(_disposable);
            this.Children = model.Children.ToReadOnlyReactiveCollection(n => new RelationSignatureViewModel(this, n)).AddTo(_disposable);
        }

        public override string DragFormat { get { return null; } }

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
