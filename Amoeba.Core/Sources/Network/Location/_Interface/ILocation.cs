using System.Collections.Generic;

namespace Amoeba.Core
{
    interface ILocation
    {
        IEnumerable<string> Uris { get; }
    }
}
