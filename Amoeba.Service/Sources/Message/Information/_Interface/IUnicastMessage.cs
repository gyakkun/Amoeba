using System;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Service
{
    interface IUnicastMessage<T>
        where T : ItemBase<T>
    {
        Signature TargetSignature { get; }
        Signature AuthorSignature { get; }
        DateTime CreationTime { get; }
        T Value { get; }
    }
}
