using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Service
{
    public sealed class BroadcastMetadataCollection : FilteredList<BroadcastMetadata>
    {
        public BroadcastMetadataCollection() : base() { }
        public BroadcastMetadataCollection(int capacity) : base(capacity) { }
        public BroadcastMetadataCollection(IEnumerable<BroadcastMetadata> collections) : base(collections) { }

        protected override bool Filter(BroadcastMetadata item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
