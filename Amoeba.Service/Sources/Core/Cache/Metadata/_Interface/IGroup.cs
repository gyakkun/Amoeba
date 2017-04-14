using System.Collections.Generic;

namespace Amoeba.Service
{
    interface IGroup
    {
        CorrectionAlgorithm CorrectionAlgorithm { get; }
        long Length { get; }
        IEnumerable<Hash> Hashes { get; }
    }
}
