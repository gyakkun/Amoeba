using System;
using System.Runtime.Serialization;

namespace Amoeba.Interface
{
    [Flags]
    enum ChatMessageState
    {
        None = 0,
        New = 0x01,
    }
}
