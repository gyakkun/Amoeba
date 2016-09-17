using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Library;
using Library.Collections;

namespace Amoeba
{
    static class Inspect
    {
        private static LockedHashSet<string> _trustSignatures = new LockedHashSet<string>();

        public static IEnumerable<string> GetTrustSignatures()
        {
            lock (_trustSignatures.ThisLock)
            {
                return _trustSignatures.ToArray();
            }
        }

        public static void SetTrustSignatures(IEnumerable<string> signatures)
        {
            lock (_trustSignatures.ThisLock)
            {
                _trustSignatures.Clear();
                _trustSignatures.UnionWith(signatures);
            }
        }

        public static bool ContainTrustSignature(string signature)
        {
            lock (_trustSignatures.ThisLock)
            {
                return _trustSignatures.Contains(signature);
            }
        }
    }
}
