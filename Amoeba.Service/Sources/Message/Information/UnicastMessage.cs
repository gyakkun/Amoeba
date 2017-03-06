using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Service
{
    [DataContract(Name = nameof(UnicastMessage<T>))]
    public sealed class UnicastMessage<T> : ItemBase<UnicastMessage<T>>, IUnicastMessage<T>
        where T : ItemBase<T>
    {
        private enum SerializeId
        {
            TargetSignature = 0,
            AuthorSignature = 1,
            CreationTime = 2,
            Value = 3,
        }

        private Signature _targetSignature;
        private Signature _authorSignature;
        private DateTime _creationTime;
        private T _value;

        public UnicastMessage(Signature targetSignature, Signature authorSignature, DateTime creationTime, T value)
        {
            this.TargetSignature = targetSignature;
            this.AuthorSignature = authorSignature;
            this.CreationTime = creationTime;
            this.Value = value;
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.TargetSignature)
                    {
                        this.TargetSignature = Signature.Import(reader.GetStream(), bufferManager);
                    }
                    else if (id == (int)SerializeId.AuthorSignature)
                    {
                        this.AuthorSignature = Signature.Import(reader.GetStream(), bufferManager);
                    }
                    else if (id == (int)SerializeId.CreationTime)
                    {
                        this.CreationTime = reader.GetDateTime();
                    }
                    else if (id == (int)SerializeId.Value)
                    {
                        this.Value = ItemBase<T>.Import(reader.GetStream(), bufferManager);
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // TargetSignature
                if (this.TargetSignature != null)
                {
                    writer.Write((int)SerializeId.TargetSignature);
                    writer.Write(this.TargetSignature.Export(bufferManager));
                }
                // AuthorSignature
                if (this.AuthorSignature != null)
                {
                    writer.Write((int)SerializeId.AuthorSignature);
                    writer.Write(this.AuthorSignature.Export(bufferManager));
                }
                // CreationTime
                if (this.CreationTime != DateTime.MinValue)
                {
                    writer.Write((int)SerializeId.CreationTime);
                    writer.Write(this.CreationTime);
                }
                // Value
                if (this.Value != null)
                {
                    writer.Write((int)SerializeId.Value);
                    writer.Write(this.Value.Export(bufferManager));
                }

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            if (this.Value == null) return 0;
            else return this.Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is UnicastMessage<T>)) return false;

            return this.Equals((UnicastMessage<T>)obj);
        }

        public override bool Equals(UnicastMessage<T> other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.TargetSignature != other.TargetSignature
                || this.AuthorSignature != other.AuthorSignature
                || this.CreationTime != other.CreationTime
                || this.Value != other.Value)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return this.Value.ToString();
        }

        #region IUnicastMessage

        [DataMember(Name = nameof(TargetSignature))]
        public Signature TargetSignature
        {
            get
            {
                return _targetSignature;
            }
            private set
            {
                _targetSignature = value;
            }
        }

        [DataMember(Name = nameof(AuthorSignature))]
        public Signature AuthorSignature
        {
            get
            {
                return _authorSignature;
            }
            private set
            {
                _authorSignature = value;
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

        [DataMember(Name = nameof(Value))]
        public T Value
        {
            get
            {
                return _value;
            }
            private set
            {
                _value = value;
            }
        }

        #endregion
    }
}
