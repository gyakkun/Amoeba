using System;
using System.Globalization;
using System.Windows.Data;

namespace Amoeba.Interface
{
    [ValueConversion(typeof(SearchState), typeof(string))]
    class SearchStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SearchState type)
            {
                if (type == SearchState.Cache)
                {
                    return LanguagesManager.Instance.SearchState_Cache;
                }
                if (type == SearchState.Download)
                {
                    return LanguagesManager.Instance.SearchState_Download;
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
