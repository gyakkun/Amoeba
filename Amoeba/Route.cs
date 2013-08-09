using System;
using System.Collections.Generic;
using Library.Collections;

namespace Library.Net.Amoeba
{
    public sealed class Route : FilterList<string>, IEquatable<Route>, IEnumerable<string>
    {
        public Route() : base() { }
        public Route(int capacity) : base(capacity) { }
        public Route(IEnumerable<string> collections) : base(collections) { }

        protected override bool Filter(string item)
        {
            if (item == null) return true;

            return false;
        }

        public override int GetHashCode()
        {
            if (this.Count == 0) return 0;
            else return this[0].GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is Route)) return false;

            return this.Equals((Route)obj);
        }

        public bool Equals(Route other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            return Collection.Equals(this, other);
        }

        #region IEnumerable<string>

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            lock (base.ThisLock)
            {
                return base.GetEnumerator();
            }
        }

        #endregion

        #region IEnumerable

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            lock (base.ThisLock)
            {
                return this.GetEnumerator();
            }
        }

        #endregion
    }
}
