using System;
using System.Runtime.Serialization;

namespace Amoeba.Messages
{
    [Flags]
    [DataContract(Name = nameof(TcpConnectionType))]
    public enum TcpConnectionType
    {
        [EnumMember(Value = "None")]
        None = 0,

        [EnumMember(Value = "Ipv4")]
        Ipv4 = 0x01,

        [EnumMember(Value = "Ipv6")]
        Ipv6 = 0x02,
    }
}
