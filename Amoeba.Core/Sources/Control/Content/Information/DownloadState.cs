using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Core
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
