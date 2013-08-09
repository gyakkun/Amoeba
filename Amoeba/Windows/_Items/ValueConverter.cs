using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Amoeba.Properties;
using Library;
using Library.Net.Amoeba;

namespace Amoeba.Windows
{
    [ValueConversion(typeof(object), typeof(BitmapImage))]
    class ObjectToImageConverter : IValueConverter
    {
        private static BitmapSource _boxIcon;
        private static Dictionary<string, BitmapSource> _icon = new Dictionary<string, BitmapSource>();

        static ObjectToImageConverter()
        {
            var ext = ".box";

            var icon = IconUtilities.FileAssociatedImage(ext, false, false);
            if (icon.CanFreeze) icon.Freeze();

            _boxIcon = icon;
        }

        public class IconUtilities
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
                SHFILEINFO fileInfo = new SHFILEINFO();
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

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            var key = value.GetType().Name.ToString();

            try
            {
                if (value is Seed)
                {
                    Seed seed = (Seed)value;
                    if (string.IsNullOrWhiteSpace(seed.Name)) return null;

                    var ext = Path.GetExtension(seed.Name);
                    if (string.IsNullOrWhiteSpace(ext)) return null;

                    BitmapSource icon;

                    if (!_icon.TryGetValue(ext, out icon))
                    {
                        icon = IconUtilities.FileAssociatedImage(ext, false, false);
                        if (icon.CanFreeze) icon.Freeze();

                        _icon[ext] = icon;
                    }

                    return icon;
                }
                else if (value is Box)
                {
                    return _boxIcon;
                }
            }
            catch (Exception)
            {

            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(bool), typeof(System.Windows.Visibility))]
    class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as bool?;
            if (item == null) return null;

            return item.Value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    class StringRegularizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as string;
            if (item == null) return null;

            StringBuilder sb = new StringBuilder(item);
            sb.Replace('\r', ' ');
            sb.Replace('\n', ' ');
            sb.Replace('\uFFFD', ' ');

            return sb.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(double), typeof(GridLength))]
    class DoubleToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as double?;
            if (item == null) return null;

            if (item.Value < 0)
            {
                return GridLength.Auto;
            }
            else
            {
                return new GridLength(item.Value);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as GridLength?;
            if (item == null) return null;

            if (GridLength.Auto == item.Value)
            {
                return -1;
            }
            else
            {
                return item.Value.Value;
            }
        }
    }

    public delegate double GetDoubleEventHandler(object sender);

    [ValueConversion(typeof(double), typeof(double))]
    class TopRelativeDoubleConverter : IValueConverter
    {
        public static GetDoubleEventHandler GetDoubleEvent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as double?;
            if (item == null) return null;

            return item + this.OnGetDoubleEvent();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as double?;
            if (item == null) return null;

            return item - this.OnGetDoubleEvent();
        }

        protected virtual double OnGetDoubleEvent()
        {
            if (GetDoubleEvent != null)
            {
                return GetDoubleEvent(this);
            }

            return 0;
        }
    }

    [ValueConversion(typeof(double), typeof(double))]
    class LeftRelativeDoubleConverter : IValueConverter
    {
        public static GetDoubleEventHandler GetDoubleEvent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as double?;
            if (item == null) return null;

            return item + this.OnGetDoubleEvent();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as double?;
            if (item == null) return null;

            return item - this.OnGetDoubleEvent();
        }

        protected virtual double OnGetDoubleEvent()
        {
            if (GetDoubleEvent != null)
            {
                return GetDoubleEvent(this);
            }

            return 0;
        }
    }

    [ValueConversion(typeof(object), typeof(string))]
    class ObjectToInfoStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Seed)
            {
                return MessageConverter.ToInfoMessage((Seed)value);
            }
            else if (value is Box)
            {
                return MessageConverter.ToInfoMessage((Box)value);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(Node), typeof(string))]
    class NodeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as Node;
            if (item == null) return null;

            return AmoebaConverter.ToNodeString(item);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(Seed), typeof(string))]
    class SeedToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as Seed;
            if (item == null) return null;

            return AmoebaConverter.ToSeedString(item);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(byte[]), typeof(string))]
    class BytesToHexStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as byte[];
            if (item == null) return null;

            return NetworkConverter.ToHexString(item);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(IEnumerable<string>), typeof(string))]
    class StringsToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var items = value as IEnumerable<string>;
            if (items == null) return null;

            return String.Join(", ", items);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(long), typeof(string))]
    class LongToSizeStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as long?;
            if (item == null) return null;

            return NetworkConverter.ToSizeString(item.Value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(bool), typeof(string))]
    class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as bool?;
            if (item == null) return null;

            return item.Value ? "＋" : "－";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(DateTime), typeof(string))]
    class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as DateTime?;
            if (item == null) return null;

            return item.Value.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(string), typeof(System.Windows.Media.FontFamily))]
    class StringToFontFamilyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as string;
            if (item == null) return null;

            return new System.Windows.Media.FontFamily(item);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(string), typeof(double))]
    class StringToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as string;
            if (item == null) return null;

            return double.Parse(item);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(SearchState), typeof(string))]
    class SearchStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is SearchState)) return null;
            var item = (SearchState)value;

            List<string> list = new List<string>();

            if (item.HasFlag(SearchState.Cache))
            {
                list.Add(LanguagesManager.Instance.SearchState_Cache);
            }
            if (item.HasFlag(SearchState.Share))
            {
                list.Add(LanguagesManager.Instance.SearchState_Share);
            }
            if (item.HasFlag(SearchState.Box))
            {
                list.Add(LanguagesManager.Instance.SearchState_Box);
            }
            if (item.HasFlag(SearchState.Downloading))
            {
                list.Add(LanguagesManager.Instance.SearchState_Downloading);
            }
            if (item.HasFlag(SearchState.Uploading))
            {
                list.Add(LanguagesManager.Instance.SearchState_Uploading);
            }
            if (item.HasFlag(SearchState.Downloaded))
            {
                list.Add(LanguagesManager.Instance.SearchState_Downloaded);
            }
            if (item.HasFlag(SearchState.Uploaded))
            {
                list.Add(LanguagesManager.Instance.SearchState_Uploaded);
            }

            if (list.Count == 0) return "";
            else return string.Join(" ", list);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(SearchState), typeof(string))]
    class SearchStateFlagToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is SearchState)) return null;
            var item = (SearchState)value;

            if (item.HasFlag(SearchState.Cache))
            {
                return LanguagesManager.Instance.SearchItemEditWindow_SearchState_Cache;
            }
            else if (item.HasFlag(SearchState.Share))
            {
                return LanguagesManager.Instance.SearchItemEditWindow_SearchState_Share;
            }
            else if (item.HasFlag(SearchState.Downloading))
            {
                return LanguagesManager.Instance.SearchItemEditWindow_SearchState_Downloading;
            }
            else if (item.HasFlag(SearchState.Uploading))
            {
                return LanguagesManager.Instance.SearchItemEditWindow_SearchState_Uploading;
            }
            else if (item.HasFlag(SearchState.Downloaded))
            {
                return LanguagesManager.Instance.SearchItemEditWindow_SearchState_Downloaded;
            }
            else if (item.HasFlag(SearchState.Uploaded))
            {
                return LanguagesManager.Instance.SearchItemEditWindow_SearchState_Uploaded;
            }
            else if (item.HasFlag(SearchState.Box))
            {
                return LanguagesManager.Instance.SearchItemEditWindow_SearchState_Box;
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(DownloadState), typeof(string))]
    class DownloadStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as DownloadState?;
            if (item == null) return null;

            if (item.Value == DownloadState.Downloading)
            {
                return LanguagesManager.Instance.DownloadState_Downloading;
            }
            else if (item.Value == DownloadState.Decoding)
            {
                return LanguagesManager.Instance.DownloadState_Decoding;
            }
            else if (item.Value == DownloadState.ParityDecoding)
            {
                return LanguagesManager.Instance.DownloadState_ParityDecoding;
            }
            else if (item.Value == DownloadState.Completed)
            {
                return LanguagesManager.Instance.DownloadState_Completed;
            }
            else if (item.Value == DownloadState.Error)
            {
                return LanguagesManager.Instance.DownloadState_Error;
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(UploadState), typeof(string))]
    class UploadStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as UploadState?;
            if (item == null) return null;

            if (item.Value == UploadState.ComputeHash)
            {
                return LanguagesManager.Instance.UploadState_ComputeHash;
            }
            else if (item.Value == UploadState.Encoding)
            {
                return LanguagesManager.Instance.UploadState_Encoding;
            }
            else if (item.Value == UploadState.ParityEncoding)
            {
                return LanguagesManager.Instance.UploadState_ParityEncoding;
            }
            else if (item.Value == UploadState.Uploading)
            {
                return LanguagesManager.Instance.UploadState_Uploading;
            }
            else if (item.Value == UploadState.Completed)
            {
                return LanguagesManager.Instance.UploadState_Completed;
            }
            else if (item.Value == UploadState.Error)
            {
                return LanguagesManager.Instance.UploadState_Error;
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(ConnectionType), typeof(string))]
    class ConnectionTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as ConnectionType?;
            if (item == null) return LanguagesManager.Instance.ConnectionType_None;

            if (item.Value == ConnectionType.None)
            {
                return LanguagesManager.Instance.ConnectionType_None;
            }
            else if (item.Value == ConnectionType.Tcp)
            {
                return LanguagesManager.Instance.ConnectionType_Tcp;
            }
            else if (item.Value == ConnectionType.Socks4Proxy)
            {
                return LanguagesManager.Instance.ConnectionType_Socks4Proxy;
            }
            else if (item.Value == ConnectionType.Socks4aProxy)
            {
                return LanguagesManager.Instance.ConnectionType_Socks4aProxy;
            }
            else if (item.Value == ConnectionType.Socks5Proxy)
            {
                return LanguagesManager.Instance.ConnectionType_Socks5Proxy;
            }
            else if (item.Value == ConnectionType.HttpProxy)
            {
                return LanguagesManager.Instance.ConnectionType_HttpProxy;
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as string;
            if (item == null) return ConnectionType.None;

            if (item == LanguagesManager.Instance.ConnectionType_None)
            {
                return ConnectionType.None;
            }
            else if (item == LanguagesManager.Instance.ConnectionType_Tcp)
            {
                return ConnectionType.Tcp;
            }
            else if (item == LanguagesManager.Instance.ConnectionType_Socks4Proxy)
            {
                return ConnectionType.Socks4Proxy;
            }
            else if (item == LanguagesManager.Instance.ConnectionType_Socks4aProxy)
            {
                return ConnectionType.Socks4aProxy;
            }
            else if (item == LanguagesManager.Instance.ConnectionType_Socks5Proxy)
            {
                return ConnectionType.Socks5Proxy;
            }
            else if (item == LanguagesManager.Instance.ConnectionType_HttpProxy)
            {
                return ConnectionType.HttpProxy;
            }

            return 0;
        }
    }

    [ValueConversion(typeof(int), typeof(bool))]
    class ExpanderToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string)value == (string)parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (System.Convert.ToBoolean(value)) return (string)parameter;
            return null;
        }
    }

    [ValueConversion(typeof(TransferLimitType), typeof(string))]
    class TransferLimitTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TransferLimitType)) return null;
            var item = (TransferLimitType)value;

            if (item == TransferLimitType.None)
            {
                return LanguagesManager.Instance.TransferLimitType_None;
            }
            else if (item == TransferLimitType.Downloads)
            {
                return LanguagesManager.Instance.TransferLimitType_Downloads;
            }
            else if (item == TransferLimitType.Uploads)
            {
                return LanguagesManager.Instance.TransferLimitType_Uploads;
            }
            else if (item == TransferLimitType.Total)
            {
                return LanguagesManager.Instance.TransferLimitType_Total;
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
