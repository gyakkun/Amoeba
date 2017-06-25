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
                if (type == SearchState.Downloading)
                {
                    return LanguagesManager.Instance.SearchState_Downloading;
                }
                if (type == SearchState.Downloaded)
                {
                    return LanguagesManager.Instance.SearchState_Downloaded;
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
