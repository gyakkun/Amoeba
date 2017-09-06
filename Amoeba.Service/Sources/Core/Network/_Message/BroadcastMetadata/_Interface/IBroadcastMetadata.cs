using System;
using Amoeba.Messages;

namespace Amoeba.Service
{
    interface IBroadcastMetadata
    {
        string Type { get; }
        DateTime CreationTime { get; }
        Metadata Metadata { get; }
    }
}
