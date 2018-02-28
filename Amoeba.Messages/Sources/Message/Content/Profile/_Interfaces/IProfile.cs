using System.Collections.Generic;
using Omnius.Security;

namespace Amoeba.Messages
{
    interface IProfile
    {
        string Comment { get; }
        AgreementPublicKey AgreementPublicKey { get; }
        IEnumerable<Signature> TrustSignatures { get; }
        IEnumerable<Signature> DeleteSignatures { get; }
        IEnumerable<Tag> Tags { get; }
    }
}
