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
                if (type == SearchState.Store)
                {
                    return LanguagesManager.Instance.SearchState_Store;
                }
                else if (type == SearchState.Cache)
                {
                    return LanguagesManager.Instance.SearchState_Cache;
                }
                else if (type == SearchState.Downloading)
                {
                    return LanguagesManager.Instance.SearchState_Downloading;
                }
                else if (type == SearchState.Downloaded)
                {
                    return LanguagesManager.Instance.SearchState_Downloaded;
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string type)
            {
                if (type == LanguagesManager.Instance.SearchState_Store)
                {
                    return SearchState.Store;
                }
                else if (type == LanguagesManager.Instance.SearchState_Cache)
                {
                    return SearchState.Cache;
                }
                else if (type == LanguagesManager.Instance.SearchState_Downloading)
                {
                    return SearchState.Downloading;
                }
                else if (type == LanguagesManager.Instance.SearchState_Downloaded)
                {
                    return SearchState.Downloaded;
                }
            }

            return (SearchState)0;
        }
    }
}
