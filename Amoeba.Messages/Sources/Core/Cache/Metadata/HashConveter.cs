using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Omnius.Base;
using Omnius.Serialization;

namespace Amoeba.Messages
{
    public static class HashConveter
    {
        public static Stream ToStream(Hash hash)
        {
            using (var writer = new ItemStreamWriter(BufferManager.Instance))
            {
                writer.Write((uint)hash.Algorithm);
                writer.Write(hash.Value);

                return writer.GetStream();
            }
        }

        public static Hash FromStream(Stream stream)
        {
            using (var reader = new ItemStreamReader(stream, BufferManager.Instance))
            {
                var algorithm = (HashAlgorithm)reader.GetUInt32();
                var value = reader.GetBytes();

                return new Hash(algorithm, value);
            }
        }
    }
}
