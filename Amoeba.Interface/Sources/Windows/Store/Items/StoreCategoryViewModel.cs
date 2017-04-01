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
	class StoreCategoryViewModel : TreeViewModelBase
	{
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ReadOnlyReactiveCollection<StoreViewModel> Stores { get; private set; }
		public ReadOnlyReactiveCollection<StoreCategoryViewModel> Categories { get; private set; }

		public StoreCategoryInfo Model { get; private set; }

		public StoreCategoryViewModel(TreeViewModelBase parent, StoreCategoryInfo model)
			: base(parent)
		{
			this.Model = model;

			this.Name = model.ToReactivePropertyAsSynchronized(n => n.Name).AddTo(_disposable);
			this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
			this.Stores = model.StoreInfos.ToReadOnlyReactiveCollection(n => new StoreViewModel(this, n)).AddTo(_disposable);
			this.Categories = model.CategoryInfos.ToReadOnlyReactiveCollection(n => new StoreCategoryViewModel(this, n)).AddTo(_disposable);
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

		public override void Dispose()
		{
			if (_disposed) return;
			_disposed = true;

			_disposable.Dispose();
		}
	}
}
