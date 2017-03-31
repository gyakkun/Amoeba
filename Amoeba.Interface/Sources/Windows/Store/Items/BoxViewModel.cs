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
using Omnius.Wpf;
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

			this.Name = model.ObserveProperty(n => n.Name).ToReactiveProperty().AddTo(_disposable);
			this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
			this.Seeds = model.SeedInfos.ToReadOnlyReactiveCollection(n => new SeedViewModel(this, n)).AddTo(_disposable);
			this.Boxes = model.BoxInfos.ToReadOnlyReactiveCollection(n => new BoxViewModel(this, n)).AddTo(_disposable);
		}

		public override string DragFormat { get { return "Store"; } }

		public override bool TryAdd(object value)
		{
			if (value is BoxViewModel viewModel)
			{
				this.Model.BoxInfos.Add(viewModel.Model);
				return true;
			}

			return false;
		}

		public override bool TryRemove(object value)
		{
			if (value is BoxViewModel viewModel)
			{
				return this.Model.BoxInfos.Remove(viewModel.Model);
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
