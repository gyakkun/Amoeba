using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Io;
using Omnius.Serialization;

namespace Amoeba.Core
{
    sealed class BroadcastMetadatasResultPacket : ItemBase<BroadcastMetadatasResultPacket>
    {
        private enum SerializeId
        {
            BroadcastMetadatas = 0,
        }

        private volatile BroadcastMetadataCollection _broadcastMetadatas;

        public const int MaxMetadataCount = 1024;

        public BroadcastMetadatasResultPacket(IEnumerable<BroadcastMetadata> broadcastMetadatas)
        {
            if (broadcastMetadatas != null) this.ProtectedBroadcastMetadatas.AddRange(broadcastMetadatas);
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.BroadcastMetadatas)
                    {
                        for (int i = reader.GetInt() - 1; i >= 0; i--)
                        {
                            this.ProtectedBroadcastMetadatas.Add(BroadcastMetadata.Import(reader.GetStream(), bufferManager));
                        }
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // BroadcastMetadatas
                if (this.ProtectedBroadcastMetadatas.Count > 0)
                {
                    writer.Write((int)SerializeId.BroadcastMetadatas);
                    writer.Write(this.ProtectedBroadcastMetadatas.Count);

                    foreach (var item in this.ProtectedBroadcastMetadatas)
                    {
                        writer.Write(item.Export(bufferManager));
                    }
                }

                return writer.GetStream();
            }
        }

        private volatile ReadOnlyCollection<BroadcastMetadata> _readOnlyBroadcastMetadatas;

        public IEnumerable<BroadcastMetadata> BroadcastMetadatas
        {
            get
            {
                if (_readOnlyBroadcastMetadatas == null)
                    _readOnlyBroadcastMetadatas = new ReadOnlyCollection<BroadcastMetadata>(this.ProtectedBroadcastMetadatas);

                return _readOnlyBroadcastMetadatas;
            }
        }

        private BroadcastMetadataCollection ProtectedBroadcastMetadatas
        {
            get
            {
                if (_broadcastMetadatas == null)
                    _broadcastMetadatas = new BroadcastMetadataCollection(MaxMetadataCount);

                return _broadcastMetadatas;
            }
        }
    }
}
