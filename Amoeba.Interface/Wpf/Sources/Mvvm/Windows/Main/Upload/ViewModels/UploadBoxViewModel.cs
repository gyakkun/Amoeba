using System.Reactive.Disposables;
using Amoeba.Messages;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class UploadBoxViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public ReadOnlyReactiveCollection<Seed> Seeds { get; private set; }
        public ReadOnlyReactiveCollection<UploadBoxViewModel> BoxViewModels { get; private set; }

        public UploadBoxInfo Model { get; private set; }

        public UploadBoxViewModel(TreeViewModelBase parent, UploadBoxInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ToReactivePropertyAsSynchronized(n => n.Name).AddTo(_disposable);
            this.IsSelected = new ReactiveProperty<bool>().AddTo(_disposable);
            this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
            this.Seeds = model.Seeds.ToReadOnlyReactiveCollection(n => n).AddTo(_disposable);
            this.BoxViewModels = model.BoxInfos.ToReadOnlyReactiveCollection(n => new UploadBoxViewModel(this, n)).AddTo(_disposable);
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
