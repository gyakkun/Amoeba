using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class SubscribeBoxViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ReadOnlyReactiveCollection<Seed> Seeds { get; private set; }
        public ReadOnlyReactiveCollection<SubscribeBoxViewModel> Boxes { get; private set; }

        public SubscribeBoxInfo Model { get; private set; }

        public SubscribeBoxViewModel(TreeViewModelBase parent, SubscribeBoxInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ToReactivePropertyAsSynchronized(n => n.Name).AddTo(_disposable);
            this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
            this.Seeds = model.Seeds.ToReadOnlyReactiveCollection(n => n).AddTo(_disposable);
            this.Boxes = model.BoxInfos.ToReadOnlyReactiveCollection(n => new SubscribeBoxViewModel(this, n)).AddTo(_disposable);
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
