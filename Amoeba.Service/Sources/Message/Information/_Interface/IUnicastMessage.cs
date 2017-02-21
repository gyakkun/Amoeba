using System;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Service
{
    interface IUnicastMessage<T>
        where T : ItemBase<T>
    {
        Signature Signature { get; }
        DateTime CreationTime { get; }
        Cost Cost { get; }
        T Value { get; }
    }
}
