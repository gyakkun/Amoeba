using System.Runtime.Serialization;

namespace Amoeba.Messages
{
    [DataContract(Name = nameof(DownloadState))]
    public enum DownloadState
    {
        [EnumMember(Value = nameof(Downloading))]
        Downloading,

        [EnumMember(Value = nameof(ParityDecoding))]
        ParityDecoding,

        [EnumMember(Value = nameof(Decoding))]
        Decoding,

        [EnumMember(Value = nameof(Completed))]
        Completed,

        [EnumMember(Value = nameof(Error))]
        Error,
    }
}
