using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using Omnius.Base;
using Omnius.Io;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Service
{
    [DataContract(Name = nameof(MulticastMetadata))]
    public sealed class MulticastMetadata : CashItemBase<MulticastMetadata>, IMulticastMetadata
    {
        private enum SerializeId
        {
            Type = 0,
            Tag = 1,
            CreationTime = 2,
            Metadata = 3,

            Cash = 4,
            Certificate = 5,
        }

        private string _type;
        private Tag _tag;
        private DateTime _creationTime;
        private Metadata _metadata;

        private Cash _cash;
        private Certificate _certificate;

        public static readonly int MaxTypeLength = 256;

        private MulticastMetadata() { }

        public MulticastMetadata(string type, Tag tag, DateTime creationTime, Metadata metadata, DigitalSignature digitalSignature, Miner miner, CancellationToken token)
        {
            this.Type = type;
            this.Tag = tag;
            this.CreationTime = creationTime;
            this.Metadata = metadata;

            this.CreateCash(miner, digitalSignature?.ToString(), token);
            this.CreateCertificate(digitalSignature);
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
                    if (id == (int)SerializeId.Type)
                    {
                        this.Type = reader.GetString();
                    }
                    else if (id == (int)SerializeId.Tag)
                    {
                        this.Tag = Tag.Import(reader.GetStream(), bufferManager);
                    }
                    else if (id == (int)SerializeId.CreationTime)
                    {
                        this.CreationTime = reader.GetDateTime();
                    }
                    else if (id == (int)SerializeId.Metadata)
                    {
                        this.Metadata = Metadata.Import(reader.GetStream(), bufferManager);
                    }

                    else if (id == (int)SerializeId.Cash)
                    {
                        this.Cash = Cash.Import(reader.GetStream(), bufferManager);
                    }
                    else if (id == (int)SerializeId.Certificate)
                    {
                        this.Certificate = Certificate.Import(reader.GetStream(), bufferManager);
                    }
                }
            }

            if (!this.VerifyCertificate()) throw new CertificateException();
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Type
                if (this.Type != null)
                {
                    writer.Write((int)SerializeId.Type);
                    writer.Write(this.Type);
                }
                // Tag
                if (this.Tag != null)
                {
                    writer.Write((int)SerializeId.Tag);
                    writer.Write(this.Tag.Export(bufferManager));
                }
                // CreationTime
                if (this.CreationTime != DateTime.MinValue)
                {
                    writer.Write((int)SerializeId.CreationTime);
                    writer.Write(this.CreationTime);
                }
                // Metadata
                if (this.Metadata != null)
                {
                    writer.Write((int)SerializeId.Metadata);
                    writer.Write(this.Metadata.Export(bufferManager));
                }

                // Cash
                if (this.Cash != null)
                {
                    writer.Write((int)SerializeId.Cash);
                    writer.Write(this.Cash.Export(bufferManager));
                }
                // Certificate
                if (this.Certificate != null)
                {
                    writer.Write((int)SerializeId.Certificate);
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
            if ((object)obj == null || !(obj is MulticastMetadata)) return false;

            return this.Equals((MulticastMetadata)obj);
        }

        public override bool Equals(MulticastMetadata other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Type != other.Type
                || this.Tag != other.Tag
                || this.CreationTime != other.CreationTime
                || this.Metadata != other.Metadata

                || this.Cash != other.Cash
                || this.Certificate != other.Certificate)
            {
                return false;
            }

            return true;
        }

        protected override Stream GetCashStream(string tag)
        {
            var tempCertificate = this.Certificate;
            this.Certificate = null;

            var tempCash = this.Cash;
            this.Cash = null;

            try
            {
                var bufferManager = BufferManager.Instance;
                var streams = new List<Stream>();

                streams.Add(this.Export(bufferManager));

                using (var writer = new ItemStreamWriter(bufferManager))
                {
                    writer.Write((int)SerializeId.Certificate);
                    writer.Write(tag);

                    streams.Add(writer.GetStream());
                }

                return new UniteStream(streams);
            }
            finally
            {
                this.Certificate = tempCertificate;
                this.Cash = tempCash;
            }
        }

        protected override Cash Cash
        {
            get
            {
                return _cash;
            }
            set
            {
                _cash = value;
            }
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

        #region IMulticastMetadata

        [DataMember(Name = nameof(Type))]
        public string Type
        {
            get
            {
                return _type;
            }
            private set
            {
                if (value != null && value.Length > MulticastMetadata.MaxTypeLength)
                {
                    throw new ArgumentException();
                }
                else
                {
                    _type = value;
                }
            }
        }

        [DataMember(Name = nameof(Tag))]
        public Tag Tag
        {
            get
            {
                return _tag;
            }
            private set
            {
                _tag = value;
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
