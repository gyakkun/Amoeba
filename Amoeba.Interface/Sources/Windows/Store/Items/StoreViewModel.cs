using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class StoreViewModel : TreeViewModelBase
    {
        private volatile bool _disposed;

        private CompositeDisposable _disposable = new CompositeDisposable();

        public ReactiveProperty<bool> IsUpdated { get; private set; }
        public ReadOnlyReactiveCollection<BoxViewModel> Boxes { get; private set; }

        public StoreInfo Model { get; private set; }

        public StoreViewModel(TreeViewModelBase parent, StoreInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ToReactivePropertyAsSynchronized(n => n.Name).AddTo(_disposable);
            this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
            this.IsUpdated = model.ToReactivePropertyAsSynchronized(n => n.IsUpdated).AddTo(_disposable);
            this.Boxes = model.BoxInfos.ToReadOnlyReactiveCollection(n => new BoxViewModel(this, n)).AddTo(_disposable);
        }

        public override string DragFormat { get { return "Store"; } }

        public override bool TryAdd(object value)
        {
            throw new NotImplementedException();
        }

        public override bool TryRemove(object value)
        {
            throw new NotImplementedException();
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
