using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Omnius.Base;
using Omnius.Serialization;

namespace Amoeba.Service
{
    sealed class MulticastMetadatasResultPacket : ItemBase<MulticastMetadatasResultPacket>
    {
        private enum SerializeId
        {
            MulticastMetadatas = 0,
        }

        private volatile MulticastMetadataCollection _multicastMetadatas;

        public const int MaxMetadataCount = 1024;

        private MulticastMetadatasResultPacket() { }

        public MulticastMetadatasResultPacket(IEnumerable<MulticastMetadata> multicastMetadatas)
        {
            if (multicastMetadatas != null) this.ProtectedMulticastMetadatas.AddRange(multicastMetadatas);
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

                    if (id == (int)SerializeId.MulticastMetadatas)
                    {
                        for (int i = (int)reader.GetUInt32() - 1; i >= 0; i--)
                        {
                            this.ProtectedMulticastMetadatas.Add(MulticastMetadata.Import(reader.GetStream(), bufferManager));
                        }
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // MulticastMetadatass
                if (this.ProtectedMulticastMetadatas.Count > 0)
                {
                    writer.Write((uint)SerializeId.MulticastMetadatas);
                    writer.Write((uint)this.ProtectedMulticastMetadatas.Count);

                    foreach (var item in this.ProtectedMulticastMetadatas)
                    {
                        writer.Write(item.Export(bufferManager));
                    }
                }

                return writer.GetStream();
            }
        }

        private volatile ReadOnlyCollection<MulticastMetadata> _readOnlyMulticastMetadatas;

        public IEnumerable<MulticastMetadata> MulticastMetadatas
        {
            get
            {
                if (_readOnlyMulticastMetadatas == null)
                    _readOnlyMulticastMetadatas = new ReadOnlyCollection<MulticastMetadata>(this.ProtectedMulticastMetadatas);

                return _readOnlyMulticastMetadatas;
            }
        }

        private MulticastMetadataCollection ProtectedMulticastMetadatas
        {
            get
            {
                if (_multicastMetadatas == null)
                    _multicastMetadatas = new MulticastMetadataCollection(MaxMetadataCount);

                return _multicastMetadatas;
            }
        }
    }
}
