using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Amoeba.Service;

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
            throw new NotSupportedException();
        }
    }
}
