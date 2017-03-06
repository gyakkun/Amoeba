using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Io;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Core
{
    [DataContract(Name = nameof(Metadata))]
    public sealed class Metadata : ItemBase<Metadata>, IMetadata
    {
        private enum SerializeId
        {
            Depth = 0,
            Hash = 1,
        }

        private int _depth;
        private Hash _hash;

        public Metadata(int depth, Hash hash)
        {
            this.Depth = depth;
            this.Hash = hash;
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.Depth)
                    {
                        this.Depth = reader.GetInt();
                    }
                    else if (id == (int)SerializeId.Hash)
                    {
                        this.Hash = Hash.Import(reader.GetStream(), bufferManager);
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Depth
                if (this.Depth != 0)
                {
                    writer.Write((int)SerializeId.Depth);
                    writer.Write(this.Depth);
                }
                // Hash
                if (this.Hash != null)
                {
                    writer.Write((int)SerializeId.Hash);
                    writer.Write(this.Hash.Export(bufferManager));
                }

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            if (this.Hash == null) return 0;
            else return this.Hash.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is Metadata)) return false;

            return this.Equals((Metadata)obj);
        }

        public override bool Equals(Metadata other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Depth != other.Depth
                || this.Hash != other.Hash)
            {
                return false;
            }

            return true;
        }

        #region IMetadata

        [DataMember(Name = nameof(Depth))]
        public int Depth
        {
            get
            {
                return _depth;
            }
            private set
            {
                _depth = value;
            }
        }

        [DataMember(Name = nameof(Hash))]
        public Hash Hash
        {
            get
            {
                return _hash;
            }
            private set
            {
                _hash = value;
            }
        }

        #endregion
    }
}
