using System;

namespace Amoeba.Core
{
    interface IBroadcastMessage
    {
        string Type { get; }
        DateTime CreationTime { get; }
        Metadata Metadata { get; }
    }
}
