using System.Runtime.Serialization;

namespace SE26Project_18.Backend.Models.Enums;

public enum UserStatus
{
    [EnumMember(Value = "正常")]
    Normal,

    [EnumMember(Value = "封禁")]
    Banned,

    [EnumMember(Value = "注销")]
    Deleted,
}
