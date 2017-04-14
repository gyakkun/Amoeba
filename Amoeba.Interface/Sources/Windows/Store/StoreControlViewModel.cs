using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Omnius.Base;
using Omnius.Wpf;

namespace Amoeba.Interface
{
    class StoreControlViewModel : ManagerBase
    {
        private CollectionViewSource _treeViewSource;
        public DragAcceptDescription DragAcceptDescription { get; private set; }

        private volatile bool _disposed;

        public StoreControlViewModel()
        {
            _treeViewSource = new CollectionViewSource();

            {
                var boxInfo = new BoxInfo() { Name = "bbbbbbbbbbbb" };
                boxInfo.BoxInfos.Add(new BoxInfo() { Name = "aaaaaaaa4" });
                boxInfo.BoxInfos.Add(new BoxInfo() { Name = "aaaaaaaa3" });
                boxInfo.BoxInfos.Add(new BoxInfo() { Name = "aaaaaaaa2" });
                boxInfo.BoxInfos.Add(new BoxInfo() { Name = "aaaaaaaa1" });
                boxInfo.SeedInfos.Add(new SeedInfo() { Name = "sssssssssss" });

                _treeViewSource.Source = new BoxViewModel[] { new BoxViewModel(null, boxInfo) };
            }

            this.DragAcceptDescription = new DragAcceptDescription();
            this.DragAcceptDescription.Effects = DragDropEffects.Move;
            this.DragAcceptDescription.Format = "Store";
            this.DragAcceptDescription.DragDrop += this.OnDragDrop;
        }

        public ICollectionView TreeView
        {
            get
            {
                return _treeViewSource.View;
            }
        }

        void OnDragDrop(DragAcceptEventArgs args)
        {
            var source = args.Source as TreeViewModelBase;
            var dest = args.Destination as TreeViewModelBase;
            if (source == null || dest == null) return;

            if (dest.GetAncestors().Contains(source)) return;

            if (dest.TryAdd(source))
            {
                source.Parent.TryRemove(source);
            }
        }

        protected override void Dispose(bool disposing)
        {

        }
    }
}
