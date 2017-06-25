using System;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Serialization;
using Omnius.Utilities;

namespace Amoeba.Service
{
    [DataContract(Name = nameof(Tag))]
    public sealed class Tag : ItemBase<Tag>, ITag
    {
        private enum SerializeId
        {
            Name = 0,
            Id = 1,
        }

        private string _name;
        private byte[] _id;

        private int _hashCode;

        public static readonly int MaxNameLength = 256;
        public static readonly int MaxIdLength = 32;

        private Tag() { }

        public Tag(string name, byte[] id)
        {
            this.Name = name;
            this.Id = id;

            this.Initialize();
        }

        protected override void Initialize()
        {
            _hashCode = (this.Id != null) ? ItemUtils.GetHashCode(this.Id) : 0;
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                while (reader.Available > 0)
                {
                    int id = (int)reader.GetUInt32();

                    if (id == (int)SerializeId.Name)
                    {
                        this.Name = reader.GetString();
                    }
                    else if (id == (int)SerializeId.Id)
                    {
                        this.Id = reader.GetBytes();
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Name
                if (this.Name != null)
                {
                    writer.Write((uint)SerializeId.Name);
                    writer.Write(this.Name);
                }
                // Id
                if (this.Id != null)
                {
                    writer.Write((uint)SerializeId.Id);
                    writer.Write(this.Id);
                }

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is Tag)) return false;

            return this.Equals((Tag)obj);
        }

        public override bool Equals(Tag other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Name != other.Name
                || (this.Id == null) != (other.Id == null))
            {
                return false;
            }

            if (this.Id != null && other.Id != null)
            {
                if (!Unsafe.Equals(this.Id, other.Id)) return false;
            }

            return true;
        }

        public override string ToString()
        {
            return this.Name;
        }

        #region ITag

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            private set
            {
                if (value != null && (value.Length > Tag.MaxNameLength || value.Contains("#")))
                {
                    throw new ArgumentException();
                }
                else
                {
                    _name = value;
                }
            }
        }

        [DataMember(Name = nameof(Id))]
        public byte[] Id
        {
            get
            {
                return _id;
            }
            private set
            {
                if (value != null && (value.Length > Tag.MaxIdLength))
                {
                    throw new ArgumentException();
                }
                else
                {
                    _id = value;
                }
            }
        }

        #endregion
    }
}
