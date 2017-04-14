using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Amoeba
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

    [DataContract(Name = nameof(TcpConnectionConfig))]
    public class TcpConnectionConfig
    {
        private TcpConnectionType _type;
        private string _proxyUri;
        private ushort _ipv4Port;
        private ushort _ipv6Port;

        private TcpConnectionConfig() { }

        public TcpConnectionConfig(TcpConnectionType type, string proxyUri, ushort ipv4Port, ushort ipv6Port)
        {
            this.Type = type;
            this.ProxyUri = proxyUri;
            this.Ipv4Port = ipv4Port;
            this.Ipv6Port = ipv6Port;
        }

        [DataMember(Name = nameof(Type))]
        public TcpConnectionType Type
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

        [DataMember(Name = nameof(ProxyUri))]
        public string ProxyUri
        {
            get
            {
                return _proxyUri;
            }
            private set
            {
                _proxyUri = value;
            }
        }

        [DataMember(Name = nameof(Ipv4Port))]
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

        [DataMember(Name = nameof(Ipv6Port))]
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
