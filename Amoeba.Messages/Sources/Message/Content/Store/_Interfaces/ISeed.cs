using System;

namespace Amoeba.Messages
{
    interface ISeed
    {
        string Name { get; }
        long Length { get; }
        DateTime CreationTime { get; }
        Metadata Metadata { get; }
    }
}
