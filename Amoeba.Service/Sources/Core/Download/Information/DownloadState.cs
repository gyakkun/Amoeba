using System.Runtime.Serialization;

namespace Amoeba.Service
{
    [DataContract(Name = nameof(DownloadState))]
    public enum DownloadState
    {
        [EnumMember(Value = "Downloading")]
        Downloading,

        [EnumMember(Value = "ParityDecoding")]
        ParityDecoding,

        [EnumMember(Value = "Decoding")]
        Decoding,

        [EnumMember(Value = "Completed")]
        Completed,

        [EnumMember(Value = "Error")]
        Error,
    }
}
