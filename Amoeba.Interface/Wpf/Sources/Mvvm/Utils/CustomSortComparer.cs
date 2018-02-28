using System;
using System.Collections;
using System.ComponentModel;

namespace Amoeba.Interface
{
    class CustomSortComparer : IComparer
    {
        public int _direction;
        public Func<object, object, int> _callback;

        public CustomSortComparer(ListSortDirection direction, Func<object, object, int> callback)
        {
            _direction = (direction == ListSortDirection.Ascending) ? 1 : -1;
            _callback = callback;
        }

        public int Compare(object x, object y)
        {
            return _direction * _callback.Invoke(x, y);
        }
    }
}
