using System;
using System.Globalization;
using System.Windows.Data;
using Amoeba.Messages;
using Amoeba.Service;

namespace Amoeba.Interface
{
    [ValueConversion(typeof(SessionType), typeof(string))]
    class SessionTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SessionType type)
            {
                if (type == SessionType.Connect)
                {
                    return LanguagesManager.Instance.SessionType_Connect;
                }
                if (type == SessionType.Accept)
                {
                    return LanguagesManager.Instance.SessionType_Accept;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
