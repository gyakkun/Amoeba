using System.Runtime.Serialization;

namespace Amoeba.Core
{
    [DataContract(Name = "CorrectionAlgorithm")]
    enum CorrectionAlgorithm : byte
    {
        [EnumMember(Value = "ReedSolomon8")]
        ReedSolomon8 = 0,
    }
}
