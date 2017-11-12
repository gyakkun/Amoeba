using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Serialization;
using Omnius.Utilities;

namespace Amoeba.Messages
{
    [DataContract(Name = nameof(Box))]
    public sealed class Box : ItemBase<Box>, IBox
    {
        private enum SerializeId
        {
            Name = 0,
            Seeds = 1,
            Boxes = 2,
        }

        private string _name;
        private SeedCollection _seeds;
        private BoxCollection _boxes;

        private int _hashCode;

        public static readonly int MaxNameLength = 256;
        public static readonly int MaxSeedCount = 1024 * 64;
        public static readonly int MaxBoxCount = 8192;

        public Box(string name, IEnumerable<Seed> seeds, IEnumerable<Box> boxes)
        {
            this.Name = name;
            if (seeds != null) this.ProtectedSeeds.AddRange(seeds);
            if (boxes != null) this.ProtectedBoxes.AddRange(boxes);

            this.Initialize();
        }

        protected override void Initialize()
        {
            _hashCode = this.Name?.GetHashCode() ?? 0
                ^ this.Seeds.FirstOrDefault()?.GetHashCode() ?? 0
                ^ this.Boxes.FirstOrDefault()?.GetHashCode() ?? 0;
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            if (depth > 256) throw new ArgumentException();

            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                while (reader.Available > 0)
                {
                    int id = (int)reader.GetUInt32();

                    if (id == (int)SerializeId.Name)
                    {
                        this.Name = reader.GetString();
                    }
                    else if (id == (int)SerializeId.Seeds)
                    {
                        for (int i = (int)reader.GetUInt32() - 1; i >= 0; i--)
                        {
                            this.ProtectedSeeds.Add(Seed.Import(reader.GetStream(), bufferManager));
                        }
                    }
                    else if (id == (int)SerializeId.Boxes)
                    {
                        for (int i = (int)reader.GetUInt32() - 1; i >= 0; i--)
                        {
                            this.ProtectedBoxes.Add(Box.Import(reader.GetStream(), bufferManager, depth + 1));
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
                // Name
                if (this.Name != null)
                {
                    writer.Write((uint)SerializeId.Name);
                    writer.Write(this.Name);
                }
                // Seeds
                if (this.ProtectedSeeds.Count > 0)
                {
                    writer.Write((uint)SerializeId.Seeds);
                    writer.Write((uint)this.ProtectedSeeds.Count);

                    foreach (var item in this.Seeds)
                    {
                        writer.Write(item.Export(bufferManager));
                    }
                }
                // Boxes
                if (this.ProtectedBoxes.Count > 0)
                {
                    writer.Write((uint)SerializeId.Boxes);
                    writer.Write((uint)this.ProtectedBoxes.Count);

                    foreach (var item in this.Boxes)
                    {
                        writer.Write(item.Export(bufferManager, depth + 1));
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
            if ((object)obj == null || !(obj is Box)) return false;

            return this.Equals((Box)obj);
        }

        public override bool Equals(Box other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Name != other.Name
                || !CollectionUtils.Equals(this.Seeds, other.Seeds)
                || !CollectionUtils.Equals(this.Boxes, other.Boxes))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return this.Name;
        }

        #region IBox

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != null && value.Length > Box.MaxNameLength)
                {
                    throw new ArgumentException();
                }
                else
                {
                    _name = value;
                }
            }
        }

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
                    _seeds = new SeedCollection(Box.MaxSeedCount);

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
                    _boxes = new BoxCollection(Box.MaxBoxCount);

                return _boxes;
            }
        }

        #endregion
    }
}
