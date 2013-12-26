using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Amoeba.Windows
{
    class TreeViewItemEx : TreeViewItem
    {
        public new TreeViewItemEx Parent { get; set; }
    }
}
