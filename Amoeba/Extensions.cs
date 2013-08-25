using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Amoeba.Windows;

namespace Amoeba
{
    static class ContextMenuExtensions
    {
        public static MenuItem GetMenuItem(this ContextMenu thisContextMenu, string name)
        {
            List<MenuItem> menus = new List<MenuItem>();
            menus.AddRange(thisContextMenu.Items.OfType<MenuItem>());

            for (int i = 0; i < menus.Count; i++)
            {
                if (menus[i].Name == name)
                {
                    return menus[i];
                }

                menus.AddRange(menus[i].Items.OfType<MenuItem>());
            }

            return null;
        }
    }

    public delegate Point GetPositionDelegate(IInputElement element);

    static class ListViewExtensions
    {
        public static int GetCurrentIndex(this ListView thisListView, GetPositionDelegate getPosition)
        {
            try
            {
                for (int i = 0; i < thisListView.Items.Count; i++)
                {
                    ListViewItem item = ListViewExtensions.GetListViewItem(thisListView, i);

                    if (ListViewExtensions.IsMouseOverTarget(thisListView, item, getPosition))
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

        private static ListViewItem GetListViewItem(ListView thisListView, int index)
        {
            if (thisListView.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                return null;

            return thisListView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
        }

        private static bool IsMouseOverTarget(ListView thisListView, Visual target, GetPositionDelegate getPosition)
        {
            if (target == null) return false;

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = MouseUtilities.GetMousePosition(target);
            return bounds.Contains(mousePos);
        }
    }

    static class ListBoxExtensions
    {
        public static int GetCurrentIndex(this ListBox thisListBox, GetPositionDelegate getPosition)
        {
            try
            {
                for (int i = 0; i < thisListBox.Items.Count; i++)
                {
                    ListBoxItem item = ListBoxExtensions.GetListBoxItem(thisListBox, i);

                    if (ListBoxExtensions.IsMouseOverTarget(thisListBox, item, getPosition))
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

        private static ListBoxItem GetListBoxItem(ListBox thisListBox, int index)
        {
            if (thisListBox.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                return null;

            return thisListBox.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
        }

        private static bool IsMouseOverTarget(ListBox thisListBox, Visual target, GetPositionDelegate getPosition)
        {
            if (target == null) return false;

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = MouseUtilities.GetMousePosition(target);
            return bounds.Contains(mousePos);
        }
    }

    static class TreeViewExtensions
    {
        public static object GetCurrentItem(this TreeView thisTreeView, GetPositionDelegate getPosition)
        {
            try
            {
                var items = new List<TreeViewItem>();
                items.AddRange(thisTreeView.Items.OfType<TreeViewItem>());

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
                    if (TreeViewExtensions.IsMouseOverTarget(thisTreeView, item, getPosition))
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

        private static bool IsMouseOverTarget(TreeView thisTreeView, Visual target, GetPositionDelegate getPosition)
        {
            if (target == null) return false;

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = MouseUtilities.GetMousePosition(target);
            return bounds.Contains(mousePos);
        }

        public static IEnumerable<TreeViewItem> GetAncestors(this TreeView parentView, TreeViewItem childItem)
        {
            if (childItem is TreeViewItemEx)
            {
                var targetList = new LinkedList<TreeViewItemEx>();
                targetList.AddFirst((TreeViewItemEx)childItem);

                for (; ; )
                {
                    var parent = targetList.First.Value.Parent;
                    if (parent == null) break;

                    targetList.AddFirst(parent);
                }

                return targetList;
            }
            else
            {
                var list = new List<TreeViewItem>();
                list.AddRange(parentView.Items.Cast<TreeViewItem>());

                for (int i = 0; i < list.Count; i++)
                {
                    foreach (TreeViewItem item in list[i].Items)
                    {
                        list.Add(item);
                    }
                }

                var current = childItem;

                var targetList = new LinkedList<TreeViewItem>();
                targetList.AddFirst(current);

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].Items.Contains(current))
                    {
                        current = list[i];
                        targetList.AddFirst(current);

                        if (parentView.Items.Contains(current)) break;
                    }
                }

                return targetList;
            }
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
