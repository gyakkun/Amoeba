using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Amoeba
{
    class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        public ObservableCollectionEx()
        {

        }

        public ObservableCollectionEx(IEnumerable<T> collection)
            : base(collection)
        {

        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                base.Add(item);
            }
        }

        public void Set(int index, T item)
        {
            base.SetItem(index, item);
        }
    }
}
