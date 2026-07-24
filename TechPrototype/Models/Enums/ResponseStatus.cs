using System.Runtime.Serialization;

namespace SE26Project_18.Backend.Models.Enums;

public enum ResponseStatus
{
    [EnumMember(Value = "已回应")]
    Responded,

    [EnumMember(Value = "已删除")]
    Deleted,
}
