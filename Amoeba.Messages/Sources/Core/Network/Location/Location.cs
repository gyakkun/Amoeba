using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Serialization;
using Omnius.Utils;

namespace Amoeba.Messages
{
    [DataContract(Name = nameof(Location))]
    public sealed class Location : ItemBase<Location>, ILocation
    {
        private enum SerializeId
        {
            Uris = 0,
        }

        private volatile UriCollection _uris;

        public static readonly int MaxUriCount = 32;

        public Location(IEnumerable<string> uris)
        {
            if (uris != null) this.ProtectedUris.AddRange(uris);
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

                    if (id == (int)SerializeId.Uris)
                    {
                        for (int i = (int)reader.GetUInt32() - 1; i >= 0; i--)
                        {
                            this.ProtectedUris.Add(reader.GetString());
                        }
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Uris
                if (this.ProtectedUris.Count > 0)
                {
                    writer.Write((uint)SerializeId.Uris);
                    writer.Write((uint)this.ProtectedUris.Count);

                    foreach (string uri in this.ProtectedUris)
                    {
                        writer.Write(uri);
                    }
                }

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            if (this.ProtectedUris.Count == 0) return 0;
            else return this.ProtectedUris[0].GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is Location)) return false;

            return this.Equals((Location)obj);
        }

        public override bool Equals(Location other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if ((this.Uris == null) != (other.Uris == null))
            {
                return false;
            }

            if (this.Uris != null && other.Uris != null)
            {
                if (!CollectionUtils.Equals(this.Uris, other.Uris)) return false;
            }

            return true;
        }

        public override string ToString()
        {
            return string.Join(", ", this.Uris);
        }

        #region ILocation

        private volatile ReadOnlyCollection<string> _readOnlyUris;

        public IEnumerable<string> Uris
        {
            get
            {
                if (_readOnlyUris == null)
                    _readOnlyUris = new ReadOnlyCollection<string>(this.ProtectedUris);

                return _readOnlyUris;
            }
        }

        [DataMember(Name = nameof(Uris))]
        private UriCollection ProtectedUris
        {
            get
            {
                if (_uris == null)
                    _uris = new UriCollection(Location.MaxUriCount);

                return _uris;
            }
        }

        #endregion
    }
}
