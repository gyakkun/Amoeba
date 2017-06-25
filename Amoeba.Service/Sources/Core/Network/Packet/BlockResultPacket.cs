using System;
using System.IO;
using Omnius.Base;
using Omnius.Serialization;

namespace Amoeba.Service
{
    sealed class BlockResultPacket : ItemBase<BlockResultPacket>
    {
        private enum SerializeId
        {
            Hash = 0,
            Value = 1,
        }

        private Hash _hash;
        private ArraySegment<byte> _value;

        private BlockResultPacket() { }

        public BlockResultPacket(Hash hash, ArraySegment<byte> value)
        {
            this.Hash = hash;
            this.Value = value;
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

                    if (id == (int)SerializeId.Hash)
                    {
                        this.Hash = Hash.Import(reader.GetStream(), bufferManager);
                    }
                    else if (id == (int)SerializeId.Value)
                    {
                        using (var rangeStream = reader.GetStream())
                        {
                            if (this.Value.Array != null)
                            {
                                bufferManager.ReturnBuffer(this.Value.Array);
                            }

                            byte[] buffer = null;

                            try
                            {
                                buffer = bufferManager.TakeBuffer((int)rangeStream.Length);
                                rangeStream.Read(buffer, 0, (int)rangeStream.Length);
                            }
                            catch (Exception e)
                            {
                                if (buffer != null)
                                {
                                    bufferManager.ReturnBuffer(buffer);
                                }

                                throw e;
                            }

                            this.Value = new ArraySegment<byte>(buffer, 0, (int)rangeStream.Length);
                        }
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Hash
                if (this.Hash != null)
                {
                    writer.Write((uint)SerializeId.Hash);
                    writer.Write(this.Hash.Export(bufferManager));
                }
                // Value
                if (this.Value.Array != null)
                {
                    writer.Write((uint)SerializeId.Value);
                    writer.Write(this.Value.Array, this.Value.Offset, this.Value.Count);
                }

                return writer.GetStream();
            }
        }

        public Hash Hash
        {
            get
            {
                return _hash;
            }
            private set
            {
                _hash = value;
            }
        }

        public ArraySegment<byte> Value
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
    }
}
