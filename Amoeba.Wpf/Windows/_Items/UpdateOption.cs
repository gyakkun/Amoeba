using System.Runtime.Serialization;

namespace Amoeba.Windows
{
    [DataContract(Name = "UpdateOption")]
    enum UpdateOption
    {
        [EnumMember(Value = "None")]
        None = 0,

        [EnumMember(Value = "AutoCheck")]
        Check = 1,

        [EnumMember(Value = "AutoUpdate")]
        Update = 2,
    }
}
