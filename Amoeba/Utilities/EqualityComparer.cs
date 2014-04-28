using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Library;
using Library.Net.Amoeba;

namespace Amoeba
{
    class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        new public bool Equals(object x, object y)
        {
            return object.ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            else return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
