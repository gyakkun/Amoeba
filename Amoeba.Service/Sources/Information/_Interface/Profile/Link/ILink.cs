using System.Collections.Generic;
using Omnius.Security;

namespace Amoeba.Service
{
    interface ILink
    {
        IEnumerable<Signature> TrustSignatures { get; }
        IEnumerable<Signature> DeleteSignatures { get; }
    }
}
