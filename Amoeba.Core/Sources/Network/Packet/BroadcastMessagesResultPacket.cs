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
    sealed class BroadcastMessagesResultPacket : ItemBase<BroadcastMessagesResultPacket>
    {
        private enum SerializeId
        {
            BroadcastMessages = 0,
        }

        private volatile BroadcastMessageCollection _broadcastMessages;

        public const int MaxMetadataCount = 1024;

        public BroadcastMessagesResultPacket(IEnumerable<BroadcastMessage> broadcastMessages)
        {
            if (broadcastMessages != null) this.ProtectedBroadcastMessages.AddRange(broadcastMessages);
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.BroadcastMessages)
                    {
                        for (int i = reader.GetInt() - 1; i >= 0; i--)
                        {
                            this.ProtectedBroadcastMessages.Add(BroadcastMessage.Import(reader.GetStream(), bufferManager));
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
                if (this.ProtectedBroadcastMessages.Count > 0)
                {
                    writer.Write((int)SerializeId.BroadcastMessages);
                    writer.Write(this.ProtectedBroadcastMessages.Count);

                    foreach (var item in this.ProtectedBroadcastMessages)
                    {
                        writer.Write(item.Export(bufferManager));
                    }
                }

                return writer.GetStream();
            }
        }

        private volatile ReadOnlyCollection<BroadcastMessage> _readOnlyBroadcastMessages;

        public IEnumerable<BroadcastMessage> BroadcastMessages
        {
            get
            {
                if (_readOnlyBroadcastMessages == null)
                    _readOnlyBroadcastMessages = new ReadOnlyCollection<BroadcastMessage>(this.ProtectedBroadcastMessages);

                return _readOnlyBroadcastMessages;
            }
        }

        private BroadcastMessageCollection ProtectedBroadcastMessages
        {
            get
            {
                if (_broadcastMessages == null)
                    _broadcastMessages = new BroadcastMessageCollection(MaxMetadataCount);

                return _broadcastMessages;
            }
        }
    }
}
