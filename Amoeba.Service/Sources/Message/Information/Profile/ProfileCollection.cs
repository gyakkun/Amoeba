using System.Collections.Generic;
using Library.Collections;

namespace Amoeba.Service
{
    public sealed class ProfileCollection : LockedList<Profile>
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
