using System;

namespace Amoeba.Core
{
    interface ITag
    {
        string Name { get; }
        byte[] Id { get; }
    }
}
