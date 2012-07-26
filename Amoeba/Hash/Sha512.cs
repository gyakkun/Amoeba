using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace Amoeba
{
    /// <summary>
    /// SHA512bitハッシュ生成クラス
    /// </summary>
    static class Sha512
    {
        public static byte[] ComputeHash(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");

            using (var sha512 = new SHA512Managed())
            {
                return sha512.ComputeHash(buffer, offset, count);
            }
        }

        /// <summary>
        /// ハッシュを生成する
        /// </summary>
        /// <param name="buffer">ハッシュ値を計算するbyte配列</param>
        public static byte[] ComputeHash(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");

            return Sha512.ComputeHash(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// ハッシュを生成する
        /// </summary>
        /// <param name="value">ハッシュ値を計算する文字列</param>
        public static byte[] ComputeHash(string value)
        {
            if (value == null) throw new ArgumentNullException("value");

            return Sha512.ComputeHash(new UTF8Encoding(false).GetBytes(value));
        }

        public static byte[] ComputeHash(ArraySegment<byte> value)
        {
            if (value.Array == null) throw new ArgumentNullException("value");

            return Sha512.ComputeHash(value.Array, value.Offset, value.Count);
        }

        public static byte[] ComputeHash(Stream inputStream)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");

            using (var sha512 = new SHA512Managed())
            {
                return sha512.ComputeHash(inputStream);
            }
        }

        public static byte[] ComputeHash(IList<ArraySegment<byte>> value)
        {
            if (value == null) throw new ArgumentNullException("value");

            if (value.Count == 1) return Sha512.ComputeHash(value[0]);

            using (var sha512 = new SHA512Managed())
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
