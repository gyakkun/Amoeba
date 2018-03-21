using System;
using System.IO;
using System.Runtime.Serialization;
using Amoeba.Messages;
using Omnius.Base;
using Omnius.Security;
using Omnius.Serialization;
using Omnius.Utils;

namespace Amoeba.Service
{
    sealed partial class BroadcastMetadata : MessageBase<BroadcastMetadata>
    {
        public BroadcastMetadata(string type, DateTime creationTime, Metadata metadata, DigitalSignature digitalSignature)
        {
            this.Type = type;
            this.CreationTime = creationTime;
            this.Metadata = metadata;
            this.Certificate = new Certificate(digitalSignature, this.GetCertificateStream());
        }

        public Stream GetCertificateStream()
        {
            var target = new BroadcastMetadata(this.Type, this.CreationTime, this.Metadata, (Certificate)null);
            return target.Export(BufferManager.Instance);
        }

        public bool VerifyCertificate()
        {
            return this.Certificate.Verify(this.GetCertificateStream());
        }
    }
}
