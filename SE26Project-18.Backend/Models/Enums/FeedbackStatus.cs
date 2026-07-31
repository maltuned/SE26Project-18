using System.Runtime.Serialization;

namespace SE26Project_18.Backend.Models.Enums;

public enum FeedbackStatus
{
    [EnumMember(Value = "待处理")]
    Pending,

    [EnumMember(Value = "已处理")]
    Resolved,
}
