using System;
using Amoeba.Messages;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Messages
{
    interface IMulticastMessage<T>
        where T : ItemBase<T>
    {
        Tag Tag { get; }
        Signature AuthorSignature { get; }
        DateTime CreationTime { get; }
        Cost Cost { get; }
        T Value { get; }
    }
}
