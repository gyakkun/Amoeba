using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Io;
using Omnius.Serialization;

namespace Amoeba.Service
{
    sealed class BlocksLinkPacket : ItemBase<BlocksLinkPacket>
    {
        private enum SerializeId
        {
            Hashes = 0,
        }

        private HashCollection _hashes;

        public const int MaxBlockLinkCount = 8192;

        private BlocksLinkPacket() { }

        public BlocksLinkPacket(IEnumerable<Hash> hashes)
        {
            if (hashes != null) this.ProtectedHashes.AddRange(hashes);
        }

        protected override void Initialize()
        {

        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                while (reader.Available > 0)
                {
                    int id = (int)reader.GetUInt32();

                    if (id == (int)SerializeId.Hashes)
                    {
                        for (int i = (int)reader.GetUInt32() - 1; i >= 0; i--)
                        {
                            this.ProtectedHashes.Add(Hash.Import(reader.GetStream(), bufferManager));
                        }
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Hashes
                if (this.ProtectedHashes.Count > 0)
                {
                    writer.Write((uint)SerializeId.Hashes);
                    writer.Write((uint)this.ProtectedHashes.Count);

                    foreach (var item in this.ProtectedHashes)
                    {
                        writer.Write(item.Export(bufferManager));
                    }
                }

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            if (this.ProtectedHashes.Count == 0) return 0;
            else return this.ProtectedHashes[0].GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is BlocksLinkPacket)) return false;

            return this.Equals((BlocksLinkPacket)obj);
        }

        public override bool Equals(BlocksLinkPacket other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (!CollectionUtils.Equals(this.Hashes, other.Hashes))
            {
                return false;
            }

            return true;
        }

        #region IBlocksLinkPacket<Hash>

        private volatile ReadOnlyCollection<Hash> _readOnlyHashes;

        public IEnumerable<Hash> Hashes
        {
            get
            {
                if (_readOnlyHashes == null)
                    _readOnlyHashes = new ReadOnlyCollection<Hash>(this.ProtectedHashes);

                return _readOnlyHashes;
            }
        }

        private HashCollection ProtectedHashes
        {
            get
            {
                if (_hashes == null)
                    _hashes = new HashCollection(MaxBlockLinkCount);

                return _hashes;
            }
        }

        #endregion
    }
}
