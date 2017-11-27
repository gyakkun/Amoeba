using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Messages
{
    public sealed class MetadataCollection : FilteredList<Metadata>
    {
        public MetadataCollection() : base() { }
        public MetadataCollection(int capacity) : base(capacity) { }
        public MetadataCollection(IEnumerable<Metadata> collections) : base(collections) { }

        protected override bool Filter(Metadata item)
        {
            return (item != null);
        }
    }
}
