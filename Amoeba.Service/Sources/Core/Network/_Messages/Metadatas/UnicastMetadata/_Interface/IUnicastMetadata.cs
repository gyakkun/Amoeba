using System;
using Amoeba.Messages;
using Omnius.Security;

namespace Amoeba.Service
{
    interface IUnicastMetadata
    {
        string Type { get; }
        Signature Signature { get; }
        DateTime CreationTime { get; }
        Metadata Metadata { get; }
    }
}
