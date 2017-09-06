using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Amoeba.Interface
{
    class IconUtils
    {
        private static Dictionary<string, BitmapSource> _map = new Dictionary<string, BitmapSource>();

        public static BitmapSource GetImage(string path)
        {
            try
            {
                string ext = Path.GetExtension(path);
                if (string.IsNullOrWhiteSpace(ext)) return null;

                BitmapSource icon;

                if (!_map.TryGetValue(ext, out icon))
                {
                    icon = NativeMethods.FileAssociatedImage(ext, false, false);
                    if (icon.CanFreeze) icon.Freeze();

                    _map[ext] = icon;
                }

                return icon;
            }
            catch (Exception)
            {

            }

            return null;
        }

        static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            struct SHFILEINFO
            {
                public IntPtr hIcon;
                public IntPtr iIcon;
                public uint dwAttributes;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                public string szDisplayName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
                public string szTypeName;
            }

            const uint SHGFI_LARGEICON = 0x00000000;
            const uint SHGFI_SMALLICON = 0x00000001;
            const uint SHGFI_USEFILEATTRIBUTES = 0x00000010;
            const uint SHGFI_ICON = 0x00000100;

            [DllImport("shell32.dll", CharSet = CharSet.Auto)]
            static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool DestroyIcon(IntPtr hIcon);

            public static BitmapSource FileAssociatedImage(string path, bool isLarge, bool isExist)
            {
                var fileInfo = new SHFILEINFO();
                uint flags = SHGFI_ICON;
                if (!isLarge) flags |= SHGFI_SMALLICON;
                if (!isExist) flags |= SHGFI_USEFILEATTRIBUTES;

                try
                {
                    SHGetFileInfo(path, 0, ref fileInfo, (uint)Marshal.SizeOf(fileInfo), flags);

                    if (fileInfo.hIcon == IntPtr.Zero)
                    {
                        return null;
                    }
                    else
                    {
                        return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(fileInfo.hIcon, new Int32Rect(0, 0, 16, 16), BitmapSizeOptions.FromEmptyOptions());
                    }
                }
                finally
                {
                    if (fileInfo.hIcon != IntPtr.Zero)
                    {
                        DestroyIcon(fileInfo.hIcon);
                    }
                }
            }
        }
    }
}