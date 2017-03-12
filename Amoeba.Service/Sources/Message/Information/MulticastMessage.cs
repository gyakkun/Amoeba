using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Amoeba.Core;
using Omnius.Base;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Service
{
    [DataContract(Name = nameof(MulticastMessage<T>))]
    public sealed class MulticastMessage<T> : ItemBase<MulticastMessage<T>>, IMulticastMessage<T>
        where T : ItemBase<T>
    {
        private enum SerializeId
        {
            Tag = 0,
            AuthorSignature = 1,
            CreationTime = 2,
            Cost = 3,
            Value = 4,
        }

        private Tag _tag;
        private Signature _authorSignature;
        private DateTime _creationTime;
        private Cost _cost;
        private T _value;

        public MulticastMessage(Tag tag, Signature authorSignature, DateTime creationTime, Cost cost, T value)
        {
            this.Tag = tag;
            this.AuthorSignature = authorSignature;
            this.CreationTime = creationTime;
            this.Cost = cost;
            this.Value = value;
        }

        protected override void Initialize()
        {

        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.Tag)
                    {
                        this.Tag = Tag.Import(reader.GetStream(), bufferManager);
                    }
                    else if (id == (int)SerializeId.AuthorSignature)
                    {
                        this.AuthorSignature = Signature.Import(reader.GetStream(), bufferManager);
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
                // Tag
                if (this.Tag != null)
                {
                    writer.Write((int)SerializeId.Tag);
                    writer.Write(this.Tag.Export(bufferManager));
                }
                // AuthorSignature
                if (this.AuthorSignature != null)
                {
                    writer.Write((int)SerializeId.AuthorSignature);
                    writer.Write(this.AuthorSignature.Export(bufferManager));
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
            if ((object)obj == null || !(obj is MulticastMessage<T>)) return false;

            return this.Equals((MulticastMessage<T>)obj);
        }

        public override bool Equals(MulticastMessage<T> other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Tag != other.Tag
                || this.AuthorSignature != other.AuthorSignature
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

        #region IMulticastMessage

        [DataMember(Name = nameof(Tag))]
        public Tag Tag
        {
            get
            {
                return _tag;
            }
            private set
            {
                _tag = value;
            }
        }

        [DataMember(Name = nameof(AuthorSignature))]
        public Signature AuthorSignature
        {
            get
            {
                return _authorSignature;
            }
            private set
            {
                _authorSignature = value;
            }
        }

        [DataMember(Name = nameof(CreationTime))]
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

        [DataMember(Name = nameof(Cost))]
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

        [DataMember(Name = nameof(Value))]
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
