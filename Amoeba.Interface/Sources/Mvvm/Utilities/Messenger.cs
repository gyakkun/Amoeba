using Prism.Events;

namespace Amoeba.Interface
{
    class RelationWindowShowEvent : PubSubEvent<RelationWindowViewModel> { }
    class OptionsWindowShowEvent : PubSubEvent<OptionsWindowViewModel> { }
    class VersionWindowShowEvent : PubSubEvent<VersionWindowViewModel> { }
    class ChatMessageEditWindowShowEvent : PubSubEvent<ChatMessageEditWindowViewModel> { }
    class SearchInfoEditWindowShowEvent : PubSubEvent<SearchInfoEditWindowViewModel> { }
    class UploadPreviewWindowShowEvent : PubSubEvent<UploadPreviewWindowViewModel> { }

    class NameEditWindowShowEvent : PubSubEvent<NameEditWindowViewModel> { }
    class ConfirmWindowShowEvent : PubSubEvent<ConfirmWindowViewModel> { }
    class NoticeWindowShowEvent : PubSubEvent<NoticeWindowViewModel> { }

    class Messenger : EventAggregator
    {
        public static Messenger Instance { get; } = new Messenger();
    }
}
