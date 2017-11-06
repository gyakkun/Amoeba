using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Messages
{
    public sealed class ConnectionFilterCollection : LockedList<ConnectionFilter>
    {
        public ConnectionFilterCollection() : base() { }
        public ConnectionFilterCollection(int capacity) : base(capacity) { }
        public ConnectionFilterCollection(IEnumerable<ConnectionFilter> collections) : base(collections) { }

        protected override bool Filter(ConnectionFilter item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
