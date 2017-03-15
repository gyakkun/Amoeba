using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Reactive.Bindings;

namespace Amoeba.Interface
{
    class StoreControlViewModel
    {
        private CollectionViewSource _treeViewSource;

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
            this.DragAcceptDescription.DragOver += this.OnDragOver;
            this.DragAcceptDescription.DragDrop += this.OnDragDrop;
        }

        public ICollectionView TreeView
        {
            get
            {
                return _treeViewSource.View;
            }
        }

        public DragAcceptDescription DragAcceptDescription { get; private set; }

        private void OnDragOver(DragEventArgs args)
        {
            if (args.AllowedEffects.HasFlag(DragDropEffects.Move))
            {
                args.Effects = DragDropEffects.Move;
            }
        }

        void OnDragDrop(DragEventArgs args)
        {
            var target = this.GetDropDestination((UIElement)args.OriginalSource);
            if (target == null) return;

            if (args.Data.GetDataPresent("Store"))
            {
                var source = args.Data.GetData("Store") as TreeViewModelBase;
                if (source == null) return;

                if (target.GetAncestors().Contains(source)) return;

                if (target.TryAdd(source))
                {
                    source.Parent.TryRemove(source);
                }
            }
        }

        private TreeViewModelBase GetDropDestination(UIElement originalSource)
        {
            var element = originalSource.FindAncestor<TreeViewItem>();
            if (element == null) return null;

            return element.DataContext as TreeViewModelBase;
        }
    }
}
