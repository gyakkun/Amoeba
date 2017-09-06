using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Messages
{
    public sealed class StoreCollection : FilteredList<Store>
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
