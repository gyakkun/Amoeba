using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Amoeba.Windows
{
    abstract class TreeViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private TreeViewModelBase _parent;

        public TreeViewModelBase(TreeViewModelBase parent)
        {
            _parent = parent;
        }

        public IEnumerable<TreeViewModelBase> GetAncestors()
        {
            var list = new LinkedList<TreeViewModelBase>();
            list.AddFirst(this);

            for (;;)
            {
                var parent = list.First.Value.Parent;
                if (parent == null) break;

                list.AddFirst(parent);
            }

            return list;
        }

        public abstract string Name { get; }
        public abstract bool IsExpanded { get; set; }
        public abstract bool IsSelected { get; set; }

        public TreeViewModelBase Parent { get { return _parent; } }
        public abstract IReadOnlyCollection<TreeViewModelBase> Children { get; }
    }
}
