using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Service
{
    sealed class GroupCollection : FilteredList<Group>
    {
        public GroupCollection() : base() { }
        public GroupCollection(int capacity) : base(capacity) { }
        public GroupCollection(IEnumerable<Group> collections) : base(collections) { }

        protected override bool Filter(Group item)
        {
            return (item != null);
        }
    }
}
