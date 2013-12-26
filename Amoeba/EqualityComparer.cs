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
            if (x == null && y == null) return true;
            if ((x == null) != (y == null)) return false;

            return object.ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            else return RuntimeHelpers.GetHashCode(obj);
        }
    }

    class SeedHashEqualityComparer : IEqualityComparer<Seed>
    {
        public bool Equals(Seed x, Seed y)
        {
            if (x == null && y == null) return true;
            if ((x == null) != (y == null)) return false;
            if (object.ReferenceEquals(x, y)) return true;

            if (x.Length != y.Length
                //|| ((x.Keywords == null) != (y.Keywords == null))
                //|| x.CreationTime != y.CreationTime
                //|| x.Name != y.Name
                //|| x.Comment != y.Comment
                || x.Rank != y.Rank

                || x.Key != y.Key

                || x.CompressionAlgorithm != y.CompressionAlgorithm

                || x.CryptoAlgorithm != y.CryptoAlgorithm
                || ((x.CryptoKey == null) != (y.CryptoKey == null)))

            //|| x.Certificate != y.Certificate)
            {
                return false;
            }

            //if (x.Keywords != null && y.Keywords != null)
            //{
            //    if (!Collection.Equals(x.Keywords, y.Keywords)) return false;
            //}

            if (x.CryptoKey != null && y.CryptoKey != null)
            {
                if (!Collection.Equals(x.CryptoKey, y.CryptoKey)) return false;
            }

            return true;
        }

        public int GetHashCode(Seed obj)
        {
            if (obj == null) return 0;
            else if (obj.Key == null) return 0;
            else return obj.Key.GetHashCode();
        }
    }
}
