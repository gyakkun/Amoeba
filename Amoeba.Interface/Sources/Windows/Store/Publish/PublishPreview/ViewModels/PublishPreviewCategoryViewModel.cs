using System.Reactive.Disposables;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class PublishPreviewCategoryViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ReadOnlyReactiveCollection<PublishPreviewCategoryViewModel> Categories { get; private set; }

        public PublishPreviewCategoryInfo Model { get; private set; }

        public PublishPreviewCategoryViewModel(TreeViewModelBase parent, PublishPreviewCategoryInfo model)
            : base(parent)
        {
            this.Model = model;

            this.IsExpanded = new ReactiveProperty<bool>(true).AddTo(_disposable);

            this.Name = model.ToReactivePropertyAsSynchronized(n => n.Name).AddTo(_disposable);
            this.Categories = model.CategoryInfos.ToReadOnlyReactiveCollection(n => new PublishPreviewCategoryViewModel(this, n)).AddTo(_disposable);
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
