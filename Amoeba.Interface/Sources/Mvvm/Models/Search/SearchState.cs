using System;
using System.Runtime.Serialization;

namespace Amoeba.Interface
{
    [Flags]
    [DataContract(Name = nameof(SearchState))]
    enum SearchState
    {
        [EnumMember(Value = nameof(Cache))]
        Cache = 0x1,

        [EnumMember(Value = nameof(Downloading))]
        Downloading = 0x2,

        [EnumMember(Value = nameof(Downloaded))]
        Downloaded = 0x4,
    }
}
