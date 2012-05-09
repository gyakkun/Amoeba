using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Amoeba
{
    static class ListViewExtensions
    {
        public delegate Point GetPositionDelegate(IInputElement element);

        public static int GetCurrentIndex(this ListView myListView, GetPositionDelegate getPosition)
        {
            try
            {
                for (int i = 0; i < myListView.Items.Count; i++)
                {
                    ListViewItem item = ListViewExtensions.GetListViewItem(myListView, i);

                    if (ListViewExtensions.IsMouseOverTarget(myListView, item, getPosition))
                    {
                        return i;
                    }
                }
            }
            catch (Exception)
            {

            }

            return -1;
        }

        private static ListViewItem GetListViewItem(ListView myListView, int index)
        {
            if (myListView.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                return null;

            return myListView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
        }

        private static bool IsMouseOverTarget(ListView myListView, Visual target, GetPositionDelegate getPosition)
        {
            if (target == null) return false;

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = MouseUtilities.GetMousePosition(target);
            return bounds.Contains(mousePos);
        }
    }

    static class ListBoxExtensions
    {
        public delegate Point GetPositionDelegate(IInputElement element);

        public static int GetCurrentIndex(this ListBox myListBox, GetPositionDelegate getPosition)
        {
            try
            {
                for (int i = 0; i < myListBox.Items.Count; i++)
                {
                    ListBoxItem item = ListBoxExtensions.GetListBoxItem(myListBox, i);

                    if (ListBoxExtensions.IsMouseOverTarget(myListBox, item, getPosition))
                    {
                        return i;
                    }
                }
            }
            catch (Exception)
            {

            }

            return -1;
        }

        private static ListBoxItem GetListBoxItem(ListBox myListBox, int index)
        {
            if (myListBox.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                return null;

            return myListBox.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
        }

        private static bool IsMouseOverTarget(ListBox myListBox, Visual target, GetPositionDelegate getPosition)
        {
            if (target == null) return false;

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = MouseUtilities.GetMousePosition(target);
            return bounds.Contains(mousePos);
        }
    }

    static class TreeViewExtensions
    {
        public delegate Point GetPositionDelegate(IInputElement element);

        public static object GetCurrentItem(this TreeView myTreeView, GetPositionDelegate getPosition)
        {
            try
            {
                var items = new List<TreeViewItem>();
                items.AddRange(myTreeView.Items.OfType<TreeViewItem>());

                for (int i = 0; i < items.Count; i++)
                {
                    if (!items[i].IsExpanded) continue;

                    foreach (TreeViewItem item in items[i].Items)
                    {
                        items.Add(item);
                    }
                }

                items.Reverse();

                foreach (var item in items)
                {
                    if (TreeViewExtensions.IsMouseOverTarget(myTreeView, item, getPosition))
                    {
                        return item;
                    }
                }
            }
            catch (Exception)
            {

            }

            return null;
        }

        private static bool IsMouseOverTarget(TreeView myTreeView, Visual target, GetPositionDelegate getPosition)
        {
            if (target == null) return false;
            
            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = MouseUtilities.GetMousePosition(target);
            return bounds.Contains(mousePos);
        }
    }

    static class TreeViewItemExtensions
    {
        public static IEnumerable<TreeViewItem> GetLineage(this TreeViewItem parentItem, TreeViewItem childItem)
        {
            var list = new List<TreeViewItem>();
            list.Add(parentItem);

            for (int i = 0; i < list.Count; i++)
            {
                foreach (TreeViewItem item in list[i].Items)
                {
                    list.Add(item);
                }
            }

            var targetList = new List<TreeViewItem>();
            targetList.Add(childItem);

            try
            {
                for (; ; )
                {
                    var item = targetList.Last();
                    if (item == parentItem) break;

                    targetList.Add(list.First(n => n.Items.Contains(item)));
                }
            }
            catch (Exception)
            {

            }

            targetList.Reverse();

            return targetList;
        }
    }

    //http://geekswithblogs.net/sonam/archive/2009/03/02/listview-dragdrop-in-wpfmultiselect.aspx

    /// <summary>
    /// Provides access to the mouse location by calling unmanaged code.
    /// </summary>
    /// <remarks>
    /// This class was written by Dan Crevier (Microsoft). 
    /// http://blogs.msdn.com/llobo/archive/2006/09/06/Scrolling-Scrollviewer-on-Mouse-Drag-at-the-boundaries.aspx
    /// </remarks>
    public class MouseUtilities
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(ref Win32Point pt);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hwnd, ref Win32Point pt);

        /// <summary>
        /// Returns the mouse cursor location.  This method is necessary during
        /// a drag-drop operation because the WPF mechanisms for retrieving the
        /// cursor coordinates are unreliable.
        /// </summary>
        /// <param name="relativeTo">The Visual to which the mouse coordinates will be relative.</param>
        public static Point GetMousePosition(Visual relativeTo)
        {
            Win32Point mouse = new Win32Point();
            GetCursorPos(ref mouse);
            return relativeTo.PointFromScreen(new Point((double)mouse.X, (double)mouse.Y));
        }
    }
}
