using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Service
{
    [DataContract(Name = "Profile")]
    public sealed class Profile : ItemBase<Profile>, IProfile
    {
        private enum SerializeId
        {
            Comment = 0,
            ExchangePublicKey = 1,
            Link = 2,
        }

        private string _comment;
        private ExchangePublicKey _exchangePublicKey;
        private Link _link;

        public static readonly int MaxCommentLength = 1024 * 8;

        public Profile(string comment, ExchangePublicKey exchangePublicKey, Link link)
        {
            this.Comment = comment;
            this.ExchangePublicKey = exchangePublicKey;
            this.Link = link;
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.Comment)
                    {
                        this.Comment = reader.GetString();
                    }
                    if (id == (int)SerializeId.ExchangePublicKey)
                    {
                        this.ExchangePublicKey = ExchangePublicKey.Import(reader.GetStream(), bufferManager);
                    }
                    else if (id == (int)SerializeId.Link)
                    {
                        this.Link = Link.Import(reader.GetStream(), bufferManager);
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
                    writer.Write((int)SerializeId.Comment);
                    writer.Write(this.Comment);
                }
                // ExchangePublicKey
                if (this.ExchangePublicKey != null)
                {
                    writer.Write((int)SerializeId.ExchangePublicKey);
                    writer.Write(this.ExchangePublicKey.Export(bufferManager));
                }
                // Link
                if (this.Link != null)
                {
                    writer.Write((int)SerializeId.Link);
                    writer.Write(this.Link.Export(bufferManager));
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
                || this.Link != other.Link)
            {
                return false;
            }

            return true;
        }

        #region IProfile

        [DataMember(Name = "Comment")]
        public string Comment
        {
            get
            {
                return _comment;
            }
            private set
            {
                if (value != null && value.Length > Mail.MaxCommentLength)
                {
                    throw new ArgumentException();
                }
                else
                {
                    _comment = value;
                }
            }
        }

        [DataMember(Name = "ExchangePublicKey")]
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

        [DataMember(Name = "Link")]
        public Link Link
        {
            get
            {
                return _link;
            }
            private set
            {
                _link = value;
            }
        }

        #endregion
    }
}
