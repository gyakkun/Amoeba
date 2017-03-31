using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Serialization;

namespace Amoeba.Service
{
    [DataContract(Name = nameof(Store))]
    public sealed class Store : ItemBase<Store>, IStore
    {
        private enum SerializeId
        {
            Boxes = 0,
        }

        private BoxCollection _boxes;

        private int _hashCode;

        public static readonly int MaxBoxCount = 8192;

        private Store() { }

        public Store(IEnumerable<Box> boxes)
        {
            if (boxes != null) this.ProtectedBoxes.AddRange(boxes);

            this.Initialize();
        }

        protected override void Initialize()
        {
            _hashCode = this.Boxes.FirstOrDefault()?.GetHashCode() ?? 0;
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            if (depth > 256) throw new ArgumentException();

            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.Boxes)
                    {
                        for (int i = reader.GetInt() - 1; i >= 0; i--)
                        {
                            this.ProtectedBoxes.Add(Box.Import(reader.GetStream(), bufferManager));
                        }
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            if (depth > 256) throw new ArgumentException();

            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Boxes
                if (this.ProtectedBoxes.Count > 0)
                {
                    writer.Write((int)SerializeId.Boxes);
                    writer.Write(this.ProtectedBoxes.Count);

                    foreach (var item in this.Boxes)
                    {
                        writer.Write(item.Export(bufferManager));
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
            if ((object)obj == null || !(obj is Store)) return false;

            return this.Equals((Store)obj);
        }

        public override bool Equals(Store other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (!CollectionUtils.Equals(this.Boxes, other.Boxes))
            {
                return false;
            }

            return true;
        }

        #region IStore

        private volatile ReadOnlyCollection<Box> _readOnlyBoxes;

        public IEnumerable<Box> Boxes
        {
            get
            {
                if (_readOnlyBoxes == null)
                    _readOnlyBoxes = new ReadOnlyCollection<Box>(this.ProtectedBoxes);

                return _readOnlyBoxes;
            }
        }

        [DataMember(Name = nameof(Boxes))]
        private BoxCollection ProtectedBoxes
        {
            get
            {
                if (_boxes == null)
                    _boxes = new BoxCollection(Store.MaxBoxCount);

                return _boxes;
            }
        }

        #endregion
    }
}
