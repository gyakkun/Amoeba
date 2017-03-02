using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Amoeba
{
    [Flags]
    [DataContract(Name = "ConnectionTcpType")]
    public enum ConnectionTcpType
    {
        [EnumMember(Value = "None")]
        None = 0,

        [EnumMember(Value = "Ipv4")]
        Ipv4 = 0x01,

        [EnumMember(Value = "Ipv6")]
        Ipv6 = 0x02,
    }

    [DataContract(Name = "ConnectionTcpConfig")]
    public class ConnectionTcpConfig
    {
        private ConnectionTcpType _type;
        private string _socks5ProxyUri;
        private ushort _ipv4Port;
        private ushort _ipv6Port;

        public ConnectionTcpConfig(ConnectionTcpType type, string socks5ProxyUri, ushort ipv4Port, ushort ipv6Port)
        {
            this.Type = type;
            this.Socks5ProxyUri = socks5ProxyUri;
            this.Ipv4Port = ipv4Port;
            this.Ipv6Port = ipv6Port;
        }

        [DataMember(Name = "Type")]
        public ConnectionTcpType Type
        {
            get
            {
                return _type;
            }
            private set
            {
                _type = value;
            }
        }

        [DataMember(Name = "Socks5ProxyUri")]
        public string Socks5ProxyUri
        {
            get
            {
                return _socks5ProxyUri;
            }
            private set
            {
                _socks5ProxyUri = value;
            }
        }

        [DataMember(Name = "Ipv4Port")]
        public ushort Ipv4Port
        {
            get
            {
                return _ipv4Port;
            }
            private set
            {
                _ipv4Port = value;
            }
        }

        [DataMember(Name = "Ipv6Port")]
        public ushort Ipv6Port
        {
            get
            {
                return _ipv6Port;
            }
            private set
            {
                _ipv6Port = value;
            }
        }
    }
}
