using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Amoeba.Service;

namespace Amoeba.Interface
{
    class SeedViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ReactiveProperty<long> Length { get; private set; }
        public ReactiveProperty<DateTime> CreationTime { get; private set; }
        public ReactiveProperty<Metadata> Metadata { get; private set; }

        public SeedInfo Model { get; private set; }

        public SeedViewModel(TreeViewModelBase parent, SeedInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ObserveProperty(n => n.Name).ToReactiveProperty().AddTo(_disposable);
            this.Length = model.ObserveProperty(n => n.Length).ToReactiveProperty().AddTo(_disposable);
            this.CreationTime = model.ObserveProperty(n => n.CreationTime).ToReactiveProperty().AddTo(_disposable);
            this.Metadata = model.ObserveProperty(n => n.Metadata).ToReactiveProperty().AddTo(_disposable);
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

        public override void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _disposable.Dispose();
        }
    }
}
