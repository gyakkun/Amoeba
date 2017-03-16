using System.Collections.Generic;
using Amoeba.Core;
using Omnius.Security;

namespace Amoeba.Service
{
    interface IProfile
    {
        string Comment { get; }
        ExchangePublicKey ExchangePublicKey { get; }
        IEnumerable<Signature> TrustSignatures { get; }
        IEnumerable<Signature> DeleteSignatures { get; }
        IEnumerable<Tag> Tags { get; }
    }
}
