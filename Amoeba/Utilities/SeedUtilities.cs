using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Library;
using Library.Net.Amoeba;
using Library.Security;

namespace Amoeba
{
    static class SeedUtilities
    {
        private static ConditionalWeakTable<Seed, byte[]> _caches = new ConditionalWeakTable<Seed, byte[]>();

        public static byte[] GetHash(Seed seed)
        {
            return _caches.GetValue(seed,
                (_) =>
                {
                    var swap = new Seed();

                    lock (seed.ThisLock)
                    {
                        swap.Rank = seed.Rank;
                        swap.Key = seed.Key;

                        swap.CompressionAlgorithm = seed.CompressionAlgorithm;

                        swap.CryptoAlgorithm = seed.CryptoAlgorithm;
                        swap.CryptoKey = seed.CryptoKey;
                    }

                    using (var stream = swap.Export(BufferManager.Instance))
                    {
                        return Sha256.ComputeHash(stream);
                    }
                });
        }
    }
}
