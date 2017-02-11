using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Service
{
    public sealed class MessageCollection : FilteredList<Message>
    {
        public MessageCollection() : base() { }
        public MessageCollection(int capacity) : base(capacity) { }
        public MessageCollection(IEnumerable<Message> collections) : base(collections) { }

        protected override bool Filter(Message item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
