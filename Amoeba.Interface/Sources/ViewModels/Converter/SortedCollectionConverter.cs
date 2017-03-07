using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Amoeba.Interface
{
    class SortedCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var view = new ListCollectionView((IList)value);
            var sort = new SortDescription((string)parameter, ListSortDirection.Ascending);
            view.SortDescriptions.Add(sort);

            //var liveShaping = view as ICollectionViewLiveShaping;
            //if (liveShaping != null && liveShaping.CanChangeLiveSorting)
            //{
            //    liveShaping.LiveSortingProperties.Add((string)parameter);
            //    liveShaping.IsLiveSorting = true;
            //}

            return view;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
