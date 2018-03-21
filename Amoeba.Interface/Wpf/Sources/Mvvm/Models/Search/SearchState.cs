using System;
using System.Runtime.Serialization;

namespace Amoeba.Interface
{
    [Flags]
    enum SearchState
    {
        Store = 0x01,
        Cache = 0x02,
        Downloading = 0x04,
        Downloaded = 0x08,
    }
}
