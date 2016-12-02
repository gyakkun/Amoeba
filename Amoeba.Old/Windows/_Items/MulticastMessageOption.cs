using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Library;

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

    [DataContract(Name = "MulticastMessageOption")]
    class MulticastMessageOption : IEquatable<MulticastMessageOption>, ICloneable<MulticastMessageOption>, IThisLock
    {
        private int _cost;
        private MulticastMessageState _state;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public override int GetHashCode()
        {
            lock (this.ThisLock)
            {
                return this.Cost.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is MulticastMessageOption)) return false;

            return this.Equals((MulticastMessageOption)obj);
        }

        public bool Equals(MulticastMessageOption other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Cost != other.Cost
                || this.State != other.State)
            {
                return false;
            }

            return true;
        }

        [DataMember(Name = "Cost")]
        public int Cost
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _cost;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _cost = value;
                }
            }
        }

        [DataMember(Name = "State")]
        public MulticastMessageState State
        {
            get
            {
                return _state;
            }
            set
            {
                if (!Enum.IsDefined(typeof(MulticastMessageState), value))
                {
                    throw new ArgumentException();
                }
                else
                {
                    _state = value;
                }
            }
        }

        #region ICloneable<MulticastMessageOption>

        public MulticastMessageOption Clone()
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
