using System;
using System.Runtime.Serialization;

namespace Amoeba.Messages
{
    [DataContract(Name = "ConnectionFilter")]
    public sealed class ConnectionFilter : IEquatable<ConnectionFilter>
    {
        private string _scheme;
        private ConnectionType _type;
        private string _proxyUri;

        public ConnectionFilter(string scheme, ConnectionType connectionType, string proxyUri)
        {
            this.Scheme = scheme;
            this.Type = connectionType;
            this.ProxyUri = proxyUri;
        }

        public override int GetHashCode()
        {
            return this.Scheme?.GetHashCode() ?? 0 ^ this.Type.GetHashCode() ^ this.ProxyUri?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is ConnectionFilter)) return false;

            return this.Equals((ConnectionFilter)obj);
        }

        public bool Equals(ConnectionFilter other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Scheme != other.Scheme
                || this.Type != other.Type
                || this.ProxyUri != other.ProxyUri)
            {
                return false;
            }

            return true;
        }

        [DataMember(Name = nameof(Scheme))]
        public string Scheme
        {
            get
            {
                return _scheme;
            }
            private set
            {
                _scheme = value;
            }
        }

        [DataMember(Name = nameof(Type))]
        public ConnectionType Type
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
    }
}
