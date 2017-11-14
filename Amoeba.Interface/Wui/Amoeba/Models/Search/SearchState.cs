using System;
using System.Runtime.Serialization;

namespace Amoeba.Interface
{
    [Flags]
    [DataContract(Name = nameof(SearchState))]
    public enum SearchState
    {
        [EnumMember(Value = nameof(Store))]
        Store = 0x01,

        [EnumMember(Value = nameof(Cache))]
        Cache = 0x02,

        [EnumMember(Value = nameof(Downloading))]
        Downloading = 0x04,

        [EnumMember(Value = nameof(Downloaded))]
        Downloaded = 0x08,
    }
}
