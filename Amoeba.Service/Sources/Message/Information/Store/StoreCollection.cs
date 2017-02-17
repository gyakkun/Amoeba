using System.Collections.Generic;
using Library.Collections;

namespace Amoeba.Service
{
    public sealed class StoreCollection : LockedList<Store>
    {
        public StoreCollection() : base() { }
        public StoreCollection(int capacity) : base(capacity) { }
        public StoreCollection(IEnumerable<Store> collections) : base(collections) { }

        protected override bool Filter(Store item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
