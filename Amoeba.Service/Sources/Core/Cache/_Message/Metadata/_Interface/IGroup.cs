using System.Collections.Generic;
using Amoeba.Messages;

namespace Amoeba.Service
{
    interface IGroup
    {
        CorrectionAlgorithm CorrectionAlgorithm { get; }
        long Length { get; }
        IEnumerable<Hash> Hashes { get; }
    }
}
