using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amoeba.Messages;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Security;
using Xunit;
using Xunit.Abstractions;

namespace Amoeba.UnitTests
{
    [Trait("Category", "Amoeba.Service")]
    public class CacheManagerTests : TestSetupBase, IDisposable
    {
        private BufferManager _bufferManager = BufferManager.Instance;
        private CacheManager _cacheManager;

        private readonly string _workPath = "Test_CacheManager";
        private readonly Random _random = new Random();

        public CacheManagerTests(ITestOutputHelper output) : base(output)
        {
            if (Directory.Exists(_workPath)) Directory.Delete(_workPath, true);
            Directory.CreateDirectory(_workPath);

            string targetPath = Path.Combine(_workPath, "Main");
            string configPath = Path.Combine(targetPath, "CacheManager");
            string blockPath = Path.Combine(targetPath, "cache.blocks"); ;

            Directory.CreateDirectory(targetPath);
            Directory.CreateDirectory(configPath);

            _cacheManager = new CacheManager(configPath, blockPath, _bufferManager);
            _cacheManager.Load();
            _cacheManager.Resize(1024 * 1024 * 256);
        }

        public void Dispose()
        {
            _cacheManager.Dispose();

            if (Directory.Exists(_workPath)) Directory.Delete(_workPath, true);
        }

        [Fact]
        public void ReadWriteTest()
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

                    Assert.True(Unsafe.Equals(block.Array, block.Offset, result.Array, result.Offset, size));

                    _bufferManager.ReturnBuffer(result.Array);
                }
            }
        }

        [Fact]
        public void CheckBrokenTest()
        {
            string targetPath = Path.Combine(_workPath, "CheckBroken");
            string configPath = Path.Combine(targetPath, "CacheManager");
            string blockPath = Path.Combine(targetPath, "cache.blocks"); ;

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
                int b = stream.ReadByte();
                stream.Seek(0, SeekOrigin.Begin);
                stream.WriteByte((byte)(b ^ 0xFF));
            }

            {
                var cacheManager = new CacheManager(configPath, blockPath, _bufferManager);
                cacheManager.Load();

                Assert.True(new HashSet<Hash>(list).SetEquals(cacheManager.ToArray()));

                Assert.Throws<BlockNotFoundException>(() =>
                {
                    var result = cacheManager[list[0]];
                });

                foreach (var hash in list.Skip(1))
                {
                    var result = cacheManager[hash];
                    _bufferManager.ReturnBuffer(result.Array);
                }

                cacheManager.Save();
                cacheManager.Dispose();
            }
        }
    }
}
