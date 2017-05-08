using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    [Flags]
    [DataContract(Name = nameof(ChatMessageState))]
    enum ChatMessageState
    {
        [EnumMember(Value = "None")]
        None = 0,

        [EnumMember(Value = "New")]
        New = 0x01,
    }
}
