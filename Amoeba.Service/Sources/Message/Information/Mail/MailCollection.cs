using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Service
{
    public sealed class MailCollection : FilteredList<Mail>
    {
        public MailCollection() : base() { }
        public MailCollection(int capacity) : base(capacity) { }
        public MailCollection(IEnumerable<Mail> collections) : base(collections) { }

        protected override bool Filter(Mail item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
