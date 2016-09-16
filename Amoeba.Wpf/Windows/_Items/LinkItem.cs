using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Io;
using Library.Net.Amoeba;
using Library.Security;

namespace Amoeba.Windows
{
    [DataContract(Name = "LinkItem")]
    class LinkItem : IEquatable<LinkItem>, ICloneable<LinkItem>, IThisLock
    {
        private string _signature;
        private SignatureCollection _trustSignatures;
        private SignatureCollection _deleteSignatures;

        private static readonly object _initializeLock = new object();
        private volatile object _thisLock;

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

            if (this.Signature != other.Signature
                || !CollectionUtils.Equals(this.TrustSignatures, other.TrustSignatures)
                || !CollectionUtils.Equals(this.DeleteSignatures, other.DeleteSignatures))
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

        [DataMember(Name = "DeleteSignatures")]
        public SignatureCollection DeleteSignatures
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_deleteSignatures == null)
                        _deleteSignatures = new SignatureCollection();

                    return _deleteSignatures;
                }
            }
        }

        #region ICloneable<LinkItem>

        public LinkItem Clone()
        {
            lock (this.ThisLock)
            {
                return JsonUtils.Clone(this);
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                if (_thisLock == null)
                {
                    lock (_initializeLock)
                    {
                        if (_thisLock == null)
                        {
                            _thisLock = new object();
                        }
                    }
                }

                return _thisLock;
            }
        }

        #endregion
    }
}
