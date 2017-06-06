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
    class PublishPreviewBoxViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ReadOnlyReactiveCollection<PublishPreviewBoxViewModel> Boxes { get; private set; }

        public PublishPreviewBoxInfo Model { get; private set; }

        public PublishPreviewBoxViewModel(TreeViewModelBase parent, PublishPreviewBoxInfo model)
            : base(parent)
        {
            this.Model = model;

            this.IsExpanded = new ReactiveProperty<bool>(true).AddTo(_disposable);

            this.Name = model.ToReactivePropertyAsSynchronized(n => n.Name).AddTo(_disposable);
            this.Boxes = model.BoxInfos.ToReadOnlyReactiveCollection(n => new PublishPreviewBoxViewModel(this, n)).AddTo(_disposable);
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
