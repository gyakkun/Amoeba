using System;
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
