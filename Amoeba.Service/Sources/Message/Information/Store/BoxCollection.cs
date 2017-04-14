using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Service
{
    public sealed class BoxCollection : FilteredList<Box>
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
