using System.Collections.Generic;

namespace Amoeba.Service
{
    interface IIndex
    {
        IEnumerable<Group> Groups { get; }
    }
}
