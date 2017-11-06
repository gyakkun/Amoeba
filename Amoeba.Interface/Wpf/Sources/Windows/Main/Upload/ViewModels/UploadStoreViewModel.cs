using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class UploadStoreViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ReactiveProperty<bool> IsUpdated { get; private set; }
        public ReadOnlyReactiveCollection<UploadDirectoryViewModel> DirectoryViewModels { get; private set; }
        public ReadOnlyReactiveCollection<UploadCategoryViewModel> CategoryViewModels { get; private set; }

        public UploadStoreInfo Model { get; private set; }

        public UploadStoreViewModel(TreeViewModelBase parent, UploadStoreInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = SettingsManager.Instance.AccountInfo.ObserveProperty(n => n.DigitalSignature).Select(n => n.ToString()).ToReactiveProperty().AddTo(_disposable);
            this.IsSelected = new ReactiveProperty<bool>().AddTo(_disposable);
            this.IsExpanded = model.ToReactivePropertyAsSynchronized(n => n.IsExpanded).AddTo(_disposable);
            this.IsUpdated = model.ToReactivePropertyAsSynchronized(n => n.IsUpdated).AddTo(_disposable);
            this.DirectoryViewModels = model.DirectoryInfos.ToReadOnlyReactiveCollection(n => new UploadDirectoryViewModel(this, n)).AddTo(_disposable);
            this.CategoryViewModels = model.CategoryInfos.ToReadOnlyReactiveCollection(n => new UploadCategoryViewModel(this, n)).AddTo(_disposable);
        }

        public override string DragFormat { get { return null; } }

        public override bool TryAdd(object value)
        {
            if (value is UploadCategoryViewModel categoryViewModel)
            {
                this.Model.CategoryInfos.Add(categoryViewModel.Model);
                return true;
            }
            else if (value is UploadDirectoryViewModel directoryViewModel)
            {
                this.Model.DirectoryInfos.Add(directoryViewModel.Model);
                return true;
            }

            return false;
        }

        public override bool TryRemove(object value)
        {
            if (value is UploadCategoryViewModel categoryViewModel)
            {
                return this.Model.CategoryInfos.Remove(categoryViewModel.Model);
            }
            else if (value is UploadDirectoryViewModel directoryViewModel)
            {
                return this.Model.DirectoryInfos.Remove(directoryViewModel.Model);
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
