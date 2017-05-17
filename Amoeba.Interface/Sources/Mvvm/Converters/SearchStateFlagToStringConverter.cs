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
    [ValueConversion(typeof(SearchState), typeof(string))]
    class SearchStateFlagToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = new List<string>();

            if (value is SearchState type)
            {
                if (type == SearchState.Cache)
                {
                    list.Add(LanguagesManager.Instance.SearchState_Flag_Cache);
                }
                if (type == SearchState.Download)
                {
                    list.Add(LanguagesManager.Instance.SearchState_Flag_Download);
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
