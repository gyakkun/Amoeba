using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Service
{
    [DataContract(Name = "BroadcastMessage")]
    public sealed class BroadcastMessage<T> : ItemBase<BroadcastMessage<T>>, IBroadcastMessage<T>
        where T : ItemBase<T>
    {
        private enum SerializeId
        {
            Signature = 0,
            CreationTime = 1,
            Value = 2,
        }

        private Signature _signature;
        private DateTime _creationTime;
        private T _value;

        public BroadcastMessage(Signature signature, DateTime creationTime, T value)
        {
            this.Signature = signature;
            this.CreationTime = creationTime;
            this.Value = value;
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.Signature)
                    {
                        this.Signature = Signature.Import(reader.GetStream(), bufferManager);
                    }
                    else if (id == (int)SerializeId.CreationTime)
                    {
                        this.CreationTime = reader.GetDateTime();
                    }
                    else if (id == (int)SerializeId.Value)
                    {
                        this.Value = ItemBase<T>.Import(reader.GetStream(), bufferManager);
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Signature
                if (this.Signature != null)
                {
                    writer.Write((int)SerializeId.Signature);
                    writer.Write(this.Signature.Export(bufferManager));
                }
                // CreationTime
                if (this.CreationTime != DateTime.MinValue)
                {
                    writer.Write((int)SerializeId.CreationTime);
                    writer.Write(this.CreationTime);
                }
                // Value
                if (this.Value != null)
                {
                    writer.Write((int)SerializeId.Value);
                    writer.Write(this.Value.Export(bufferManager));
                }

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            if (this.Value == null) return 0;
            else return this.Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is BroadcastMessage<T>)) return false;

            return this.Equals((BroadcastMessage<T>)obj);
        }

        public override bool Equals(BroadcastMessage<T> other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Signature != other.Signature
                || this.CreationTime != other.CreationTime
                || this.Value != other.Value)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return this.Value.ToString();
        }

        #region IBroadcastMessage

        [DataMember(Name = "Signature")]
        public Signature Signature
        {
            get
            {
                return _signature;
            }
            private set
            {
                _signature = value;
            }
        }

        [DataMember(Name = "CreationTime")]
        public DateTime CreationTime
        {
            get
            {
                return _creationTime;
            }
            private set
            {
                var utc = value.ToUniversalTime();
                _creationTime = utc.AddTicks(-(utc.Ticks % TimeSpan.TicksPerSecond));
            }
        }

        [DataMember(Name = "Value")]
        public T Value
        {
            get
            {
                return _value;
            }
            private set
            {
                _value = value;
            }
        }

        #endregion
    }
}
