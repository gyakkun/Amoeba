using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Amoeba.Interface
{
    class StoreTreeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CategoryTemplate { get; set; }
        public DataTemplate StoreTemplate { get; set; }
        public DataTemplate BoxTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is StoreCategoryViewModel)
            {
                return this.CategoryTemplate;
            }
            else if (item is StoreViewModel)
            {
                return this.StoreTemplate;
            }
            else if (item is BoxViewModel)
            {
                return this.BoxTemplate;
            }

            return null;
        }
    }
}
