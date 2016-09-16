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
    [DataContract(Name = "MulticastMessageItem")]
    class MulticastMessageItem : IEquatable<MulticastMessageItem>, ICloneable<MulticastMessageItem>, IThisLock
    {
        private Tag _tag;
        private DateTime _creationTime;
        private string _signature;
        private string _comment;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public override int GetHashCode()
        {
            lock (this.ThisLock)
            {
                if (this.Tag == null) return 0;
                else return this.Tag.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is MulticastMessageItem)) return false;

            return this.Equals((MulticastMessageItem)obj);
        }

        public bool Equals(MulticastMessageItem other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Tag != other.Tag
                || this.CreationTime != other.CreationTime
                || this.Signature != other.Signature
                || this.Comment != other.Comment)
            {
                return false;
            }

            return true;
        }

        [DataMember(Name = "Tag")]
        public Tag Tag
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _tag;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _tag = value;
                }
            }
        }

        [DataMember(Name = "CreationTime")]
        public DateTime CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _creationTime;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _creationTime = value;
                }
            }
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

        [DataMember(Name = "Comment")]
        public string Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _comment;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _comment = value;
                }
            }
        }

        #region ICloneable<MulticastMessageItem>

        public MulticastMessageItem Clone()
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
