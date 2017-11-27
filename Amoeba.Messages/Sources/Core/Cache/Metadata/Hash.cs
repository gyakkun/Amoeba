using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Security;
using Omnius.Serialization;
using Omnius.Utilities;

namespace Amoeba.Messages
{
    [DataContract(Name = nameof(Hash))]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Hash : IHash, IEquatable<Hash>
    {
        private HashAlgorithm _algorithm;
        private byte[] _value;

        public static readonly int MaxValueLength = 32;

        public Hash(HashAlgorithm algorithm, byte[] value)
        {
            _algorithm = 0;
            _value = null;

            this.Algorithm = algorithm;
            this.Value = value;
        }

        public static bool operator ==(Hash x, Hash y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Hash x, Hash y)
        {
            return !x.Equals(y);
        }

        public override int GetHashCode()
        {
            return ((int)this.Algorithm) ^ Fnv1.ComputeHash32(this.Value);
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is Hash)) return false;

            return this.Equals((Hash)obj);
        }

        public bool Equals(Hash other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Algorithm != other.Algorithm
                || (this.Value == null) != (other.Value == null))
            {
                return false;
            }

            if (this.Value != null && other.Value != null)
            {
                if (!Unsafe.Equals(this.Value, other.Value)) return false;
            }

            return true;
        }

        #region IHash

        [DataMember(Name = nameof(Algorithm))]
        public HashAlgorithm Algorithm
        {
            get
            {
                return _algorithm;
            }
            private set
            {
                if (!Enum.IsDefined(typeof(HashAlgorithm), value))
                {
                    throw new ArgumentException();
                }
                else
                {
                    _algorithm = value;
                }
            }
        }

        [DataMember(Name = nameof(Value))]
        public byte[] Value
        {
            get
            {
                return _value;
            }
            private set
            {
                if (value != null && value.Length > Hash.MaxValueLength)
                {
                    throw new ArgumentException();
                }
                else
                {
                    _value = value;
                }
            }
        }

        #endregion
    }
}
