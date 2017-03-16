using System;
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
    sealed class ProfilePacket : ItemBase<ProfilePacket>
    {
        private enum SerializeId
        {
            Id = 0,
            Location = 1,
        }

        private byte[] _id;
        private Location _location;

        public ProfilePacket(byte[] id, Location location)
        {
            this.Id = id;
            this.Location = location;
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
                    if (id == (int)SerializeId.Id)
                    {
                        this.Id = reader.GetBytes();
                    }
                    else if (id == (int)SerializeId.Location)
                    {
                        this.Location = Location.Import(reader.GetStream(), bufferManager);
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Id
                if (this.Id != null)
                {
                    writer.Write((int)SerializeId.Id);
                    writer.Write(this.Id);
                }
                // Location
                if (this.Location != null)
                {
                    writer.Write((int)SerializeId.Location);
                    writer.Write(this.Location.Export(bufferManager));
                }

                return writer.GetStream();
            }
        }

        public byte[] Id
        {
            get
            {
                return _id;
            }
            private set
            {
                _id = value;
            }
        }

        public Location Location
        {
            get
            {
                return _location;
            }
            private set
            {
                _location = value;
            }
        }
    }
}
