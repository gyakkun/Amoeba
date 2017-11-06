using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Messages
{
    public sealed class SeedCollection : FilteredList<Seed>
    {
        public SeedCollection() : base() { }
        public SeedCollection(int capacity) : base(capacity) { }
        public SeedCollection(IEnumerable<Seed> collections) : base(collections) { }

        protected override bool Filter(Seed item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
