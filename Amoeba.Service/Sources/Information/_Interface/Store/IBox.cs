using System;
using System.Collections.Generic;

namespace Amoeba.Service
{
    interface IBox
    {
        string Name { get; }
        IEnumerable<Seed> Seeds { get; }
        IEnumerable<Box> Boxes { get; }
    }
}
