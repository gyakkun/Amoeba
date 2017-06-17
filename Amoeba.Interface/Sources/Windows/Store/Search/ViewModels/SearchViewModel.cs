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
    class SearchViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ReadOnlyReactiveCollection<SearchViewModel> Children { get; private set; }
        public ReactiveProperty<bool> IsUpdate { get; private set; }
        public ReactiveProperty<int> Count { get; private set; }

        public SearchInfo Model { get; private set; }

        public SearchViewModel(TreeViewModelBase parent, SearchInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ObserveProperty(n => n.Name).ToReactiveProperty().AddTo(_disposable);
            this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
            this.Children = model.Children.ToReadOnlyReactiveCollection(n => new SearchViewModel(this, n)).AddTo(_disposable);
            this.IsUpdate = model.ToReactivePropertyAsSynchronized(n => n.IsUpdated).AddTo(_disposable);
            this.Count = new ReactiveProperty<int>().AddTo(_disposable);
        }

        public override string DragFormat { get { return "Search"; } }

        public override bool TryAdd(object value)
        {
            if (value is SearchViewModel viewModel)
            {
                this.Model.Children.Add(viewModel.Model);
                return true;
            }

            return false;
        }

        public override bool TryRemove(object value)
        {
            if (value is SearchViewModel viewModel)
            {
                return this.Model.Children.Remove(viewModel.Model);
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
