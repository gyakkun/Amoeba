using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Serialization;

namespace Amoeba.Core
{
    [DataContract(Name = nameof(Group))]
    sealed class Group : ItemBase<Group>, IGroup
    {
        private enum SerializeId
        {
            CorrectionAlgorithm = 0,
            Length = 1,
            Hashes = 2,
        }

        private CorrectionAlgorithm _correctionAlgorithm;
        private long _length;
        private HashCollection _hashes;

        public Group(CorrectionAlgorithm correctionAlgorithm, long length, IEnumerable<Hash> hashes)
        {
            this.CorrectionAlgorithm = correctionAlgorithm;
            this.Length = length;
            if (hashes != null) this.ProtectedHashes.AddRange(hashes);
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
                    if (id == (int)SerializeId.CorrectionAlgorithm)
                    {
                        this.CorrectionAlgorithm = (CorrectionAlgorithm)reader.GetByte();
                    }
                    else if (id == (int)SerializeId.Length)
                    {
                        this.Length = reader.GetLong();
                    }
                    else if (id == (int)SerializeId.Hashes)
                    {
                        for (int i = reader.GetInt() - 1; i >= 0; i--)
                        {
                            this.ProtectedHashes.Add(Hash.Import(reader.GetStream(), bufferManager));
                        }
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // CorrectionAlgorithm
                if (this.CorrectionAlgorithm != 0)
                {
                    writer.Write((int)SerializeId.CorrectionAlgorithm);
                    writer.Write((byte)this.CorrectionAlgorithm);
                }
                // Length
                if (this.Length != 0)
                {
                    writer.Write((int)SerializeId.Length);
                    writer.Write(this.Length);
                }
                // Hashes
                if (this.ProtectedHashes.Count > 0)
                {
                    writer.Write((int)SerializeId.Hashes);
                    writer.Write(this.ProtectedHashes.Count);

                    foreach (var item in this.ProtectedHashes)
                    {
                        writer.Write(item.Export(bufferManager));
                    }
                }

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            if (this.ProtectedHashes.Count == 0) return 0;
            else return this.ProtectedHashes[0].GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is Group)) return false;

            return this.Equals((Group)obj);
        }

        public override bool Equals(Group other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.CorrectionAlgorithm != other.CorrectionAlgorithm
                || this.Length != other.Length
                || !CollectionUtils.Equals(this.Hashes, other.Hashes))
            {
                return false;
            }

            return true;
        }

        #region IGroup

        private volatile ReadOnlyCollection<Hash> _readOnlyHashes;

        public IEnumerable<Hash> Hashes
        {
            get
            {
                if (_readOnlyHashes == null)
                    _readOnlyHashes = new ReadOnlyCollection<Hash>(this.ProtectedHashes);

                return _readOnlyHashes;
            }
        }

        [DataMember(Name = nameof(CorrectionAlgorithm))]
        public CorrectionAlgorithm CorrectionAlgorithm
        {
            get
            {
                return _correctionAlgorithm;
            }
            private set
            {
                if (!Enum.IsDefined(typeof(CorrectionAlgorithm), value))
                {
                    throw new ArgumentException();
                }
                else
                {
                    _correctionAlgorithm = value;
                }
            }
        }

        [DataMember(Name = nameof(Length))]
        public long Length
        {
            get
            {
                return _length;
            }
            private set
            {
                _length = value;
            }
        }

        [DataMember(Name = nameof(Hashes))]
        private HashCollection ProtectedHashes
        {
            get
            {
                if (_hashes == null)
                    _hashes = new HashCollection();

                return _hashes;
            }
        }

        #endregion
    }
}
