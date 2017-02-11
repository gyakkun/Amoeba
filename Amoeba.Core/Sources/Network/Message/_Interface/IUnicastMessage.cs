using System;
using Omnius.Security;

namespace Amoeba.Core
{
    interface IUnicastMessage
    {
        string Type { get; }
        Signature Signature { get; }
        DateTime CreationTime { get; }
        Metadata Metadata { get; }
    }
}
