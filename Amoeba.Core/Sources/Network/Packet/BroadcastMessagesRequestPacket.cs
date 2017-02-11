using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Io;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Core.Network
{
    sealed class BroadcastMessagesRequestPacket : ItemBase<BroadcastMessagesRequestPacket>
    {
        private enum SerializeId
        {
            Signatures = 0,
        }

        private SignatureCollection _signatures;

        public const int MaxMetadataRequestCount = 1024;

        public BroadcastMessagesRequestPacket(IEnumerable<Signature> signatures)
        {
            if (signatures != null) this.ProtectedSignatures.AddRange(signatures);
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.Signatures)
                    {
                        for (int i = reader.GetInt() - 1; i >= 0; i--)
                        {
                            this.ProtectedSignatures.Add(Signature.Import(reader.GetStream(), bufferManager));
                        }
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Signatures
                if (this.ProtectedSignatures.Count > 0)
                {
                    writer.Write((int)SerializeId.Signatures);
                    writer.Write(this.ProtectedSignatures.Count);

                    foreach (var item in this.ProtectedSignatures)
                    {
                        writer.Write(item.Export(bufferManager));
                    }
                }

                return writer.GetStream();
            }
        }

        private volatile ReadOnlyCollection<Signature> _readOnlySignatures;

        public IEnumerable<Signature> Signatures
        {
            get
            {
                if (_readOnlySignatures == null)
                    _readOnlySignatures = new ReadOnlyCollection<Signature>(this.ProtectedSignatures);

                return _readOnlySignatures;
            }
        }

        private SignatureCollection ProtectedSignatures
        {
            get
            {
                if (_signatures == null)
                    _signatures = new SignatureCollection(MaxMetadataRequestCount);

                return _signatures;
            }
        }
    }
}
