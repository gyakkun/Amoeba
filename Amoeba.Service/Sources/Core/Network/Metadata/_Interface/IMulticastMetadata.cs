using System;

namespace Amoeba.Service
{
    interface IMulticastMetadata
    {
        string Type { get; }
        Tag Tag { get; }
        DateTime CreationTime { get; }
        Metadata Metadata { get; }
    }
}
