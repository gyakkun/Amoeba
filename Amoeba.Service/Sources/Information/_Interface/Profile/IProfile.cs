using System.Collections.Generic;
using Omnius.Security;

namespace Amoeba.Service
{
    interface IProfile
    {
        ExchangePublicKey ExchangePublicKey { get; }
        Link Link { get; }
    }
}
