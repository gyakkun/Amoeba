using System;
using System.Globalization;
using System.Windows.Data;
using Omnius.Net.Amoeba;

namespace Amoeba.Interface
{
    [ValueConversion(typeof(object), typeof(string))]
    class ObjectToInfoStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Seed seed)
            {
                return MessageUtils.ToInfoMessage(seed);
            }
            else if (value is Box box)
            {
                return MessageUtils.ToInfoMessage(box);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
