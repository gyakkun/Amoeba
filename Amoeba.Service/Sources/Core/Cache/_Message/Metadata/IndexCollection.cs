using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Service
{
    sealed class IndexCollection : FilteredList<Index>
    {
        public IndexCollection() : base() { }
        public IndexCollection(int capacity) : base(capacity) { }
        public IndexCollection(IEnumerable<Index> collections) : base(collections) { }

        protected override bool Filter(Index item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
