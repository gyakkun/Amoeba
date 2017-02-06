using System.Collections.Generic;
using Library.Collections;

namespace Amoeba.Service
{
    public sealed class BoxCollection : LockedList<Box>
    {
        public BoxCollection() : base() { }
        public BoxCollection(int capacity) : base(capacity) { }
        public BoxCollection(IEnumerable<Box> collections) : base(collections) { }

        protected override bool Filter(Box item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
