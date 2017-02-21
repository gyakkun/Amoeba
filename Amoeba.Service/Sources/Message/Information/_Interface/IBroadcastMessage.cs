using System;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Service
{
    interface IBroadcastMessage<T>
        where T : ItemBase<T>
    {
        Signature Signature { get; }
        DateTime CreationTime { get; }
        T Value { get; }
    }
}
