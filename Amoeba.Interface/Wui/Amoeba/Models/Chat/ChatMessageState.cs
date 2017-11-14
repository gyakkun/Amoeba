using System;
using System.Runtime.Serialization;

namespace Amoeba.Interface
{
    [Flags]
    [DataContract(Name = nameof(ChatMessageState))]
    public enum ChatMessageState
    {
        [EnumMember(Value = "None")]
        None = 0,

        [EnumMember(Value = "New")]
        New = 0x01,
    }
}
