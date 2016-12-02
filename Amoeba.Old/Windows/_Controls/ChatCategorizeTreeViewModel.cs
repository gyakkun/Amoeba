using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Library;
using Library.Net.Amoeba;
using Library.Security;
using System.Windows.Input;

namespace Amoeba.Windows
{
    sealed class ChatCategorizeTreeViewModel : TreeViewModelBase
    {
        private bool _isSelected;
        private bool _isHit;
        private ChatCategorizeTreeItem _value;

        private ReadOnlyObservableCollection<TreeViewModelBase> _readonlyChildren;
        private ObservableCollectionEx<TreeViewModelBase> _children;

        public ChatCategorizeTreeViewModel(TreeViewModelBase parent, ChatCategorizeTreeItem value)
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
            this.NotifyPropertyChanged(nameof(this.IsExpanded));

            foreach (var item in _children.OfType<ChatCategorizeTreeViewModel>().ToArray())
            {
                if (!_value.Children.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _children.Remove(item);
                }
            }

            foreach (var item in _value.Children)
            {
                if (!_children.OfType<ChatCategorizeTreeViewModel>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    _children.Add(new ChatCategorizeTreeViewModel(this, item));
                }
            }

            foreach (var item in _children.OfType<ChatTreeViewModel>().ToArray())
            {
                if (!_value.ChatTreeItems.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _children.Remove(item);
                }
            }

            foreach (var item in _value.ChatTreeItems)
            {
                if (!_children.OfType<ChatTreeViewModel>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    _children.Add(new ChatTreeViewModel(this, item));
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _children.Cast<TreeViewModelBase>().ToList();

            list.Sort((x, y) =>
            {
                if (x is ChatCategorizeTreeViewModel)
                {
                    if (y is ChatCategorizeTreeViewModel)
                    {
                        var vx = ((ChatCategorizeTreeViewModel)x).Value;
                        var vy = ((ChatCategorizeTreeViewModel)y).Value;

                        int c = vx.Name.CompareTo(vy.Name);
                        if (c != 0) return c;
                        c = vx.ChatTreeItems.Count.CompareTo(vy.ChatTreeItems.Count);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is ChatTreeViewModel)
                    {
                        return 1;
                    }
                }
                else if (x is ChatTreeViewModel)
                {
                    if (y is ChatTreeViewModel)
                    {
                        var vx = ((ChatTreeViewModel)x).Value;
                        var vy = ((ChatTreeViewModel)y).Value;

                        int c = vx.Tag.Name.CompareTo(vy.Tag.Name);
                        if (c != 0) return c;
                        c = CollectionUtils.Compare(vx.Tag.Id, vy.Tag.Id);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is ChatCategorizeTreeViewModel)
                    {
                        return -1;
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

                    this.NotifyPropertyChanged(nameof(this.IsSelected));
                }
            }
        }

        public bool IsExpanded
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

        public ChatCategorizeTreeItem Value
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
