﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Io;
using Omnius.Serialization;
using Omnius.Utilities;

namespace Amoeba.Core
{
    [DataContract(Name = "Location")]
    public sealed class Location : ItemBase<Location>, ILocation
    {
        private enum SerializeId
        {
            Uris = 0,
        }

        private volatile UriCollection _uris;

        private volatile int _hashCode;

        public static readonly int MaxUriCount = 32;

        public Location(IEnumerable<string> uris)
        {
            if (uris != null) this.ProtectedUris.AddRange(uris);
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.Uris)
                    {
                        for (int i = reader.GetInt() - 1; i >= 0; i--)
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
                    writer.Write((int)SerializeId.Uris);
                    writer.Write(this.ProtectedUris.Count);

                    foreach (var uri in this.ProtectedUris)
                    {
                        writer.Write(uri);
                    }
                }

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            return _hashCode;
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
            return String.Join(", ", this.Uris);
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

        [DataMember(Name = "Uris")]
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