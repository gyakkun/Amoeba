using System.Collections.Generic;

namespace Amoeba.Messages
{
    interface ILocation
    {
        IEnumerable<string> Uris { get; }
    }
}
