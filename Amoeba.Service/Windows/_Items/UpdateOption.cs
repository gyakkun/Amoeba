using System.Runtime.Serialization;

namespace Amoeba.Windows
{
    [DataContract(Name = "UpdateOption", Namespace = "http://Amoeba/Windows")]
    enum UpdateOption
    {
        [EnumMember(Value = "None")]
        None = 0,

        [EnumMember(Value = "AutoCheck")]
        AutoCheck = 1,

        [EnumMember(Value = "AutoUpdate")]
        AutoUpdate = 2,
    }
}
