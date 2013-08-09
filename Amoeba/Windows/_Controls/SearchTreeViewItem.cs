using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Amoeba.Windows
{
    class SearchTreeViewItem : TreeViewItem
    {
        private SearchTreeItem _value;

        private ObservableCollection<SearchTreeViewItem> _listViewItemCollection = new ObservableCollection<SearchTreeViewItem>();
        private TextBlock _header = new TextBlock();
        private int _hit;

        public SearchTreeViewItem()
            : base()
        {
            this.Value = new SearchTreeItem()
            {
                SearchItem = new SearchItem()
                {
                    Name = "",
                },
            };

            base.ItemsSource = _listViewItemCollection;
            base.Header = _header;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        public SearchTreeViewItem(SearchTreeItem searchTreeItem)
            : base()
        {
            this.Value = searchTreeItem;

            base.ItemsSource = _listViewItemCollection;
            base.Header = _header;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
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
            _header.Text = string.Format("{0} ({1})", _value.SearchItem.Name, _hit);

            base.IsExpanded = this.Value.IsExpanded;

            foreach (var item in _listViewItemCollection.OfType<SearchTreeViewItem>().ToArray())
            {
                if (!_value.Children.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in _value.Children)
            {
                if (!_listViewItemCollection.OfType<SearchTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    _listViewItemCollection.Add(new SearchTreeViewItem(item));
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.OfType<SearchTreeViewItem>().ToList();

            list.Sort((x, y) =>
            {
                int c = x.Value.SearchItem.Name.CompareTo(y.Value.SearchItem.Name);
                if (c != 0) return c;
                c = x.Hit.CompareTo(y.Hit);
                if (c != 0) return c;

                return x.GetHashCode().CompareTo(y.GetHashCode());
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _listViewItemCollection.IndexOf(list[i]);

                if (i != o) _listViewItemCollection.Move(o, i);
            }

            foreach (var item in this.Items.OfType<SearchTreeViewItem>())
            {
                item.Sort();
            }
        }

        public SearchTreeItem Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                this.Update();
            }
        }

        public int Hit
        {
            get
            {
                return _hit;
            }
            set
            {
                _hit = value;

                this.Update();
            }
        }
    }
}
