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
    class StoreCategorizeTreeViewItem : TreeViewItemEx
    {
        private StoreCategorizeTreeItem _value;

        private ObservableCollectionEx<TreeViewItem> _listViewItemCollection = new ObservableCollectionEx<TreeViewItem>();
        private TextBlock _header = new TextBlock();

        public StoreCategorizeTreeViewItem(StoreCategorizeTreeItem value)
            : base()
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            base.ItemsSource = _listViewItemCollection;
            base.Header = _header;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };

            this.Value = value;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        protected override void OnExpanded(RoutedEventArgs e)
        {
            base.OnExpanded(e);

            this.Value.IsExpanded = true;
        }

        protected override void OnCollapsed(RoutedEventArgs e)
        {
            base.OnCollapsed(e);

            this.Value.IsExpanded = false;
        }

        public void Update()
        {
            _header.Text = this.Value.Name;

            base.IsExpanded = this.Value.IsExpanded;

            foreach (var item in _listViewItemCollection.OfType<StoreCategorizeTreeViewItem>().ToArray())
            {
                if (!_value.Children.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in _value.Children)
            {
                if (!_listViewItemCollection.OfType<StoreCategorizeTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    var treeViewItem = new StoreCategorizeTreeViewItem(item);
                    treeViewItem.Parent = this;

                    _listViewItemCollection.Add(treeViewItem);
                }
            }

            foreach (var item in _listViewItemCollection.OfType<StoreTreeViewItem>().ToArray())
            {
                if (!_value.StoreTreeItems.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in _value.StoreTreeItems)
            {
                if (!_listViewItemCollection.OfType<StoreTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    var treeViewItem = new StoreTreeViewItem(item);
                    treeViewItem.Parent = this;

                    _listViewItemCollection.Add(treeViewItem);
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.Cast<TreeViewItem>().ToList();

            list.Sort((x, y) =>
            {
                if (x is StoreCategorizeTreeViewItem)
                {
                    if (y is StoreCategorizeTreeViewItem)
                    {
                        var vx = ((StoreCategorizeTreeViewItem)x).Value;
                        var vy = ((StoreCategorizeTreeViewItem)y).Value;

                        int c = vx.Name.CompareTo(vy.Name);
                        if (c != 0) return c;
                        c = vx.StoreTreeItems.Count.CompareTo(vy.StoreTreeItems.Count);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is StoreTreeViewItem)
                    {
                        return -1;
                    }
                }
                else if (x is StoreTreeViewItem)
                {
                    if (y is StoreTreeViewItem)
                    {
                        var vx = ((StoreTreeViewItem)x).Value;
                        var vy = ((StoreTreeViewItem)y).Value;

                        int c = vx.Signature.CompareTo(vy.Signature);
                        if (c != 0) return c;
                        c = vx.Boxes.Count.CompareTo(vy.Boxes.Count);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is StoreCategorizeTreeViewItem)
                    {
                        return 1;
                    }
                }

                return 0;
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _listViewItemCollection.IndexOf(list[i]);

                if (i != o) _listViewItemCollection.Move(o, i);
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
