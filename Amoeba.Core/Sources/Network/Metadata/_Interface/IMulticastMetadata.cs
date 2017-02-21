using System;

namespace Amoeba.Core
{
    interface IMulticastMetadata
    {
        string Type { get; }
        Tag Tag { get; }
        DateTime CreationTime { get; }
        Metadata Metadata { get; }
    }
}
