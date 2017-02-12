using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Core
{
  public  sealed class BroadcastMessageCollection : FilteredList<BroadcastMessage>
    {
        public BroadcastMessageCollection() : base() { }
        public BroadcastMessageCollection(int capacity) : base(capacity) { }
        public BroadcastMessageCollection(IEnumerable<BroadcastMessage> collections) : base(collections) { }

        protected override bool Filter(BroadcastMessage item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
