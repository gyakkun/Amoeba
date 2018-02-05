using System.Collections.Generic;

namespace Amoeba.Messages
{
    interface IBox
    {
        string Name { get; }
        IEnumerable<Seed> Seeds { get; }
        IEnumerable<Box> Boxes { get; }
    }
}
