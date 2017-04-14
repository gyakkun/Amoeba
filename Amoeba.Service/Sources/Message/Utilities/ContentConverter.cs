using Omnius.Base;
using Omnius.Io;
using Omnius.Security;
using Omnius.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Amoeba.Service
{
    static class ContentConverter
    {
        private enum ConvertCompressionAlgorithm
        {
            None = 0,
            Deflate = 1,
        }

        private enum ConvertCryptoAlgorithm
        {
            Aes256 = 0,
        }

        private enum ConvertHashAlgorithm
        {
            Sha256 = 0,
        }

        private static BufferManager _bufferManager = BufferManager.Instance;
        private static RandomNumberGenerator _random = RandomNumberGenerator.Create();

        private static Stream AddVersion(Stream stream, int version)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var versionStream = new BufferStream(_bufferManager);
            VintUtils.Write(versionStream, version);

            return new UniteStream(versionStream, stream);
        }

        private static Stream RemoveVersion(Stream stream, int version)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (VintUtils.Get(stream) != version) throw new FormatException();

            return new RangeStream(stream, true);
        }

        private static Stream Compress(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            try
            {
                var dic = new Dictionary<byte, Stream>();

                try
                {
                    stream.Seek(0, SeekOrigin.Begin);

                    BufferStream deflateBufferStream = null;

                    try
                    {
                        deflateBufferStream = new BufferStream(_bufferManager);

                        using (var deflateStream = new DeflateStream(deflateBufferStream, CompressionMode.Compress, true))
                        using (var safeBuffer = _bufferManager.CreateSafeBuffer(1024 * 4))
                        {
                            int length;

                            while ((length = stream.Read(safeBuffer.Value, 0, safeBuffer.Value.Length)) > 0)
                            {
                                deflateStream.Write(safeBuffer.Value, 0, length);
                            }
                        }

                        deflateBufferStream.Seek(0, SeekOrigin.Begin);

                        dic.Add((byte)ConvertCompressionAlgorithm.Deflate, deflateBufferStream);
                    }
                    catch (Exception)
                    {
                        if (deflateBufferStream != null)
                        {
                            deflateBufferStream.Dispose();
                        }
                    }
                }
                catch (Exception)
                {

                }

                dic.Add((byte)ConvertCompressionAlgorithm.None, stream);

                var list = dic.ToList();

                list.Sort((x, y) =>
                {
                    int c = x.Value.Length.CompareTo(y.Value.Length);
                    if (c != 0) return c;

                    return x.Key.CompareTo(y.Key);
                });

                for (int i = 1; i < list.Count; i++)
                {
                    list[i].Value.Dispose();
                }

                var headerStream = new BufferStream(_bufferManager);
                VintUtils.Write(headerStream, list[0].Key);

                return new UniteStream(headerStream, list[0].Value);
            }
            catch (Exception ex)
            {
                if (stream != null)
                {
                    stream.Dispose();
                }

                throw new ArgumentException(ex.Message, ex);
            }
        }

        private static Stream Decompress(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            try
            {
                stream.Seek(0, SeekOrigin.Begin);

                int type = (int)VintUtils.Get(stream);

                if (type == (int)ConvertCompressionAlgorithm.None)
                {
                    return new RangeStream(stream);
                }
                else if (type == (int)ConvertCompressionAlgorithm.Deflate)
                {
                    BufferStream deflateBufferStream = null;

                    try
                    {
                        deflateBufferStream = new BufferStream(_bufferManager);

                        using (var deflateStream = new DeflateStream(stream, CompressionMode.Decompress))
                        using (var safeBuffer = _bufferManager.CreateSafeBuffer(1024 * 4))
                        {
                            int length;

                            while ((length = deflateStream.Read(safeBuffer.Value, 0, safeBuffer.Value.Length)) > 0)
                            {
                                deflateBufferStream.Write(safeBuffer.Value, 0, length);

                                if (deflateBufferStream.Length > 1024 * 1024 * 256) throw new Exception("too large");
                            }
                        }

                        deflateBufferStream.Seek(0, SeekOrigin.Begin);

                        return deflateBufferStream;
                    }
                    catch (Exception)
                    {
                        if (deflateBufferStream != null)
                        {
                            deflateBufferStream.Dispose();
                        }

                        throw;
                    }
                }
                else
                {
                    throw new ArgumentException("ArgumentException");
                }
            }
            catch (Exception e)
            {
                if (stream != null)
                {
                    stream.Dispose();
                }

                throw new ArgumentException(e.Message, e);
            }
        }

        private static Stream Encrypt(Stream stream, IExchangeEncrypt publicKey)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (publicKey == null) throw new ArgumentNullException(nameof(publicKey));

            try
            {
                BufferStream outStream = null;

                try
                {
                    outStream = new BufferStream(_bufferManager);
                    VintUtils.Write(outStream, (int)ConvertCryptoAlgorithm.Aes256);

                    var cryptoKey = new byte[32];
                    _random.GetBytes(cryptoKey);

                    {
                        var encryptedBuffer = Exchange.Encrypt(publicKey, cryptoKey);
                        VintUtils.Write(outStream, (int)encryptedBuffer.Length);
                        outStream.Write(encryptedBuffer, 0, encryptedBuffer.Length);
                    }

                    var iv = new byte[32];
                    _random.GetBytes(iv);
                    outStream.Write(iv, 0, iv.Length);

                    using (var aes = Aes.Create())
                    {
                        aes.KeySize = 256;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using (var inStream = new WrapperStream(stream, true))
                        using (var cs = new CryptoStream(inStream, aes.CreateEncryptor(cryptoKey, iv), CryptoStreamMode.Read))
                        using (var safeBuffer = _bufferManager.CreateSafeBuffer(1024 * 4))
                        {
                            int length;

                            while ((length = cs.Read(safeBuffer.Value, 0, safeBuffer.Value.Length)) > 0)
                            {
                                outStream.Write(safeBuffer.Value, 0, length);
                            }
                        }
                    }

                    outStream.Seek(0, SeekOrigin.Begin);
                }
                catch (Exception)
                {
                    if (outStream != null)
                    {
                        outStream.Dispose();
                    }

                    throw;
                }

                return outStream;
            }
            catch (Exception e)
            {
                throw new ArgumentException(e.Message, e);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        private static Stream Decrypt(Stream stream, IExchangeDecrypt privateKey)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (privateKey == null) throw new ArgumentNullException(nameof(privateKey));

            try
            {
                int type = (int)VintUtils.Get(stream);

                if (type == (int)ConvertCryptoAlgorithm.Aes256)
                {
                    byte[] cryptoKey;

                    {
                        int length = (int)VintUtils.Get(stream);

                        var encryptedBuffer = new byte[length];
                        if (stream.Read(encryptedBuffer, 0, encryptedBuffer.Length) != encryptedBuffer.Length) throw new ArgumentException();

                        cryptoKey = Exchange.Decrypt(privateKey, encryptedBuffer);
                    }

                    var iv = new byte[32];
                    stream.Read(iv, 0, iv.Length);

                    BufferStream outStream = null;

                    try
                    {
                        outStream = new BufferStream(_bufferManager);

                        using (var aes = Aes.Create())
                        {
                            aes.KeySize = 256;
                            aes.Mode = CipherMode.CBC;
                            aes.Padding = PaddingMode.PKCS7;

                            using (var inStream = new RangeStream(stream, stream.Position, stream.Length - stream.Position, true))
                            using (var cs = new CryptoStream(inStream, aes.CreateDecryptor(cryptoKey, iv), CryptoStreamMode.Read))
                            using (var safeBuffer = _bufferManager.CreateSafeBuffer(1024 * 4))
                            {
                                int length;

                                while ((length = cs.Read(safeBuffer.Value, 0, safeBuffer.Value.Length)) > 0)
                                {
                                    outStream.Write(safeBuffer.Value, 0, length);
                                }
                            }
                        }

                        outStream.Seek(0, SeekOrigin.Begin);
                    }
                    catch (Exception)
                    {
                        if (outStream != null)
                        {
                            outStream.Dispose();
                        }

                        throw;
                    }

                    return outStream;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException(e.Message, e);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        private static Stream AddHash(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var hashStream = new BufferStream(_bufferManager);
            {
                VintUtils.Write(hashStream, (int)ConvertHashAlgorithm.Sha256);
                var value = Sha256.ComputeHash(new WrapperStream(stream, true));
                hashStream.Write(value, 0, value.Length);
            }

            return new UniteStream(hashStream, stream);
        }

        private static Stream RemoveHash(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            int type = (int)VintUtils.Get(stream);

            if (type == (int)ConvertHashAlgorithm.Sha256)
            {
                var value = new byte[32];
                stream.Read(value, 0, value.Length);

                var dataStream = new RangeStream(stream, true);
                if (!Unsafe.Equals(value, Sha256.ComputeHash(new WrapperStream(dataStream, true)))) throw new ArgumentException("Hash");

                dataStream.Seek(0, SeekOrigin.Begin);
                return dataStream;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static Stream AddPadding(Stream stream, int size)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            try
            {
                var lengthStream = new BufferStream(_bufferManager);
                VintUtils.Write(lengthStream, stream.Length);

                Stream paddingStream;
                {
                    Random random;
                    {
                        var seedBuffer = new byte[4];
                        _random.GetBytes(seedBuffer);
                        random = new Random(NetworkConverter.ToInt32(seedBuffer));
                    }

                    int paddingLength = size - (int)(lengthStream.Length + stream.Length);

                    paddingStream = new BufferStream(_bufferManager);

                    using (var safeBuffer = _bufferManager.CreateSafeBuffer(1024 * 4))
                    {
                        while (paddingLength > 0)
                        {
                            int writeSize = Math.Min(paddingLength, safeBuffer.Value.Length);

                            random.NextBytes(safeBuffer.Value);
                            paddingStream.Write(safeBuffer.Value, 0, writeSize);

                            paddingLength -= writeSize;
                        }
                    }
                }

                return new UniteStream(lengthStream, stream, paddingStream);
            }
            catch (Exception e)
            {
                if (stream != null)
                {
                    stream.Dispose();
                }

                throw new ArgumentException(e.Message, e);
            }
        }

        private static Stream RemovePadding(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            try
            {
                int length = (int)VintUtils.Get(stream);
                return new RangeStream(stream, stream.Position, length);
            }
            catch (Exception e)
            {
                if (stream != null)
                {
                    stream.Dispose();
                }

                throw new ArgumentException(e.Message, e);
            }
        }

        public static Stream ToStream<T>(int version, T message)
            where T : ItemBase<T>
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            try
            {
                return AddVersion(Compress(message.Export(_bufferManager)), version);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static T FromStream<T>(int version, Stream stream)
            where T : ItemBase<T>
        {
            if (stream == null) throw new ArgumentException("stream", nameof(stream));

            try
            {
                return ItemBase<T>.Import(Decompress(RemoveVersion(stream, version)), _bufferManager);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Stream ToCryptoStream<T>(int version, T message, int paddingSize, IExchangeEncrypt publicKey)
            where T : ItemBase<T>
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (publicKey == null) throw new ArgumentNullException(nameof(publicKey));

            try
            {
                return AddVersion(AddHash(Encrypt(AddPadding(Compress(message.Export(_bufferManager)), paddingSize), publicKey)), version);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static T FromCryptoStream<T>(int version, Stream stream, IExchangeDecrypt privateKey)
            where T : ItemBase<T>
        {
            if (stream == null) throw new ArgumentException("stream", nameof(stream));
            if (privateKey == null) throw new ArgumentNullException(nameof(privateKey));

            try
            {
                return ItemBase<T>.Import(Decompress(RemovePadding(RemoveHash(Decrypt(RemoveVersion(stream, version), privateKey)))), _bufferManager);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
