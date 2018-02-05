using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Omnius.Base;

namespace Amoeba.Interface
{
    static partial class Clipboard
    {
        // https://stackoverflow.com/questions/621577/clipboard-event-c-sharp
        class Monitor : ManagerBase
        {
            private HwndSource _hwndSource;

            public Monitor(Window windowSource)
            {
                _hwndSource = PresentationSource.FromVisual(windowSource) as HwndSource;

                if (_hwndSource == null)
                {
                    throw new ArgumentException(
                        "Window source MUST be initialized first, such as in the Window's OnSourceInitialized handler."
                        , nameof(windowSource));
                }

                _hwndSource.AddHook(this.WndProc);

                // get window handle for interop
                var windowHandle = new WindowInteropHelper(windowSource).Handle;

                // register for clipboard events
                NativeMethods.AddClipboardFormatListener(windowHandle);
            }

            private static class NativeMethods
            {
                // See http://msdn.microsoft.com/en-us/library/ms649021%28v=vs.85%29.aspx
                public const int WM_CLIPBOARDUPDATE = 0x031D;
                public static IntPtr HWND_MESSAGE = new IntPtr(-3);

                // See http://msdn.microsoft.com/en-us/library/ms632599%28VS.85%29.aspx#message_only
                [DllImport("user32.dll", SetLastError = true)]
                [return: MarshalAs(UnmanagedType.Bool)]
                public static extern bool AddClipboardFormatListener(IntPtr hwnd);
            }

            public event EventHandler ClipboardChanged;

            private void OnClipboardChanged()
            {
                ClipboardChanged?.Invoke(this, EventArgs.Empty);
            }

            private static readonly IntPtr _wndProcSuccess = IntPtr.Zero;

            private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
                {
                    OnClipboardChanged();
                    handled = true;
                }

                return _wndProcSuccess;
            }

            protected override void Dispose(bool isDisposing)
            {
                _hwndSource.RemoveHook(this.WndProc);
                _hwndSource.Dispose();
            }
        }
    }
}
