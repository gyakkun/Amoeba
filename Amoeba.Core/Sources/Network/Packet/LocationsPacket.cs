using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Serialization;

namespace Amoeba.Core
{
    sealed class LocationsPacket : ItemBase<LocationsPacket>
    {
        private enum SerializeId
        {
            Locations = 0,
        }

        private LocationCollection _locations;

        public static readonly int MaxLocationCount = 256;

        public LocationsPacket(IEnumerable<Location> locations)
        {
            if (locations != null) this.ProtectedLocations.AddRange(locations);
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                int id;

                while ((id = reader.GetInt()) != -1)
                {
                    if (id == (int)SerializeId.Locations)
                    {
                        for (int i = reader.GetInt() - 1; i >= 0; i--)
                        {
                            this.ProtectedLocations.Add(Location.Import(reader.GetStream(), bufferManager));
                        }
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Locations
                if (this.ProtectedLocations.Count > 0)
                {
                    writer.Write((int)SerializeId.Locations);
                    writer.Write(this.ProtectedLocations.Count);

                    foreach (var item in this.ProtectedLocations)
                    {
                        writer.Write(item.Export(bufferManager));
                    }
                }

                return writer.GetStream();
            }
        }

        private volatile ReadOnlyCollection<Location> _readOnlyLocations;

        public IEnumerable<Location> Locations
        {
            get
            {
                if (_readOnlyLocations == null)
                    _readOnlyLocations = new ReadOnlyCollection<Location>(this.ProtectedLocations);

                return _readOnlyLocations;
            }
        }

        private LocationCollection ProtectedLocations
        {
            get
            {
                if (_locations == null)
                    _locations = new LocationCollection(MaxLocationCount);

                return _locations;
            }
        }
    }
}
