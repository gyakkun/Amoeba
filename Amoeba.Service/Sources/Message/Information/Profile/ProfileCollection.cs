using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Service
{
    public sealed class ProfileCollection : FilteredList<Profile>
    {
        public ProfileCollection() : base() { }
        public ProfileCollection(int capacity) : base(capacity) { }
        public ProfileCollection(IEnumerable<Profile> collections) : base(collections) { }

        protected override bool Filter(Profile item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
