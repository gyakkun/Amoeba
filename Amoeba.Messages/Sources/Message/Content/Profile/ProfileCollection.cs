using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Messages
{
    public sealed class ProfileCollection : FilteredList<Profile>
    {
        public ProfileCollection() : base() { }
        public ProfileCollection(int capacity) : base(capacity) { }
        public ProfileCollection(IEnumerable<Profile> collections) : base(collections) { }

        protected override bool Filter(Profile item)
        {
            return (item != null);
        }
    }
}
