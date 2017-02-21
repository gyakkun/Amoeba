using System;

namespace Amoeba.Core
{
    interface IBroadcastMetadata
    {
        string Type { get; }
        DateTime CreationTime { get; }
        Metadata Metadata { get; }
    }
}
