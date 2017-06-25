using System;

namespace Amoeba.Service
{
    interface ISeed
    {
        string Name { get; }
        long Length { get; }
        DateTime CreationTime { get; }
        Metadata Metadata { get; }
    }
}
