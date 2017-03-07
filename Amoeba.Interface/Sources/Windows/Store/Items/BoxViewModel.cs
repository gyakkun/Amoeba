using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Omnius.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class BoxViewModel : TreeViewModelBase
    {
        private volatile bool _disposed;

        private CompositeDisposable _disposable = new CompositeDisposable();

        public ReadOnlyReactiveCollection<SeedViewModel> Seeds { get; private set; }
        public ReadOnlyReactiveCollection<BoxViewModel> Boxes { get; private set; }

        public BoxInfo Model { get; private set; }

        public BoxViewModel(TreeViewModelBase parent, BoxInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ToReactivePropertyAsSynchronized(n => n.Name).AddTo(_disposable);
            this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
            this.Seeds = model.SeedInfos.ToReadOnlyReactiveCollection(n => new SeedViewModel(this, n)).AddTo(_disposable);
            this.Boxes = model.BoxInfos.ToReadOnlyReactiveCollection(n => new BoxViewModel(this, n)).AddTo(_disposable);
        }

        public override string DragFormat { get { return "Store"; } }

        public override bool TryAdd(object value)
        {
            if (value is BoxViewModel)
            {
                this.Model.BoxInfos.Add(((BoxViewModel)value).Model);
                return true;
            }

            return false;
        }

        public override bool TryRemove(object value)
        {
            if (value is BoxViewModel)
            {
                return this.Model.BoxInfos.Remove(((BoxViewModel)value).Model);
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
