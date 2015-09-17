using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Amoeba.Windows;

namespace Amoeba
{
    static class ProcessExtensions
    {
        [DllImport("ntdll.dll")]
        private static extern uint NtSetInformationProcess(IntPtr hProcess, uint processInformationClass, ref uint processInformation, uint processInformationLength);

        const uint ProcessInformationMemoryPriority = 0x27;
        const uint ProcessInformationIoPriority = 0x21;

        public static void SetMemoryPriority(this Process process, int priority)
        {
            uint memoryPriority = (uint)priority;
            ProcessExtensions.NtSetInformationProcess(process.Handle, ProcessExtensions.ProcessInformationMemoryPriority, ref memoryPriority, sizeof(uint));
        }

        public static void SetIoPriority(this Process process, int priority)
        {
            uint ioPriority = (uint)priority;
            ProcessExtensions.NtSetInformationProcess(process.Handle, ProcessExtensions.ProcessInformationIoPriority, ref ioPriority, sizeof(uint));
        }
    }

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

    delegate Point GetPositionDelegate(IInputElement element);

    static class ItemsControlExtensions
    {
        public static int GetCurrentIndex(this ItemsControl thisItemsControl, GetPositionDelegate getPosition)
        {
            try
            {
                if (!ItemsControlExtensions.IsMouseOverTarget(thisItemsControl, getPosition)) return -1;

                for (int i = 0; i < thisItemsControl.Items.Count; i++)
                {
                    Visual item = ItemsControlExtensions.GetItemsControlItem(thisItemsControl, i);

                    if (ItemsControlExtensions.IsMouseOverTarget(item, getPosition))
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

        private static Visual GetItemsControlItem(ItemsControl thisItemsControl, int index)
        {
            if (thisItemsControl.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                return null;

            return thisItemsControl.ItemContainerGenerator.ContainerFromIndex(index) as Visual;
        }

        private static bool IsMouseOverTarget(Visual target, GetPositionDelegate getPosition)
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
                if (!TreeViewExtensions.IsMouseOverTarget(thisTreeView, getPosition)) return null;

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
                    if (TreeViewExtensions.IsMouseOverTarget(item, getPosition))
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

        private static bool IsMouseOverTarget(Visual target, GetPositionDelegate getPosition)
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

    static class TreeViewItemExtensions
    {
        public static IEnumerable<TreeViewItem> GetAncestors(this TreeViewItem parentItem, TreeViewItem childItem)
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
                list.AddRange(parentItem.Items.Cast<TreeViewItem>());

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

                        if (parentItem.Items.Contains(current)) break;
                    }
                }

                return targetList;
            }
        }
    }

    static class ItemCollectionExtensions
    {
        public static void AddRange(this ItemCollection itemCollection, IEnumerable<object> collection)
        {
            foreach (var item in collection)
            {
                itemCollection.Add(item);
            }
        }
    }

    // http://geekswithblogs.net/sonam/archive/2009/03/02/listview-dragdrop-in-wpfmultiselect.aspx

    /// <summary>
    /// Provides access to the mouse location by calling unmanaged code.
    /// </summary>
    /// <remarks>
    /// This class was written by Dan Crevier (Microsoft). 
    /// http://blogs.msdn.com/llobo/archive/2006/09/06/Scrolling-Scrollviewer-on-Mouse-Drag-at-the-boundaries.aspx
    /// </remarks>
    class MouseUtilities
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

    // http://pro.art55.jp/?eid=1160884
    static class ItemsControlUtilities
    {
        public static void GoBottom(this ItemsControl itemsControl)
        {
            try
            {
                var panel = itemsControl.FindItemsHostPanel() as IScrollInfo;
                if (panel == null) return;

                panel.SetVerticalOffset(double.PositiveInfinity);
            }
            catch (Exception)
            {

            }
        }

        public static void GoTop(this ItemsControl itemsControl)
        {
            try
            {
                var panel = itemsControl.FindItemsHostPanel() as IScrollInfo;
                if (panel == null) return;

                panel.SetVerticalOffset(0);
            }
            catch (Exception)
            {

            }
        }

        public static void GoRight(this ItemsControl itemsControl)
        {
            try
            {
                var panel = itemsControl.FindItemsHostPanel() as IScrollInfo;
                if (panel == null) return;

                panel.SetHorizontalOffset(double.PositiveInfinity);
            }
            catch (Exception)
            {

            }
        }

        public static void GoLeft(this ItemsControl itemsControl)
        {
            try
            {
                var panel = itemsControl.FindItemsHostPanel() as IScrollInfo;
                if (panel == null) return;

                panel.SetHorizontalOffset(0);
            }
            catch (Exception)
            {

            }
        }

        public static void PageDown(this ItemsControl itemsControl)
        {
            try
            {
                var panel = itemsControl.FindItemsHostPanel() as IScrollInfo;
                if (panel == null) return;

                panel.PageDown();
            }
            catch (Exception)
            {

            }
        }

        public static void PageUp(this ItemsControl itemsControl)
        {
            try
            {
                var panel = itemsControl.FindItemsHostPanel() as IScrollInfo;
                if (panel == null) return;

                panel.PageUp();
            }
            catch (Exception)
            {

            }
        }

        public static void PageRight(this ItemsControl itemsControl)
        {
            try
            {
                var panel = itemsControl.FindItemsHostPanel() as IScrollInfo;
                if (panel == null) return;

                panel.PageRight();
            }
            catch (Exception)
            {

            }
        }

        public static void PageLeft(this ItemsControl itemsControl)
        {
            try
            {
                var panel = itemsControl.FindItemsHostPanel() as IScrollInfo;
                if (panel == null) return;

                panel.PageLeft();
            }
            catch (Exception)
            {

            }
        }

        public static void LineDown(this ItemsControl itemsControl)
        {
            try
            {
                var panel = itemsControl.FindItemsHostPanel() as IScrollInfo;
                if (panel == null) return;

                panel.LineDown();
            }
            catch (Exception)
            {

            }
        }

        public static void LineUp(this ItemsControl itemsControl)
        {
            try
            {
                var panel = itemsControl.FindItemsHostPanel() as IScrollInfo;
                if (panel == null) return;

                panel.LineUp();
            }
            catch (Exception)
            {

            }
        }

        public static void LineRight(this ItemsControl itemsControl)
        {
            try
            {
                var panel = itemsControl.FindItemsHostPanel() as IScrollInfo;
                if (panel == null) return;

                panel.LineRight();
            }
            catch (Exception)
            {

            }
        }

        public static void LineLeft(this ItemsControl itemsControl)
        {
            try
            {
                var panel = itemsControl.FindItemsHostPanel() as IScrollInfo;
                if (panel == null) return;

                panel.LineLeft();
            }
            catch (Exception)
            {

            }
        }

        private static Panel FindItemsHostPanel(this ItemsControl itemsControl)
        {
            return Find(itemsControl.ItemContainerGenerator, itemsControl);
        }

        private static Panel Find(this IItemContainerGenerator generator, DependencyObject control)
        {
            int count = VisualTreeHelper.GetChildrenCount(control);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(control, i);

                if (IsItemsHostPanel(generator, child))
                {
                    return (Panel)child;
                }

                Panel panel = Find(generator, child);

                if (panel != null)
                {
                    return panel;
                }
            }

            return null;
        }

        private static bool IsItemsHostPanel(IItemContainerGenerator generator, DependencyObject target)
        {
            var panel = target as Panel;
            return panel != null && panel.IsItemsHost && generator == generator.GetItemContainerGeneratorForPanel(panel);
        }
    }
}
