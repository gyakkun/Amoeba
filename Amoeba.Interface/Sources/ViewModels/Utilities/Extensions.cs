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

namespace Amoeba.Interface
{
    static class ProcessExtensions
    {
        [DllImport("ntdll.dll")]
        private static extern uint NtSetInformationProcess(IntPtr processHandle, uint processInformationClass, ref uint processInformation, uint processInformationLength);

        private const uint ProcessInformationMemoryPriority = 0x27;

        public static void SetMemoryPriority(this Process process, int priority)
        {
            uint memoryPriority = (uint)priority;
            ProcessExtensions.NtSetInformationProcess(process.Handle, ProcessExtensions.ProcessInformationMemoryPriority, ref memoryPriority, sizeof(uint));
        }
    }

    static class UIElementExtensions
    {
        public static TAncestor FindAncestor<TAncestor>(this UIElement element)
            where TAncestor : class
        {
            var temp = element;

            while ((temp != null) && !(temp is TAncestor))
            {
                temp = VisualTreeHelper.GetParent(temp) as UIElement;
            }

            return temp as TAncestor;
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
                    var item = ItemsControlExtensions.GetItemsControlItem(thisItemsControl, i);

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

            var bounds = VisualTreeHelper.GetDescendantBounds(target);
            var mousePos = MouseUtils.GetMousePosition(target);
            return bounds.Contains(mousePos);
        }
    }

    static class ItemCollectionExtensions
    {
        public static void AddRange(this ItemCollection itemCollection, IEnumerable<object> collection)
        {
            foreach (object item in collection)
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
    class MouseUtils
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public int X;
            public int Y;
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
            var mouse = new Win32Point();
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
                var child = VisualTreeHelper.GetChild(control, i);

                if (IsItemsHostPanel(generator, child))
                {
                    return (Panel)child;
                }

                var panel = Find(generator, child);

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

        public static DependencyObject SearchContainerFromElement(this ItemsControl itemsControl, DependencyObject buttomControl)
        {
            var target = itemsControl;

            for (;;)
            {
                var temp = ItemsControl.ContainerFromElement(target, buttomControl) as ItemsControl;
                if (temp == null) break;

                target = temp;
            }

            return target;
        }

        public static object SearchItemFromElement(this ItemsControl itemsControl, DependencyObject buttomControl)
        {
            object parent = null;
            var target = itemsControl;

            for (;;)
            {
                var temp = ItemsControl.ContainerFromElement(target, buttomControl) as ItemsControl;
                if (temp == null) break;

                parent = target.ItemContainerGenerator.ItemFromContainer(temp);
                target = temp;
            }

            return parent;
        }

        public static T GetItem<T>(this ItemsControl itemsControl, string name)
            where T : ItemsControl
        {
            var items = new List<T>();
            items.AddRange(itemsControl.Items.OfType<T>());

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Name == name)
                {
                    return items[i];
                }

                items.AddRange(items[i].Items.OfType<T>());
            }

            return null;
        }
    }
}
