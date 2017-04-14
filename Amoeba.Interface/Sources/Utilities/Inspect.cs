using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Omnius.Collections;
using Omnius.Security;

namespace Amoeba.Interface
{
    static class Inspect
    {
        private static LockedHashSet<Signature> _trustSignatures = new LockedHashSet<Signature>();

        public static IEnumerable<Signature> GetTrustSignatures()
        {
            lock (_trustSignatures.LockObject)
            {
                return _trustSignatures.ToArray();
            }
        }

        public static void SetTrustSignatures(IEnumerable<Signature> signatures)
        {
            lock (_trustSignatures.LockObject)
            {
                _trustSignatures.Clear();
                _trustSignatures.UnionWith(signatures);
            }
        }

        public static bool ContainTrustSignature(Signature signature)
        {
            lock (_trustSignatures.LockObject)
            {
                return _trustSignatures.Contains(signature);
            }
        }
    }
}
