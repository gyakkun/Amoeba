using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class UploadStoreCategoryViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ReadOnlyReactiveCollection<UploadStoreViewModel> Stores { get; private set; }
        public ReadOnlyReactiveCollection<UploadStoreCategoryViewModel> Categories { get; private set; }

        public UploadStoreCategoryInfo Model { get; private set; }

        public UploadStoreCategoryViewModel(TreeViewModelBase parent, UploadStoreCategoryInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ToReactivePropertyAsSynchronized(n => n.Name).AddTo(_disposable);
            this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
            this.Stores = model.StoreInfos.ToReadOnlyReactiveCollection(n => new UploadStoreViewModel(this, n)).AddTo(_disposable);
            this.Categories = model.CategoryInfos.ToReadOnlyReactiveCollection(n => new UploadStoreCategoryViewModel(this, n)).AddTo(_disposable);
        }

        public override string DragFormat { get { return "Store"; } }

        public override bool TryAdd(object value)
        {
            if (value is UploadStoreCategoryViewModel categoryViewModel)
            {
                this.Model.CategoryInfos.Add(categoryViewModel.Model);
                return true;
            }
            else if (value is UploadStoreViewModel storeViewModel)
            {
                this.Model.StoreInfos.Add(storeViewModel.Model);
                return true;
            }

            return false;
        }

        public override bool TryRemove(object value)
        {
            if (value is UploadStoreCategoryViewModel categoryViewModel)
            {
                return this.Model.CategoryInfos.Remove(categoryViewModel.Model);
            }
            else if (value is UploadStoreViewModel storeViewModel)
            {
                return this.Model.StoreInfos.Remove(storeViewModel.Model);
            }

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
