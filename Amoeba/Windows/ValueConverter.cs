using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private static Dictionary<string, BitmapImage> _images = new Dictionary<string, BitmapImage>();

        static ObjectToImageConverter()
        {
            try
            {
                var boxImage = ObjectToImageConverter.GetImage(Path.Combine(App.DirectoryPaths["Icons"], "Box.png"));
                var seedImage = ObjectToImageConverter.GetImage(Path.Combine(App.DirectoryPaths["Icons"], "Seed.png"));

                _images["Box"] = boxImage;
                _images["Seed"] = seedImage;
            }
            catch (Exception)
            {

            }
        }

        private static BitmapImage GetImage(string path)
        {
            var icon = new BitmapImage();
            icon.BeginInit();
            icon.StreamSource = new FileStream(path, FileMode.Open);
            icon.EndInit();
            return icon;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            var key = value.GetType().Name.ToString();

            if (_images.ContainsKey(key))
            {
                return _images[key];
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    class StringRegularizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as string;
            if (item == null) return null;

            return item.Replace('\r', ' ').Replace('\n', ' ');
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

    [ValueConversion(typeof(SearchState), typeof(string))]
    class SearchStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is SearchState)) return null;
            var item = (SearchState)value;

            string text = "";

            if ((item & SearchState.Searching) == SearchState.Searching)
            {
                text += LanguagesManager.Instance.SearchState_Searching + ", ";
            }
            if ((item & SearchState.Uploading) == SearchState.Uploading)
            {
                text += LanguagesManager.Instance.SearchState_Uploading + ", ";
            }
            if ((item & SearchState.Downloading) == SearchState.Downloading)
            {
                text += LanguagesManager.Instance.SearchState_Downloading + ", ";
            }
            if ((item & SearchState.Uploaded) == SearchState.Uploaded)
            {
                text += LanguagesManager.Instance.SearchState_Uploaded + ", ";
            }
            if ((item & SearchState.Downloaded) == SearchState.Downloaded)
            {
                text += LanguagesManager.Instance.SearchState_Downloaded + ", ";
            }

            if (text.Length < 2) return "";
            return text.Remove(text.Length - 2);
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
            else if (item.Value == UploadState.ComputeCorrection)
            {
                return LanguagesManager.Instance.UploadState_ComputeCorrection;
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
}
