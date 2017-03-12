using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Amoeba.IntegrationTest
{
    // http://neue.cc/2013/03/06_399.html
    public static class RandomProvider
    {
        private static ThreadLocal<Random> _randomWrapper = new ThreadLocal<Random>(() =>
        {
            using (var rng = new RNGCryptoServiceProvider())
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
