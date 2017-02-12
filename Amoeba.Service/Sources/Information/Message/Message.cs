using System;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Serialization;

namespace Amoeba.Service
{
    [DataContract(Name = "Message")]
    public sealed class Message : ItemBase<Message>, IMessage
    {
        private enum SerializeId
        {
            Comment = 0,
        }

        private string _comment;

        public static readonly int MaxCommentLength = 1024 * 8;

        public Message(string comment)
        {
            this.Comment = comment;
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

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            return this.Comment.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is Message)) return false;

            return this.Equals((Message)obj);
        }

        public override bool Equals(Message other)
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

        [DataMember(Name = "Comment")]
        public string Comment
        {
            get
            {
                return _comment;
            }
            private set
            {
                if (value != null && value.Length > Message.MaxCommentLength)
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
