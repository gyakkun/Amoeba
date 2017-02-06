using System;
using System.Collections.Generic;

namespace Amoeba.Service
{
    interface IBox
    {
        string Name { get; }
        ICollection<Seed> Seeds { get; }
        ICollection<Box> Boxes { get; }
    }
}
