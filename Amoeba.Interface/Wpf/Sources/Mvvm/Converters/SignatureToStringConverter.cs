using System;
using System.Globalization;
using System.Windows.Data;
using Omnius.Security;

namespace Amoeba.Interface
{
    [ValueConversion(typeof(Signature), typeof(string))]
    class SignatureToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Signature item) return item.ToString();
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string item) return Signature.Parse(item);
            return null;
        }
    }
}
