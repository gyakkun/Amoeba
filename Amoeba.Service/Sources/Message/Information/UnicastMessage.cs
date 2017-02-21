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
    [DataContract(Name = "UnicastMessage")]
    public sealed class UnicastMessage<T> : ItemBase<UnicastMessage<T>>, IUnicastMessage<T>
        where T : ItemBase<T>
    {
        private enum SerializeId
        {
            Signature = 0,
            CreationTime = 1,
            Cost = 2,
            Value = 3,
        }

        private Signature _signature;
        private DateTime _creationTime;
        private Cost _cost;
        private T _value;

        public UnicastMessage(Signature signature, DateTime creationTime, Cost cost, T value)
        {
            this.Signature = signature;
            this.CreationTime = creationTime;
            this.Cost = cost;
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
                    else if (id == (int)SerializeId.Cost)
                    {
                        this.Cost = Cost.Import(reader.GetStream(), bufferManager);
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
                // Cost
                if (this.Cost != null)
                {
                    writer.Write((int)SerializeId.Cost);
                    writer.Write(this.Cost.Export(bufferManager));
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
            if ((object)obj == null || !(obj is UnicastMessage<T>)) return false;

            return this.Equals((UnicastMessage<T>)obj);
        }

        public override bool Equals(UnicastMessage<T> other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Signature != other.Signature
                || this.CreationTime != other.CreationTime
                || this.Cost != other.Cost
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

        #region IUnicastMessage

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

        [DataMember(Name = "Cost")]
        public Cost Cost
        {
            get
            {
                return _cost;
            }
            private set
            {
                _cost = value;
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
