using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Core
{
   public sealed class UnicastMessageCollection : FilteredList<UnicastMessage>
    {
        public UnicastMessageCollection() : base() { }
        public UnicastMessageCollection(int capacity) : base(capacity) { }
        public UnicastMessageCollection(IEnumerable<UnicastMessage> collections) : base(collections) { }

        protected override bool Filter(UnicastMessage item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
