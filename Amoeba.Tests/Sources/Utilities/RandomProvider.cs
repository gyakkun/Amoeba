using System;
using System.Security.Cryptography;
using System.Threading;

namespace Amoeba.Tests
{
    // http://neue.cc/2013/03/06_399.html
    public static class RandomProvider
    {
        private static ThreadLocal<Random> _randomWrapper = new ThreadLocal<Random>(() =>
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var buffer = new byte[sizeof(int)];
                rng.GetBytes(buffer);
                return new Random(BitConverter.ToInt32(buffer, 0));
            }
        });

        public static Random GetThreadRandom()
        {
            return _randomWrapper.Value;
        }
    }
}
