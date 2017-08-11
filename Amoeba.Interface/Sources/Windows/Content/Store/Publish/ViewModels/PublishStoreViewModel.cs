using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class PublishStoreViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ReactiveProperty<bool> IsUpdated { get; private set; }
        public ReadOnlyReactiveCollection<PublishBoxViewModel> Boxes { get; private set; }

        public PublishStoreInfo Model { get; private set; }

        public PublishStoreViewModel(TreeViewModelBase parent, PublishStoreInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = SettingsManager.Instance.AccountInfo.ObserveProperty(n => n.DigitalSignature).Select(n => n.ToString()).ToReactiveProperty().AddTo(_disposable);
            this.IsSelected = new ReactiveProperty<bool>().AddTo(_disposable);
            this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
            this.IsUpdated = model.ToReactivePropertyAsSynchronized(n => n.IsUpdated).AddTo(_disposable);
            this.Boxes = model.BoxInfos.ToReadOnlyReactiveCollection(n => new PublishBoxViewModel(this, n)).AddTo(_disposable);
        }

        public override string DragFormat { get { return null; } }

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
