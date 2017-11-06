using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Messages
{
    public sealed class MailMessageCollection : FilteredList<MailMessage>
    {
        public MailMessageCollection() : base() { }
        public MailMessageCollection(int capacity) : base(capacity) { }
        public MailMessageCollection(IEnumerable<MailMessage> collections) : base(collections) { }

        protected override bool Filter(MailMessage item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
