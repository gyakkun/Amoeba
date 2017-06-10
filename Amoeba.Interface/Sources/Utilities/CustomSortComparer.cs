using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    public class CustomSortComparer : IComparer
    {
        public ListSortDirection _direction;
        public Func<object, object, int> _callback;

        public CustomSortComparer(ListSortDirection direction, Func<object, object, int> callback)
        {
            _direction = direction;
            _callback = callback;
        }

        public int Compare(object x, object y)
        {
            if (_direction == ListSortDirection.Ascending) return _callback.Invoke(x, y);
            else return _callback.Invoke(y, x);
        }
    }
}
