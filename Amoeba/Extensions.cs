using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Amoeba
{
    static class ListViewExtensions
    {
        public delegate Point GetPositionDelegate(IInputElement element);

        public static int GetCurrentIndex(this ListView myListView, GetPositionDelegate getPosition)
        {
            int index = -1;

            for (int i = 0; i < myListView.Items.Count; i++)
            {
                ListViewItem item = ListViewExtensions.GetListViewItem(myListView, i);

                if (ListViewExtensions.IsMouseOverTarget(myListView, item, getPosition))
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        private static ListViewItem GetListViewItem(ListView myListView, int index)
        {
            if (myListView.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                return null;

            return myListView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
        }

        private static bool IsMouseOverTarget(ListView myListView, Visual target, GetPositionDelegate getPosition)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = getPosition((IInputElement)target);
            return bounds.Contains(mousePos);
        }
    }

    static class TreeViewItemExtensions
    {
        public static IEnumerable<TreeViewItem> GetLineage(this TreeViewItem parentItem, TreeViewItem childItem)
        {
            var items = new List<TreeViewItem>();
            items.Add(parentItem);

            for (int i = 0; i < items.Count; i++)
            {
                foreach (TreeViewItem item in items[i].Items)
                {
                    if (childItem.IsDescendantOf(item))
                    {
                        items.Add(item);
                    }
                }
            }

            return items;
        }
    }
}
