using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amoeba.Core;
using NUnit.Framework;
using Omnius.Base;
using Omnius.Security;

namespace Amoeba.Test
{
    [TestFixture(Category = "Amoeba.Core.CacheManager")]
    class Test_CacheManager
    {
        private BufferManager _bufferManager = BufferManager.Instance;
        private CacheManager _cacheManager;

        private readonly string _workPath = "Test_CacheManager";
        private readonly Random _random = new Random();

        [SetUp]
        public void Setup()
        {
            if (Directory.Exists(_workPath)) Directory.Delete(_workPath, true);
            Directory.CreateDirectory(_workPath);

            var targetPath = Path.Combine(_workPath, "Main");
            var configPath = Path.Combine(targetPath, "CacheManager");
            var blockPath = Path.Combine(targetPath, "cache.blocks"); ;

            Directory.CreateDirectory(targetPath);
            Directory.CreateDirectory(configPath);

            _cacheManager = new CacheManager(configPath, blockPath, _bufferManager);
            _cacheManager.Load();
            _cacheManager.Resize(1024 * 1024 * 256);
        }

        [TearDown]
        public void Shutdown()
        {
            _cacheManager.Dispose();

            if (Directory.Exists(_workPath)) Directory.Delete(_workPath, true);
        }

        [Test]
        public void Test_DecodeEncode()
        {
            //private IEnumerable<Hash> ParityEncoding(IEnumerable<ArraySegment<byte>> buffers, HashAlgorithm hashAlgorithm, CorrectionAlgorithm correctionAlgorithm, CancellationToken token)

            var hashes = new List<Hash>();
            var parityHashes = new List<Hash>();

            {
                var blocks = new List<ArraySegment<byte>>();

                for (int i = 0; i < 4; i++)
                {
                    var buffer = _bufferManager.TakeBuffer(1024 * 1024);
                    _random.NextBytes(buffer);

                    var block = new ArraySegment<byte>(buffer, 0, 1024 * 1024);
                    var hash = new Hash(HashAlgorithm.Sha256, Sha256.ComputeHash(block));

                    hashes.Add(hash);
                    blocks.Add(block);
                }

                using (var tokenSource = new CancellationTokenSource())
                {
                    parityHashes.AddRange(_cacheManager.ToUnlimited().ParityEncoding(blocks, HashAlgorithm.Sha256, CorrectionAlgorithm.ReedSolomon8, tokenSource.Token));
                }
            }

            {
                var group = new Group(CorrectionAlgorithm.ReedSolomon8, 1024 * 1024 * 4, CollectionUtils.Unite(hashes, parityHashes));

                using (var tokenSource = new CancellationTokenSource())
                {
                    var hashSet = new HashSet<Hash>(_cacheManager.ParityDecoding(group, tokenSource.Token).Result);
                    if (!hashSet.SetEquals(hashes)) throw new ArgumentException("Broken");
                }
            }
        }

        [Test]
        public void Test_ReadWrite()
        {
            for (int i = 0; i < 256; i++)
            {
                int size = _random.Next(1, 1024 * 1024 * 4);

                using (var safeBuffer = _bufferManager.CreateSafeBuffer(size))
                {
                    _random.NextBytes(safeBuffer.Value);

                    var block = new ArraySegment<byte>(safeBuffer.Value, 0, size);
                    var hash = new Hash(HashAlgorithm.Sha256, Sha256.ComputeHash(block));

                    _cacheManager[hash] = block;
                    var result = _cacheManager[hash];

                    Assert.IsTrue(Unsafe.Equals(block.Array, block.Offset, result.Array, result.Offset, size));

                    _bufferManager.ReturnBuffer(result.Array);
                }
            }
        }

        [Test]
        public void Test_CheckBroken()
        {
            var targetPath = Path.Combine(_workPath, "CheckBroken");
            var configPath = Path.Combine(targetPath, "CacheManager");
            var blockPath = Path.Combine(targetPath, "cache.blocks"); ;

            Directory.CreateDirectory(targetPath);
            Directory.CreateDirectory(configPath);

            var list = new List<Hash>();

            {
                var cacheManager = new CacheManager(configPath, blockPath, _bufferManager);
                cacheManager.Load();

                for (int i = 0; i < 256; i++)
                {
                    int size = _random.Next(1, 1024 * 256);

                    using (var safeBuffer = _bufferManager.CreateSafeBuffer(size))
                    {
                        _random.NextBytes(safeBuffer.Value);

                        var block = new ArraySegment<byte>(safeBuffer.Value, 0, size);
                        var hash = new Hash(HashAlgorithm.Sha256, Sha256.ComputeHash(block));

                        cacheManager[hash] = block;
                        list.Add(hash);
                    }
                }

                cacheManager.Save();
                cacheManager.Dispose();
            }

            using (var stream = new FileStream(blockPath, FileMode.Open))
            {
                var b = stream.ReadByte();
                stream.Seek(0, SeekOrigin.Begin);
                stream.WriteByte((byte)(b ^ 0xFF));
            }

            {
                var cacheManager = new CacheManager(configPath, blockPath, _bufferManager);
                cacheManager.Load();

                Assert.IsTrue(new HashSet<Hash>(list).SetEquals(cacheManager.ToArray()));

                Assert.Throws<BlockNotFoundException>(() =>
                {
                    var result = cacheManager[list[0]];
                });

                Assert.DoesNotThrow(() =>
                {
                    foreach (var hash in list.Skip(1))
                    {
                        var result = cacheManager[hash];
                        _bufferManager.ReturnBuffer(result.Array);
                    }
                });

                cacheManager.Save();
                cacheManager.Dispose();
            }
        }
    }
}
