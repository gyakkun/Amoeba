using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Omnius.Base;
using Omnius.Security;
using Omnius.Serialization;
using Omnius.Utilities;

namespace Amoeba.Messages
{
    [DataContract(Name = nameof(Hash))]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct Hash : IHash, IEquatable<Hash>
    {
        private readonly HashAlgorithm _algorithm;
        private readonly byte[] _value;

        public static readonly int MaxValueLength = 32;

        [JsonConstructor]
        public Hash(HashAlgorithm algorithm, byte[] value)
        {
            if (!Enum.IsDefined(typeof(HashAlgorithm), algorithm)) throw new ArgumentException(nameof(algorithm));
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Length > Hash.MaxValueLength) throw new ArgumentOutOfRangeException(nameof(value));

            _algorithm = algorithm;
            _value = value;
        }

        public static Hash Import(Stream stream, BufferManager bufferManager)
        {
            using (var reader = new ItemStreamReader(stream, BufferManager.Instance))
            {
                var algorithm = (HashAlgorithm)reader.GetUInt32();
                var value = reader.GetBytes();

                return new Hash(algorithm, value);
            }
        }

        public Stream Export(BufferManager bufferManager)
        {
            using (var writer = new ItemStreamWriter(BufferManager.Instance))
            {
                writer.Write((uint)this.Algorithm);
                writer.Write(this.Value);

                return writer.GetStream();
            }
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
            return ((int)this.Algorithm) ^ ItemUtils.GetHashCode(this.Value);
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
        }

        [DataMember(Name = nameof(Value))]
        public byte[] Value
        {
            get
            {
                return _value;
            }
        }

        #endregion
    }
}
