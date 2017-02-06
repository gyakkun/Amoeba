using System.Collections.Generic;

namespace Amoeba.Service
{
    interface IStore
    {
        ICollection<Box> Boxes { get; }
    }
}
