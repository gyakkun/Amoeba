using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Net.Amoeba;
using Library.Security;
using System;

namespace Amoeba.Windows
{
    [DataContract(Name = "LinkItem", Namespace = "http://Amoeba/Windows")]
    class LinkItem : IEquatable<LinkItem>, IDeepCloneable<LinkItem>, IThisLock
    {
        private string _signature = null;
        private SignatureCollection _trustSignatures = null;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        public override int GetHashCode()
        {
            lock (this.ThisLock)
            {
                if (this.Signature == null) return 0;
                else return this.Signature.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is LinkItem)) return false;

            return this.Equals((LinkItem)obj);
        }

        public bool Equals(LinkItem other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.Signature != other.Signature
                || !Collection.Equals(this.TrustSignatures, other.TrustSignatures))
            {
                return false;
            }

            return true;
        }

        [DataMember(Name = "Signature")]
        public string Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _signature;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _signature = value;
                }
            }
        }

        [DataMember(Name = "TrustSignatures")]
        public SignatureCollection TrustSignatures
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_trustSignatures == null)
                        _trustSignatures = new SignatureCollection();

                    return _trustSignatures;
                }
            }
        }

        #region IDeepClone<LinkItem>

        public LinkItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(LinkItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (LinkItem)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }
}
