using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Library;
using Library.Net.Amoeba;

namespace Amoeba.Windows
{
    sealed class BoxTreeViewModel : TreeViewModelBase
    {
        private bool _isSelected;
        private bool _isExpanded;
        private Box _value;

        private ReadOnlyObservableCollection<TreeViewModelBase> _readonlyChildren;
        private ObservableCollectionEx<TreeViewModelBase> _children;

        public BoxTreeViewModel(TreeViewModelBase parent, Box value)
            : base(parent)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            _children = new ObservableCollectionEx<TreeViewModelBase>();
            _readonlyChildren = new ReadOnlyObservableCollection<TreeViewModelBase>(_children);

            this.Value = value;
        }

        private static int GetTotalSeedCount(Box box)
        {
            var boxList = new List<Box>();
            var seedList = new List<Seed>();

            boxList.Add(box);

            for (int i = 0; i < boxList.Count; i++)
            {
                boxList.AddRange(boxList[i].Boxes);
                seedList.AddRange(boxList[i].Seeds);
            }

            return seedList.Count;
        }

        public void Update()
        {
            this.NotifyPropertyChanged("Name");

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
                c = (x.Value.Certificate == null).CompareTo(y.Value.Certificate == null);
                if (c != 0) return c;
                if (x.Value.Certificate != null && x.Value.Certificate != null)
                {
                    c = CollectionUtilities.Compare(x.Value.Certificate.PublicKey, y.Value.Certificate.PublicKey);
                    if (c != 0) return c;
                }
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
                if (this.Value.Certificate == null)
                {
                    return string.Format("{0} ({1})", this.Value.Name, BoxTreeViewModel.GetTotalSeedCount(this.Value));
                }
                else
                {
                    return string.Format("{0} ({1}) - {2}", this.Value.Name, BoxTreeViewModel.GetTotalSeedCount(this.Value), this.Value.Certificate.ToString());
                }
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
                return _isExpanded;
            }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;

                    this.NotifyPropertyChanged("IsExpanded");
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

        public Box Value
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
