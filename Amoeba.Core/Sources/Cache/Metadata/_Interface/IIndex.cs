using System.Collections.Generic;

namespace Amoeba.Core
{
    interface IIndex
    {
        IEnumerable<Group> Groups { get; }
    }
}
