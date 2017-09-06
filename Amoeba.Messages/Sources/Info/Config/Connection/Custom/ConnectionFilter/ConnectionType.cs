using System.Runtime.Serialization;

namespace Amoeba.Messages
{
    [DataContract(Name = nameof(ConnectionType))]
    public enum ConnectionType
    {
        [EnumMember(Value = nameof(None))]
        None = 0,

        [EnumMember(Value = nameof(Tcp))]
        Tcp = 1,

        [EnumMember(Value = nameof(Socks5Proxy))]
        Socks5Proxy = 2,

        [EnumMember(Value = nameof(HttpProxy))]
        HttpProxy = 3,
    }
}
