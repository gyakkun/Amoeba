using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Messages
{
    public sealed class UriCollection : FilteredList<string>
    {
        public UriCollection() : base() { }
        public UriCollection(int capacity) : base(capacity) { }
        public UriCollection(IEnumerable<string> collections) : base(collections) { }

        protected override bool Filter(string item)
        {
            return (item != null);
        }
    }
}
