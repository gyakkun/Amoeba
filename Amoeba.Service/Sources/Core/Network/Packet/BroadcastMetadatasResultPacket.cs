using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Amoeba.Messages;
using Omnius.Base;
using Omnius.Serialization;

namespace Amoeba.Service
{
    sealed class BroadcastMetadatasResultPacket : ItemBase<BroadcastMetadatasResultPacket>
    {
        private enum SerializeId
        {
            BroadcastMetadatas = 0,
        }

        private volatile BroadcastMetadataCollection _broadcastMetadatas;

        public BroadcastMetadatasResultPacket(IEnumerable<BroadcastMetadata> broadcastMetadatas)
        {
            if (broadcastMetadatas != null) this.ProtectedBroadcastMetadatas.AddRange(broadcastMetadatas);
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

                    if (id == (int)SerializeId.BroadcastMetadatas)
                    {
                        for (int i = (int)reader.GetUInt32() - 1; i >= 0; i--)
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
                    writer.Write((uint)SerializeId.BroadcastMetadatas);
                    writer.Write((uint)this.ProtectedBroadcastMetadatas.Count);

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
                    _broadcastMetadatas = new BroadcastMetadataCollection();

                return _broadcastMetadatas;
            }
        }
    }
}
