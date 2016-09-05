using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Library;

namespace Amoeba.Windows
{
    sealed class StoreTreeViewModel : TreeViewModelBase
    {
        private bool _isSelected;
        private bool _isHit;
        private StoreTreeItem _value;

        private ReadOnlyObservableCollection<TreeViewModelBase> _readonlyChildren;
        private ObservableCollectionEx<TreeViewModelBase> _children;

        public StoreTreeViewModel(TreeViewModelBase parent, StoreTreeItem value)
            : base(parent)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            _children = new ObservableCollectionEx<TreeViewModelBase>();
            _readonlyChildren = new ReadOnlyObservableCollection<TreeViewModelBase>(_children);

            this.Value = value;
        }

        public void Update()
        {
            this.NotifyPropertyChanged(nameof(this.Name));

            foreach (var item in _children.OfType<BoxTreeViewModel>().ToArray())
            {
                if (!_value.Boxes.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _children.Remove(item);
                }
            }

            foreach (var item in _value.Boxes)
            {
                if (!_children.OfType<BoxTreeViewModel>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    _children.Add(new BoxTreeViewModel(this, item));
                }
            }

            this.Sort();
        }

        private void Sort()
        {
            var list = _children.OfType<BoxTreeViewModel>().ToList();

            list.Sort((x, y) =>
            {
                int c = x.Value.Name.CompareTo(y.Value.Name);
                if (c != 0) return c;
                c = y.Value.Seeds.Count.CompareTo(x.Value.Seeds.Count);
                if (c != 0) return c;
                c = y.Value.Boxes.Count.CompareTo(x.Value.Boxes.Count);
                if (c != 0) return c;

                return x.GetHashCode().CompareTo(y.GetHashCode());
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
                return this.Value.Signature;
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

                    this.NotifyPropertyChanged(nameof(this.IsSelected));
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

                    this.NotifyPropertyChanged(nameof(this.IsExpanded));
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

                    this.NotifyPropertyChanged(nameof(this.IsHit));
                }
            }
        }

        public override IReadOnlyCollection<TreeViewModelBase> Children
        {
            get
            {
                return _readonlyChildren;
            }
        }

        public StoreTreeItem Value
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
