using System;
using System.Globalization;
using System.Windows.Data;
using Amoeba.Service;

namespace Amoeba.Interface
{
    [ValueConversion(typeof(object), typeof(string))]
    class ObjectToInfoStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Seed seed)
            {
                return MessageConvertUtils.ToInfoMessage(seed);
            }
            else if (value is Box box)
            {
                return MessageConvertUtils.ToInfoMessage(box);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
