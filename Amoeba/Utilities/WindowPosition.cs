using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Amoeba
{
    static class WindowPosition
    {
        public static void Move(Window window)
        {
            if (window.WindowState != WindowState.Normal) return;

            var n = System.Windows.Forms.Screen.AllScreens
                .OrderBy(m =>
                {
                    return Math.Abs((m.WorkingArea.Left + (m.WorkingArea.Width / 2)) - (window.Left + (window.ActualWidth / 2)))
                    + Math.Abs((m.WorkingArea.Top + (m.WorkingArea.Height / 2)) - (window.Top + (window.ActualHeight / 2)));
                })
                .First();

            {
                var maxLeft = n.WorkingArea.Left;
                var maxTop = n.WorkingArea.Top;
                var maxRight = (n.WorkingArea.Left + n.WorkingArea.Width) - window.ActualWidth;
                var maxBottom = (n.WorkingArea.Top + n.WorkingArea.Height) - window.ActualHeight;

                window.Left = Math.Min(Math.Max(maxLeft, window.Left), maxRight);
                window.Top = Math.Min(Math.Max(maxTop, window.Top), maxBottom);

                return;
            }
        }
    }
}
