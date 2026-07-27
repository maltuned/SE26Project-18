using System.Runtime.Serialization;

namespace SE26Project_18.Backend.Models.Enums;

public enum ReportTargetType
{
    [EnumMember(Value = "招募")]
    Recruitment,

    [EnumMember(Value = "用户")]
    User,

    [EnumMember(Value = "聊天")]
    Chat,
}