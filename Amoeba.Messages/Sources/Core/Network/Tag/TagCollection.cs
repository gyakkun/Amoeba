using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Messages
{
    public sealed class TagCollection : FilteredList<Tag>
    {
        public TagCollection() : base() { }
        public TagCollection(int capacity) : base(capacity) { }
        public TagCollection(IEnumerable<Tag> collections) : base(collections) { }

        protected override bool Filter(Tag item)
        {
            return (item != null);
        }
    }
}
