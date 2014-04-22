using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Threading;

namespace Amoeba
{
    static class Sha512
    {
        private static readonly ThreadLocal<SHA512> _threadLocalSha512 = new ThreadLocal<SHA512>(() => SHA512.Create());
        private static readonly ThreadLocal<Encoding> _threadLocalEncoding = new ThreadLocal<Encoding>(() => new UTF8Encoding(false));

        public static byte[] ComputeHash(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");

            return _threadLocalSha512.Value.ComputeHash(buffer, offset, count);
        }

        public static byte[] ComputeHash(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");

            return _threadLocalSha512.Value.ComputeHash(buffer, 0, buffer.Length);
        }

        public static byte[] ComputeHash(string value)
        {
            if (value == null) throw new ArgumentNullException("value");

            return _threadLocalSha512.Value.ComputeHash(_threadLocalEncoding.Value.GetBytes(value));
        }

        public static byte[] ComputeHash(ArraySegment<byte> value)
        {
            if (value.Array == null) throw new ArgumentNullException("value");

            return _threadLocalSha512.Value.ComputeHash(value.Array, value.Offset, value.Count);
        }

        public static byte[] ComputeHash(Stream inputStream)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");

            return _threadLocalSha512.Value.ComputeHash(inputStream);
        }

        public static byte[] ComputeHash(IList<ArraySegment<byte>> value)
        {
            if (value == null) throw new ArgumentNullException("value");

            if (value.Count == 1) return _threadLocalSha512.Value.ComputeHash(value[0].Array, value[0].Offset, value[0].Count);

            using (var sha512 = SHA512.Create())
            {
                for (int i = 0; i < value.Count; i++)
                {
                    if (i == value.Count - 1)
                    {
                        sha512.TransformFinalBlock(value[i].Array, value[i].Offset, value[i].Count);
                    }
                    else
                    {
                        sha512.TransformBlock(value[i].Array, value[i].Offset, value[i].Count, value[i].Array, value[i].Offset);
                    }
                }

                return sha512.Hash;
            }
        }
    }
}
