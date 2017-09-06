using System.Reactive.Disposables;
using Amoeba.Messages;
using Amoeba.Service;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class PublishBoxViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ReadOnlyReactiveCollection<Seed> Seeds { get; private set; }
        public ReadOnlyReactiveCollection<PublishBoxViewModel> Boxes { get; private set; }

        public PublishBoxInfo Model { get; private set; }

        public PublishBoxViewModel(TreeViewModelBase parent, PublishBoxInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ToReactivePropertyAsSynchronized(n => n.Name).AddTo(_disposable);
            this.IsSelected = new ReactiveProperty<bool>().AddTo(_disposable);
            this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
            this.Seeds = model.Seeds.ToReadOnlyReactiveCollection(n => n).AddTo(_disposable);
            this.Boxes = model.BoxInfos.ToReadOnlyReactiveCollection(n => new PublishBoxViewModel(this, n)).AddTo(_disposable);
        }

        public override string DragFormat { get { return "Amoeba_Publish"; } }

        public override bool TryAdd(object value)
        {
            if (value is PublishBoxViewModel boxViewModel)
            {
                this.Model.BoxInfos.Add(boxViewModel.Model);
                return true;
            }

            return false;
        }

        public override bool TryRemove(object value)
        {
            if (value is PublishBoxViewModel boxViewModel)
            {
                return this.Model.BoxInfos.Remove(boxViewModel.Model);
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
