using System.Collections.Generic;

namespace Amoeba.Service
{
    interface IStore
    {
        IEnumerable<Box> Boxes { get; }
    }
}
