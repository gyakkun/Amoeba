using System.Collections.Generic;

namespace Amoeba.Messages
{
    interface IStore
    {
        IEnumerable<Box> Boxes { get; }
    }
}
