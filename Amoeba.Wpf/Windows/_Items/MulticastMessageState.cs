using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Amoeba.Windows
{
    [Flags]
    [DataContract(Name = "MulticastMessageState")]
    enum MulticastMessageState
    {
        [EnumMember(Value = "None")]
        None = 0,

        [EnumMember(Value = "IsUnread")]
        IsUnread = 0x01,

        [EnumMember(Value = "IsLocked")]
        IsLocked = 0x02,
    }
}
