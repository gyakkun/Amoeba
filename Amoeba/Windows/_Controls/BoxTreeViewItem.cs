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
    class BoxTreeViewItem : TreeViewItemEx
    {
        private Box _value;

        private ObservableCollectionEx<BoxTreeViewItem> _listViewItemCollection = new ObservableCollectionEx<BoxTreeViewItem>();
        private TextBlock _header = new TextBlock();

        public BoxTreeViewItem(Box value)
            : base()
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            this.ItemsSource = _listViewItemCollection;
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

        private static int GetTotalSeedCount(Box box)
        {
            List<Box> boxList = new List<Box>();
            List<Seed> seedList = new List<Seed>();

            boxList.Add(box);

            for (int i = 0; i < boxList.Count; i++)
            {
                boxList.AddRange(boxList[i].Boxes);
                seedList.AddRange(boxList[i].Seeds);
            }

            return seedList.Count;
        }

        private void Update_Header()
        {
            if (this.Value.Certificate == null)
            {
                _header.Text = string.Format("{0} ({1})", this.Value.Name, BoxTreeViewItem.GetTotalSeedCount(this.Value));
            }
            else
            {
                _header.Text = string.Format("{0} ({1}) - {2}", this.Value.Name, BoxTreeViewItem.GetTotalSeedCount(this.Value), this.Value.Certificate.ToString());
            }

            if (this.Parent != null)
            {
                if (this.Parent is BoxTreeViewItem)
                {
                    var parentBoxTreeViewItem = (BoxTreeViewItem)this.Parent;

                    parentBoxTreeViewItem.Update_Header();
                }
            }
        }

        public void Update()
        {
            this.Update_Header();

            foreach (var item in _listViewItemCollection.OfType<BoxTreeViewItem>().ToArray())
            {
                if (!_value.Boxes.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in _value.Boxes)
            {
                if (!_listViewItemCollection.OfType<BoxTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    var treeViewItem = new BoxTreeViewItem(item);
                    treeViewItem.Parent = this;

                    _listViewItemCollection.Add(treeViewItem);
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.OfType<BoxTreeViewItem>().ToList();

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
                var o = _listViewItemCollection.IndexOf(list[i]);

                if (i != o) _listViewItemCollection.Move(o, i);
            }

            foreach (var item in this.Items.OfType<BoxTreeViewItem>())
            {
                item.Sort();
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
