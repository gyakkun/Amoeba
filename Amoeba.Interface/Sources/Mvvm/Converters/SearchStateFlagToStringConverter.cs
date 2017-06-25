using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Amoeba.Interface
{
    [ValueConversion(typeof(SearchState), typeof(string))]
    class SearchStateFlagToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = new List<string>();

            if (value is SearchState type)
            {
                if (type.HasFlag(SearchState.Cache))
                {
                    list.Add(LanguagesManager.Instance.SearchState_Flag_Cache);
                }
                if (type.HasFlag(SearchState.Downloading))
                {
                    list.Add(LanguagesManager.Instance.SearchState_Flag_Downloading);
                }
                if (type.HasFlag(SearchState.Downloaded))
                {
                    list.Add(LanguagesManager.Instance.SearchState_Flag_Downloaded);
                }
            }

            if (list.Count == 0) return "";
            else return string.Join(" ", list);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
