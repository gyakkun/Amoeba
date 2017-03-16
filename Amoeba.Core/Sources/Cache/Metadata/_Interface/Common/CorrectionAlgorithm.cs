using System.Runtime.Serialization;

namespace Amoeba.Core
{
    [DataContract(Name = nameof(CorrectionAlgorithm))]
    enum CorrectionAlgorithm : byte
    {
        [EnumMember(Value = "ReedSolomon8")]
        ReedSolomon8 = 0,
    }
}
