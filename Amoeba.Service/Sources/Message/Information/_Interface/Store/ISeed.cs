using System;
using System.Collections.Generic;
using Amoeba.Core;

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
