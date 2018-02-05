using System.Runtime.Serialization;

namespace Amoeba.Messages
{
    [DataContract(Name = nameof(HashAlgorithm))]
    public enum HashAlgorithm : byte
    {
        [EnumMember(Value = "Sha256")]
        Sha256 = 0,
    }
}
