using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Io;
using Omnius.Serialization;

namespace Amoeba.Core.Network
{
    sealed class UnicastMessagesResultPacket : ItemBase<UnicastMessagesResultPacket>
    {
        private enum SerializeId
        {
            UnicastMessages = 0,
        }

        private volatile UnicastMessageCollection _unicastMessages;

        public const int MaxMetadataCount = 1024;

        public UnicastMessagesResultPacket(IEnumerable<UnicastMessage> unicastMessages)
        {
            if (unicastMessages != null) this.ProtectedUnicastMessages.AddRange(unicastMessages);
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.UnicastMessages)
                    {
                        for (int i = reader.GetInt() - 1; i >= 0; i--)
                        {
                            this.ProtectedUnicastMessages.Add(UnicastMessage.Import(reader.GetStream(), bufferManager));
                        }
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Seeds
                if (this.ProtectedUnicastMessages.Count > 0)
                {
                    writer.Write((int)SerializeId.UnicastMessages);
                    writer.Write(this.ProtectedUnicastMessages.Count);

                    foreach (var item in this.ProtectedUnicastMessages)
                    {
                        writer.Write(item.Export(bufferManager));
                    }
                }

                return writer.GetStream();
            }
        }

        private volatile ReadOnlyCollection<UnicastMessage> _readOnlyUnicastMessages;

        public IEnumerable<UnicastMessage> UnicastMessages
        {
            get
            {
                if (_readOnlyUnicastMessages == null)
                    _readOnlyUnicastMessages = new ReadOnlyCollection<UnicastMessage>(this.ProtectedUnicastMessages);

                return _readOnlyUnicastMessages;
            }
        }

        private UnicastMessageCollection ProtectedUnicastMessages
        {
            get
            {
                if (_unicastMessages == null)
                    _unicastMessages = new UnicastMessageCollection(MaxMetadataCount);

                return _unicastMessages;
            }
        }
    }
}
