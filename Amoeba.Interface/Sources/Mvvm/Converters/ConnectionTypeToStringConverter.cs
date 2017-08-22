using System;
using System.Globalization;
using System.Windows.Data;
using Omnius.Net.Amoeba;

namespace Amoeba.Interface
{
    [ValueConversion(typeof(ConnectionType), typeof(string))]
    class ConnectionTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionType type)
            {
                if (type == ConnectionType.None)
                {
                    return LanguagesManager.Instance.ConnectionType_None;
                }
                else if (type == ConnectionType.Tcp)
                {
                    return LanguagesManager.Instance.ConnectionType_Tcp;
                }
                else if (type == ConnectionType.Socks5Proxy)
                {
                    return LanguagesManager.Instance.ConnectionType_Socks5Proxy;
                }
                else if (type == ConnectionType.HttpProxy)
                {
                    return LanguagesManager.Instance.ConnectionType_HttpProxy;
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
