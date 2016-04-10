using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Amoeba.Windows
{
    class StoreControl_StyleSelector : StyleSelector
    {
        public Style StoreCategorizeTreeViewModelStyle { get; set; }
        public Style StoreTreeViewModelStyle { get; set; }
        public Style BoxTreeViewModelStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is StoreCategorizeTreeViewModel)
            {
                return this.StoreCategorizeTreeViewModelStyle;
            }
            else if (item is StoreTreeViewModel)
            {
                return this.StoreTreeViewModelStyle;
            }
            else if (item is BoxTreeViewModel)
            {
                return this.BoxTreeViewModelStyle;
            }

            return null;
        }
    }
}
