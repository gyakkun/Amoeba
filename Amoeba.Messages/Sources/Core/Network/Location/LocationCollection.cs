using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Messages
{
    public sealed class LocationCollection : FilteredList<Location>
    {
        public LocationCollection() : base() { }
        public LocationCollection(int capacity) : base(capacity) { }
        public LocationCollection(IEnumerable<Location> collections) : base(collections) { }

        protected override bool Filter(Location item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
