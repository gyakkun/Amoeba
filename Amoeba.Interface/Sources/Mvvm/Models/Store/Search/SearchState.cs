using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    [Flags]
    [DataContract(Name = nameof(SearchState))]
    enum SearchState
    {
        [EnumMember(Value = nameof(Cache))]
        Cache = 0x1,

        [EnumMember(Value = nameof(Download))]
        Download = 0x2,
    }
}
