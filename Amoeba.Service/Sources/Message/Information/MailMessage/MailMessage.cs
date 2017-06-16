using System;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Serialization;

namespace Amoeba.Service
{
    [DataContract(Name = nameof(MailMessage))]
    public sealed class MailMessage : ItemBase<MailMessage>, IMailMessage
    {
        private enum SerializeId
        {
            Comment = 0,
        }

        private string _comment;

        public static readonly int MaxCommentLength = 1024 * 8;

        private MailMessage() { }

        public MailMessage(string comment)
        {
            this.Comment = comment;
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

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            return this.Comment.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is MailMessage)) return false;

            return this.Equals((MailMessage)obj);
        }

        public override bool Equals(MailMessage other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Comment != other.Comment)
            {
                return false;
            }

            return true;
        }

        #region IMessage

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

        #endregion
    }
}
