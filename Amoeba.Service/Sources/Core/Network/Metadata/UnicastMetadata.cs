using System;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Service
{
    [DataContract(Name = nameof(UnicastMetadata))]
    public sealed class UnicastMetadata : CertificateItemBase<UnicastMetadata>, IUnicastMetadata
    {
        private enum SerializeId
        {
            Type = 0,
            Signature = 1,
            CreationTime = 2,
            Metadata = 3,

            Certificate = 4,
        }

        private string _type;
        private Signature _signature;
        private DateTime _creationTime;
        private Metadata _metadata;

        private Certificate _certificate;

        public static readonly int MaxTypeLength = 256;

        private UnicastMetadata() { }

        public UnicastMetadata(string type, Signature signature, DateTime creationTime, Metadata metadata, DigitalSignature digitalSignature)
        {
            this.Type = type;
            this.Signature = signature;
            this.CreationTime = creationTime;
            this.Metadata = metadata;

            this.CreateCertificate(digitalSignature);
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

                    if (id == (int)SerializeId.Type)
                    {
                        this.Type = reader.GetString();
                    }
                    else if (id == (int)SerializeId.Signature)
                    {
                        this.Signature = Signature.Import(reader.GetStream(), bufferManager);
                    }
                    else if (id == (int)SerializeId.CreationTime)
                    {
                        this.CreationTime = reader.GetDateTime();
                    }
                    else if (id == (int)SerializeId.Metadata)
                    {
                        this.Metadata = Metadata.Import(reader.GetStream(), bufferManager);
                    }

                    else if (id == (int)SerializeId.Certificate)
                    {
                        this.Certificate = Certificate.Import(reader.GetStream(), bufferManager);
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Type
                if (this.Type != null)
                {
                    writer.Write((uint)SerializeId.Type);
                    writer.Write(this.Type);
                }
                // Signature
                if (this.Signature != null)
                {
                    writer.Write((uint)SerializeId.Signature);
                    writer.Write(this.Signature.Export(bufferManager));
                }
                // CreationTime
                if (this.CreationTime != DateTime.MinValue)
                {
                    writer.Write((uint)SerializeId.CreationTime);
                    writer.Write(this.CreationTime);
                }
                // Metadata
                if (this.Metadata != null)
                {
                    writer.Write((uint)SerializeId.Metadata);
                    writer.Write(this.Metadata.Export(bufferManager));
                }

                // Certificate
                if (this.Certificate != null)
                {
                    writer.Write((uint)SerializeId.Certificate);
                    writer.Write(this.Certificate.Export(bufferManager));
                }

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            if (this.Metadata == null) return 0;
            else return this.Metadata.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is UnicastMetadata)) return false;

            return this.Equals((UnicastMetadata)obj);
        }

        public override bool Equals(UnicastMetadata other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Type != other.Type
                || this.Signature != other.Signature
                || this.CreationTime != other.CreationTime
                || this.Metadata != other.Metadata

                || this.Certificate != other.Certificate)
            {
                return false;
            }

            return true;
        }

        protected override Stream GetCertificateStream()
        {
            var temp = this.Certificate;
            this.Certificate = null;

            try
            {
                return this.Export(BufferManager.Instance);
            }
            finally
            {
                this.Certificate = temp;
            }
        }

        [DataMember(Name = nameof(Certificate))]
        public override Certificate Certificate
        {
            get
            {
                return _certificate;
            }
            protected set
            {
                _certificate = value;
            }
        }

        #region IUnicastMetadata

        [DataMember(Name = nameof(Type))]
        public string Type
        {
            get
            {
                return _type;
            }
            private set
            {
                if (value != null && value.Length > UnicastMetadata.MaxTypeLength)
                {
                    throw new ArgumentException();
                }
                else
                {
                    _type = value;
                }
            }
        }

        [DataMember(Name = nameof(Signature))]
        public Signature Signature
        {
            get
            {
                return _signature;
            }
            private set
            {
                _signature = value;
            }
        }

        [DataMember(Name = nameof(CreationTime))]
        public DateTime CreationTime
        {
            get
            {
                return _creationTime;
            }
            private set
            {
                var utc = value.ToUniversalTime();
                _creationTime = utc.AddTicks(-(utc.Ticks % TimeSpan.TicksPerSecond));
            }
        }

        [DataMember(Name = nameof(Metadata))]
        public Metadata Metadata
        {
            get
            {
                return _metadata;
            }
            private set
            {
                _metadata = value;
            }
        }

        #endregion
    }
}
