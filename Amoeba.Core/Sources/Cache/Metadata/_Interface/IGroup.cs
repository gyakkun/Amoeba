using System.Collections.Generic;

namespace Amoeba.Core
{
    interface IGroup
    {
        CorrectionAlgorithm CorrectionAlgorithm { get; }
        long Length { get; }
        IEnumerable<Hash> Hashes { get; }
    }
}
