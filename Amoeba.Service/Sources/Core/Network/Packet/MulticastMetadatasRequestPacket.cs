using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Amoeba.Messages;
using Omnius.Base;
using Omnius.Serialization;

namespace Amoeba.Service
{
    sealed class MulticastMetadatasRequestPacket : ItemBase<MulticastMetadatasRequestPacket>
    {
        private enum SerializeId
        {
            Tags = 0,
        }

        private TagCollection _tags;

        public MulticastMetadatasRequestPacket(IEnumerable<Tag> tags)
        {
            if (tags != null) this.ProtectedTags.AddRange(tags);
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

                    if (id == (int)SerializeId.Tags)
                    {
                        for (int i = (int)reader.GetUInt32() - 1; i >= 0; i--)
                        {
                            this.ProtectedTags.Add(Tag.Import(reader.GetStream(), bufferManager));
                        }
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Tags
                if (this.ProtectedTags.Count > 0)
                {
                    writer.Write((uint)SerializeId.Tags);
                    writer.Write((uint)this.ProtectedTags.Count);

                    foreach (var item in this.ProtectedTags)
                    {
                        writer.Write(item.Export(bufferManager));
                    }
                }

                return writer.GetStream();
            }
        }

        private volatile ReadOnlyCollection<Tag> _readOnlyTags;

        public IEnumerable<Tag> Tags
        {
            get
            {
                if (_readOnlyTags == null)
                    _readOnlyTags = new ReadOnlyCollection<Tag>(this.ProtectedTags);

                return _readOnlyTags;
            }
        }

        private TagCollection ProtectedTags
        {
            get
            {
                if (_tags == null)
                    _tags = new TagCollection();

                return _tags;
            }
        }
    }
}
