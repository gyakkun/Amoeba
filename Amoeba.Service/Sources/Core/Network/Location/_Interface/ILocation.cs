using System.Collections.Generic;

namespace Amoeba.Service
{
    interface ILocation
    {
        IEnumerable<string> Uris { get; }
    }
}
