using System;
using System.Collections.Generic;
using Library.Collections;

namespace Library.Net.Amoeba
{
    public sealed class Route : LockedList<string>, IEquatable<Route>, IEnumerable<string>
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

            return Collection.Equals(this, other);
        }
    }
}
