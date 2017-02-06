using System;
using System.Collections.Generic;
using Omnius.Net.Amoeba;

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
