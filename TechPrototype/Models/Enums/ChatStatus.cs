using System.Runtime.Serialization;

namespace SE26Project_18.Backend.Models.Enums;

public enum ChatStatus
{
    [EnumMember(Value = "限制")]
    Restricted,

    [EnumMember(Value = "开放")]
    Open,

    [EnumMember(Value = "关闭")]
    Closed,
}
