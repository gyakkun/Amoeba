using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    class RelationWindowShowEvent : PubSubEvent<RelationWindowViewModel> { }
    class OptionsWindowShowEvent : PubSubEvent<OptionsWindowViewModel> { }
    class ChatMessageEditWindowShowEvent : PubSubEvent<ChatMessageEditWindowViewModel> { }
    class PublishDirectoryInfoEditWindowShowEvent : PubSubEvent<PublishDirectoryInfoEditWindowViewModel> { }
    class PublishPreviewWindowShowEvent : PubSubEvent<PublishPreviewWindowViewModel> { }
    class NameEditWindowShowEvent : PubSubEvent<NameEditWindowViewModel> { }
    class ConfirmWindowShowEvent : PubSubEvent<ConfirmWindowViewModel> { }

    class Messenger : EventAggregator
    {
        public static Messenger Instance { get; } = new Messenger();
    }
}
