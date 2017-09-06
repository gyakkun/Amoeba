using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Messages
{
    public sealed class ChatMessageCollection : FilteredList<ChatMessage>
    {
        public ChatMessageCollection() : base() { }
        public ChatMessageCollection(int capacity) : base(capacity) { }
        public ChatMessageCollection(IEnumerable<ChatMessage> collections) : base(collections) { }

        protected override bool Filter(ChatMessage item)
        {
            if (item == null) return true;

            return false;
        }
    }
}
