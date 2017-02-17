using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Service
{
    public sealed class LinkCollection : FilteredList<Link>
    {
        public LinkCollection() : base() { }
        public LinkCollection(int capacity) : base(capacity) { }
        public LinkCollection(IEnumerable<Link> collections) : base(collections) { }

        protected override bool Filter(Link item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
