using System;
using System.Globalization;
using System.Windows.Data;
using Amoeba.Service;

namespace Amoeba.Interface
{
    [ValueConversion(typeof(Tag), typeof(string))]
    class TagToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Tag item) return MessageConvertUtils.ToString(item);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
