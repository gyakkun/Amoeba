using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class StoreSignatureViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public ReactiveProperty<bool> IsUpdated { get; private set; }
        public ReadOnlyReactiveCollection<StoreBoxViewModel> BoxViewModels { get; private set; }

        public StoreSignatureInfo Model { get; private set; }

        public StoreSignatureViewModel(TreeViewModelBase parent, StoreSignatureInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ObserveProperty(n => n.AuthorSignature).Select(n => n.ToString()).ToReactiveProperty().AddTo(_disposable);
            this.IsSelected = new ReactiveProperty<bool>().AddTo(_disposable);
            this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
            this.IsUpdated = model.ToReactivePropertyAsSynchronized(n => n.IsUpdated).AddTo(_disposable);
            this.BoxViewModels = model.BoxInfos.ToReadOnlyReactiveCollection(n => new StoreBoxViewModel(this, n)).AddTo(_disposable);
        }

        public override string DragFormat { get { return "Amoeba_Store"; } }

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
