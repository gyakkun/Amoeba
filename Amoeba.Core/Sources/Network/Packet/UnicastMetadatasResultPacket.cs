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
    sealed class UnicastMetadatasResultPacket : ItemBase<UnicastMetadatasResultPacket>
    {
        private enum SerializeId
        {
            UnicastMetadatas = 0,
        }

        private volatile UnicastMetadataCollection _unicastMetadatas;

        public const int MaxMetadataCount = 1024;

        public UnicastMetadatasResultPacket(IEnumerable<UnicastMetadata> unicastMetadatas)
        {
            if (unicastMetadatas != null) this.ProtectedUnicastMetadatas.AddRange(unicastMetadatas);
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
                    if (id == (int)SerializeId.UnicastMetadatas)
                    {
                        for (int i = reader.GetInt() - 1; i >= 0; i--)
                        {
                            this.ProtectedUnicastMetadatas.Add(UnicastMetadata.Import(reader.GetStream(), bufferManager));
                        }
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // UnicastMetadatas
                if (this.ProtectedUnicastMetadatas.Count > 0)
                {
                    writer.Write((int)SerializeId.UnicastMetadatas);
                    writer.Write(this.ProtectedUnicastMetadatas.Count);

                    foreach (var item in this.ProtectedUnicastMetadatas)
                    {
                        writer.Write(item.Export(bufferManager));
                    }
                }

                return writer.GetStream();
            }
        }

        private volatile ReadOnlyCollection<UnicastMetadata> _readOnlyUnicastMetadatas;

        public IEnumerable<UnicastMetadata> UnicastMetadatas
        {
            get
            {
                if (_readOnlyUnicastMetadatas == null)
                    _readOnlyUnicastMetadatas = new ReadOnlyCollection<UnicastMetadata>(this.ProtectedUnicastMetadatas);

                return _readOnlyUnicastMetadatas;
            }
        }

        private UnicastMetadataCollection ProtectedUnicastMetadatas
        {
            get
            {
                if (_unicastMetadatas == null)
                    _unicastMetadatas = new UnicastMetadataCollection(MaxMetadataCount);

                return _unicastMetadatas;
            }
        }
    }
}
