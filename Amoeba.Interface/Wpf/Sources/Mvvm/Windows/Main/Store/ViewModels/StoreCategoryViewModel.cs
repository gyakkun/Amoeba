using System.Reactive.Disposables;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class StoreCategoryViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public ReadOnlyReactiveCollection<StoreSignatureViewModel> SignatureViewModels { get; private set; }
        public ReadOnlyReactiveCollection<StoreCategoryViewModel> CategoryViewModels { get; private set; }

        public StoreCategoryInfo Model { get; private set; }

        public StoreCategoryViewModel(TreeViewModelBase parent, StoreCategoryInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ToReactivePropertyAsSynchronized(n => n.Name).AddTo(_disposable);
            this.IsSelected = new ReactiveProperty<bool>().AddTo(_disposable);
            this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
            this.SignatureViewModels = model.SignatureInfos.ToReadOnlyReactiveCollection(n => new StoreSignatureViewModel(this, n)).AddTo(_disposable);
            this.CategoryViewModels = model.CategoryInfos.ToReadOnlyReactiveCollection(n => new StoreCategoryViewModel(this, n)).AddTo(_disposable);
        }

        public override string DragFormat { get { return "Amoeba_Store"; } }

        public override bool TryAdd(object value)
        {
            if (value is StoreCategoryViewModel categoryViewModel)
            {
                this.Model.CategoryInfos.Add(categoryViewModel.Model);
                return true;
            }
            else if (value is StoreSignatureViewModel signatureViewModel)
            {
                this.Model.SignatureInfos.Add(signatureViewModel.Model);
                return true;
            }

            return false;
        }

        public override bool TryRemove(object value)
        {
            if (value is StoreCategoryViewModel categoryViewModel)
            {
                return this.Model.CategoryInfos.Remove(categoryViewModel.Model);
            }
            else if (value is StoreSignatureViewModel signatureViewModel)
            {
                return this.Model.SignatureInfos.Remove(signatureViewModel.Model);
            }

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
