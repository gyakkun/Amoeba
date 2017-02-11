using System.Collections.Generic;
using Omnius.Security;

namespace Amoeba.Service
{
    interface IProfile
    {
        string Comment { get; }
        ExchangePublicKey ExchangePublicKey { get; }
        Link Link { get; }
    }
}
