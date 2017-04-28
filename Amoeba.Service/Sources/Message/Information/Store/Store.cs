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
            Seeds = 0,
            Boxes = 1,
        }

        private SeedCollection _seeds;
        private BoxCollection _boxes;

        private int _hashCode;

        public static readonly int MaxSeedCount = 1024 * 64;
        public static readonly int MaxBoxCount = 8192;

        private Store() { }

        public Store(IEnumerable<Seed> seeds, IEnumerable<Box> boxes)
        {
            if (seeds != null) this.ProtectedSeeds.AddRange(seeds);
            if (boxes != null) this.ProtectedBoxes.AddRange(boxes);

            this.Initialize();
        }

        protected override void Initialize()
        {
            _hashCode = this.Seeds.FirstOrDefault()?.GetHashCode() ?? 0
                ^ this.Boxes.FirstOrDefault()?.GetHashCode() ?? 0;
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            if (depth > 256) throw new ArgumentException();

            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.Seeds)
                    {
                        for (int i = reader.GetInt() - 1; i >= 0; i--)
                        {
                            this.ProtectedSeeds.Add(Seed.Import(reader.GetStream(), bufferManager));
                        }
                    }
                    else if (id == (int)SerializeId.Boxes)
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
                // Seeds
                if (this.ProtectedSeeds.Count > 0)
                {
                    writer.Write((int)SerializeId.Seeds);
                    writer.Write(this.ProtectedSeeds.Count);

                    foreach (var item in this.Seeds)
                    {
                        writer.Write(item.Export(bufferManager));
                    }
                }
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

            if (!CollectionUtils.Equals(this.Seeds, other.Seeds)
                || !CollectionUtils.Equals(this.Boxes, other.Boxes))
            {
                return false;
            }

            return true;
        }

        #region IStore

        private volatile ReadOnlyCollection<Seed> _readOnlySeeds;

        public IEnumerable<Seed> Seeds
        {
            get
            {
                if (_readOnlySeeds == null)
                    _readOnlySeeds = new ReadOnlyCollection<Seed>(this.ProtectedSeeds);

                return _readOnlySeeds;
            }
        }

        [DataMember(Name = nameof(Seeds))]
        private SeedCollection ProtectedSeeds
        {
            get
            {
                if (_seeds == null)
                    _seeds = new SeedCollection(Store.MaxSeedCount);

                return _seeds;
            }
        }

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
