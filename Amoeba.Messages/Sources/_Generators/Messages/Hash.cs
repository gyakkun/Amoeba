using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Omnius.Base;
using Omnius.Io;
using Omnius.Serialization;
using Omnius.Utils;

namespace Amoeba.Messages
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public readonly struct Hash : IEquatable<Hash>, IMessageSize
    {
        public static IMessageFormatter<Hash> Formatter { get; private set; }

        public static readonly int MaxValueLength = 32;

        static Hash()
        {
            Formatter = new CustomFormatter();
        }

        [JsonConstructor]
        public Hash(HashAlgorithm algorithm, byte[] value)
        {
            if (value == null) throw new ArgumentNullException("value");
            if (value.Length > MaxValueLength) throw new ArgumentOutOfRangeException("value");
            this.Algorithm = algorithm;
            this.Value = value;
        }

        #region Properties

        [JsonProperty]
        public HashAlgorithm Algorithm { get; }

        [JsonProperty]
        public byte[] Value { get; }

        #endregion

        public static bool operator ==(Hash x, Hash y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Hash x, Hash y)
        {
            return !x.Equals(y);
        }

        public override bool Equals(object other)
        {
            if (!(other is Hash)) return false;
            return this.Equals((Hash)other);
        }

        public bool Equals(Hash other)
        {
            if ((object)other == null) return false;
            if (Object.ReferenceEquals(this, other)) return true;

            if (this.Algorithm != other.Algorithm) return false;
            if ((this.Value == null) != (other.Value == null)) return false;
            if ((this.Value != null && other.Value != null)
                && !Unsafe.Equals(this.Value, other.Value)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return ((int)this.Algorithm) ^ ItemUtils.GetHashCode(this.Value);
        }

        public long GetMessageSize()
        {
            long result = 0;
            result += MessageSizeComputer.GetSize((uint)this.Algorithm);
            result += MessageSizeComputer.GetSize(this.Value);

            return result;
        }

        private sealed class CustomFormatter : IMessageFormatter<Hash>
        {
            public void Serialize(MessageStreamWriter w, Hash value, int rank)
            {
                if (rank > 256) throw new FormatException();

                w.Write((ulong)value.Algorithm);
                w.Write(value.Value);
            }
            public Hash Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();

                var p_algorithm = (HashAlgorithm)r.GetUInt64();
                var p_value = r.GetBytes(MaxValueLength);

                return new Hash(p_algorithm, p_value);
            }
        }
    }
}
