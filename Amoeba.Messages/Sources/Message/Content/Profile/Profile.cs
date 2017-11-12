using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Security;
using Omnius.Serialization;
using Omnius.Utilities;

namespace Amoeba.Messages
{
    [DataContract(Name = nameof(Profile))]
    public sealed class Profile : ItemBase<Profile>, IProfile
    {
        private enum SerializeId
        {
            Comment = 0,
            ExchangePublicKey = 1,
            TrustSignatures = 2,
            DeleteSignatures = 3,
            Tags = 4,
        }

        private string _comment;
        private ExchangePublicKey _exchangePublicKey;
        private SignatureCollection _trustSignatures;
        private SignatureCollection _deleteSignatures;
        private TagCollection _tags = new TagCollection();

        public static readonly int MaxCommentLength = 1024 * 8;
        public static readonly int MaxTrustSignatureCount = 1024;
        public static readonly int MaxDeleteSignatureCount = 1024;
        public static readonly int MaxTagCount = 1024;

        public Profile(string comment, ExchangePublicKey exchangePublicKey,
            IEnumerable<Signature> trustSignatures, IEnumerable<Signature> deleteSignatures,
            IEnumerable<Tag> tags)
        {
            this.Comment = comment;
            this.ExchangePublicKey = exchangePublicKey;
            if (trustSignatures != null) this.ProtectedTrustSignatures.AddRange(trustSignatures);
            if (deleteSignatures != null) this.ProtectedDeleteSignatures.AddRange(deleteSignatures);
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

                    if (id == (int)SerializeId.Comment)
                    {
                        this.Comment = reader.GetString();
                    }
                    else if (id == (int)SerializeId.ExchangePublicKey)
                    {
                        this.ExchangePublicKey = ExchangePublicKey.Import(reader.GetStream(), bufferManager);
                    }
                    else if (id == (int)SerializeId.TrustSignatures)
                    {
                        for (int i = (int)reader.GetUInt32() - 1; i >= 0; i--)
                        {
                            this.ProtectedTrustSignatures.Add(Signature.Import(reader.GetStream(), bufferManager));
                        }
                    }
                    else if (id == (int)SerializeId.DeleteSignatures)
                    {
                        for (int i = (int)reader.GetUInt32() - 1; i >= 0; i--)
                        {
                            this.ProtectedDeleteSignatures.Add(Signature.Import(reader.GetStream(), bufferManager));
                        }
                    }
                    else if (id == (int)SerializeId.Tags)
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
                // Comment
                if (this.Comment != null)
                {
                    writer.Write((uint)SerializeId.Comment);
                    writer.Write(this.Comment);
                }
                // ExchangePublicKey
                if (this.ExchangePublicKey != null)
                {
                    writer.Write((uint)SerializeId.ExchangePublicKey);
                    writer.Write(this.ExchangePublicKey.Export(bufferManager));
                }
                // TrustSignatures
                if (this.ProtectedTrustSignatures.Count > 0)
                {
                    writer.Write((uint)SerializeId.TrustSignatures);
                    writer.Write((uint)this.ProtectedTrustSignatures.Count);

                    foreach (var value in this.TrustSignatures)
                    {
                        writer.Write(value.Export(bufferManager));
                    }
                }
                // DeleteSignatures
                if (this.ProtectedDeleteSignatures.Count > 0)
                {
                    writer.Write((uint)SerializeId.DeleteSignatures);
                    writer.Write((uint)this.ProtectedDeleteSignatures.Count);

                    foreach (var value in this.DeleteSignatures)
                    {
                        writer.Write(value.Export(bufferManager));
                    }
                }
                // Tags
                if (this.ProtectedTags.Count > 0)
                {
                    writer.Write((uint)SerializeId.Tags);
                    writer.Write((uint)this.ProtectedTags.Count);

                    foreach (var value in this.Tags)
                    {
                        writer.Write(value.Export(bufferManager));
                    }
                }

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            if (this.ExchangePublicKey == null) return 0;
            else return this.ExchangePublicKey.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is Profile)) return false;

            return this.Equals((Profile)obj);
        }

        public override bool Equals(Profile other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Comment != other.Comment
                || this.ExchangePublicKey != other.ExchangePublicKey
                || !CollectionUtils.Equals(this.TrustSignatures, other.TrustSignatures)
                || !CollectionUtils.Equals(this.DeleteSignatures, other.DeleteSignatures)
                || !CollectionUtils.Equals(this.Tags, other.Tags))
            {
                return false;
            }

            return true;
        }

        #region IProfile

        [DataMember(Name = nameof(Comment))]
        public string Comment
        {
            get
            {
                return _comment;
            }
            private set
            {
                if (value != null && value.Length > MailMessage.MaxCommentLength)
                {
                    throw new ArgumentException();
                }
                else
                {
                    _comment = value;
                }
            }
        }

        [DataMember(Name = nameof(ExchangePublicKey))]
        public ExchangePublicKey ExchangePublicKey
        {
            get
            {
                return _exchangePublicKey;
            }
            private set
            {
                _exchangePublicKey = value;
            }
        }

        private volatile ReadOnlyCollection<Signature> _readOnlyTrustSignatures;

        public IEnumerable<Signature> TrustSignatures
        {
            get
            {
                if (_readOnlyTrustSignatures == null)
                    _readOnlyTrustSignatures = new ReadOnlyCollection<Signature>(this.ProtectedTrustSignatures.ToArray());

                return _readOnlyTrustSignatures;
            }
        }

        [DataMember(Name = nameof(TrustSignatures))]
        private SignatureCollection ProtectedTrustSignatures
        {
            get
            {
                if (_trustSignatures == null)
                    _trustSignatures = new SignatureCollection(Profile.MaxTrustSignatureCount);

                return _trustSignatures;
            }
        }

        private volatile ReadOnlyCollection<Signature> _readOnlyDeleteSignatures;

        public IEnumerable<Signature> DeleteSignatures
        {
            get
            {
                if (_readOnlyDeleteSignatures == null)
                    _readOnlyDeleteSignatures = new ReadOnlyCollection<Signature>(this.ProtectedDeleteSignatures.ToArray());

                return _readOnlyDeleteSignatures;
            }
        }

        [DataMember(Name = nameof(DeleteSignatures))]
        private SignatureCollection ProtectedDeleteSignatures
        {
            get
            {
                if (_deleteSignatures == null)
                    _deleteSignatures = new SignatureCollection(Profile.MaxDeleteSignatureCount);

                return _deleteSignatures;
            }
        }

        private volatile ReadOnlyCollection<Tag> _readOnlyTags;

        public IEnumerable<Tag> Tags
        {
            get
            {
                if (_readOnlyTags == null)
                    _readOnlyTags = new ReadOnlyCollection<Tag>(this.ProtectedTags.ToArray());

                return _readOnlyTags;
            }
        }

        [DataMember(Name = nameof(Tags))]
        private TagCollection ProtectedTags
        {
            get
            {
                if (_tags == null)
                    _tags = new TagCollection(Profile.MaxTagCount);

                return _tags;
            }
        }

        #endregion
    }
}
