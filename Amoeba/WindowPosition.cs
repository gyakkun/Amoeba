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
            foreach (var n in System.Windows.Forms.Screen.AllScreens)
            {
                if (n.WorkingArea.Left <= (window.Left + (window.Width / 2)) && (window.Left + (window.Width / 2)) <= (n.WorkingArea.Left + n.WorkingArea.Width)
                    && n.WorkingArea.Top <= window.Top && window.Top <= (n.WorkingArea.Top + n.WorkingArea.Height))
                {
                    var maxLeft = n.WorkingArea.Left;
                    var maxTop = n.WorkingArea.Top;
                    var maxRight = (n.WorkingArea.Left + n.WorkingArea.Width) - window.Width;
                    var maxBottom = (n.WorkingArea.Top + n.WorkingArea.Height) - window.Height;

                    window.Left = Math.Min(Math.Max(maxLeft, window.Left), maxRight);
                    window.Top = Math.Min(Math.Max(maxTop, window.Top), maxBottom);

                    return;
                }
            }

            window.Top = 0;
            window.Left = 0;
        }
    }
}
