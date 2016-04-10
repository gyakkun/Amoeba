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
    sealed class StoreCategorizeTreeViewModel : TreeViewModelBase
    {
        private bool _isSelected;
        private bool _isHit;
        private StoreCategorizeTreeItem _value;

        private ReadOnlyObservableCollection<TreeViewModelBase> _readonlyChildren;
        private ObservableCollectionEx<TreeViewModelBase> _children;

        public StoreCategorizeTreeViewModel(TreeViewModelBase parent, StoreCategorizeTreeItem value)
            : base(parent)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            _children = new ObservableCollectionEx<TreeViewModelBase>();
            _readonlyChildren = new ReadOnlyObservableCollection<TreeViewModelBase>(_children);

            this.Value = value;
        }

        public void Update()
        {
            this.NotifyPropertyChanged("Name");
            this.NotifyPropertyChanged("IsExpanded");

            foreach (var item in _children.OfType<StoreCategorizeTreeViewModel>().ToArray())
            {
                if (!_value.Children.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _children.Remove(item);
                }
            }

            foreach (var item in _value.Children)
            {
                if (!_children.OfType<StoreCategorizeTreeViewModel>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    _children.Add(new StoreCategorizeTreeViewModel(this, item));
                }
            }

            foreach (var item in _children.OfType<StoreTreeViewModel>().ToArray())
            {
                if (!_value.StoreTreeItems.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _children.Remove(item);
                }
            }

            foreach (var item in _value.StoreTreeItems)
            {
                if (!_children.OfType<StoreTreeViewModel>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    _children.Add(new StoreTreeViewModel(this, item));
                }
            }

            this.Sort();
        }

        private void Sort()
        {
            var list = _children.Cast<TreeViewModelBase>().ToList();

            list.Sort((x, y) =>
            {
                if (x is StoreCategorizeTreeViewModel)
                {
                    if (y is StoreCategorizeTreeViewModel)
                    {
                        var vx = ((StoreCategorizeTreeViewModel)x).Value;
                        var vy = ((StoreCategorizeTreeViewModel)y).Value;

                        int c = vx.Name.CompareTo(vy.Name);
                        if (c != 0) return c;
                        c = vx.StoreTreeItems.Count.CompareTo(vy.StoreTreeItems.Count);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is StoreTreeViewModel)
                    {
                        return -1;
                    }
                }
                else if (x is StoreTreeViewModel)
                {
                    if (y is StoreTreeViewModel)
                    {
                        var vx = ((StoreTreeViewModel)x).Value;
                        var vy = ((StoreTreeViewModel)y).Value;

                        int c = vx.Signature.CompareTo(vy.Signature);
                        if (c != 0) return c;
                        c = vx.Boxes.Count.CompareTo(vy.Boxes.Count);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is StoreCategorizeTreeViewModel)
                    {
                        return 1;
                    }
                }

                return 0;
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _children.IndexOf(list[i]);

                if (i != o) _children.Move(o, i);
            }
        }

        public override string Name
        {
            get
            {
                return this.Value.Name;
            }
        }

        public override bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;

                    this.NotifyPropertyChanged("IsSelected");
                }
            }
        }

        public override bool IsExpanded
        {
            get
            {
                return _value.IsExpanded;
            }
            set
            {
                if (value != _value.IsExpanded)
                {
                    _value.IsExpanded = value;

                    this.NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        public bool IsHit
        {
            get
            {
                return _isHit;
            }
            set
            {
                if (value != _isHit)
                {
                    _isHit = value;

                    this.NotifyPropertyChanged("IsHit");
                }
            }
        }

        public override ReadOnlyObservableCollection<TreeViewModelBase> Children
        {
            get
            {
                return _readonlyChildren;
            }
        }

        public StoreCategorizeTreeItem Value
        {
            get
            {
                return _value;
            }
            private set
            {
                _value = value;

                this.Update();
            }
        }
    }
}
