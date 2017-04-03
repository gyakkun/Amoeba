using System.Runtime.Serialization;

namespace Amoeba.Service
{
    [DataContract(Name = nameof(HashAlgorithm))]
    public enum HashAlgorithm : byte
    {
        [EnumMember(Value = "Sha256")]
        Sha256 = 0,
    }
}
