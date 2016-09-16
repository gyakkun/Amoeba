using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Net.Amoeba;
using Library.Security;
using Library.Io;
using Library.Collections;

namespace Amoeba.Windows
{
    [DataContract(Name = "ProfileItem")]
    class ProfileItem : IEquatable<ProfileItem>, ICloneable<ProfileItem>, IThisLock
    {
        private string _signature;
        private ExchangePublicKey _exchangePublicKey;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

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
            if ((object)obj == null || !(obj is ProfileItem)) return false;

            return this.Equals((ProfileItem)obj);
        }

        public bool Equals(ProfileItem other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Signature != other.Signature
                || this.ExchangePublicKey != other.ExchangePublicKey)
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

        [DataMember(Name = "ExchangePublicKey")]
        public ExchangePublicKey ExchangePublicKey
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _exchangePublicKey;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _exchangePublicKey = value;
                }
            }
        }

        #region ICloneable<ProfileItem>

        public ProfileItem Clone()
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
