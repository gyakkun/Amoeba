using System;
using System.Globalization;
using System.Windows.Data;
using Omnius.Net.Amoeba;

namespace Amoeba.Interface
{
    [ValueConversion(typeof(Tag), typeof(string))]
    class TagToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Tag item) return MessageUtils.ToString(item);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
