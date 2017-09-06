using System.Collections.Generic;
using Omnius.Security;

namespace Amoeba.Messages
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
