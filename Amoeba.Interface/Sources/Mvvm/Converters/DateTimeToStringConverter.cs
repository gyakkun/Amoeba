using System;
using System.Globalization;
using System.Windows.Data;

namespace Amoeba.Interface
{
    [ValueConversion(typeof(DateTime), typeof(string))]
    class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime item) return item.ToString(LanguagesManager.Instance.Global_DateTime_StringFormat);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string item)
            {
                try
                {
                    var result = DateTime.ParseExact(item, LanguagesManager.Instance.Global_DateTime_StringFormat, DateTimeFormatInfo.InvariantInfo);
                    return result;
                }
                catch (Exception)
                {

                }
            }

            return DateTime.MinValue;
        }
    }
}
