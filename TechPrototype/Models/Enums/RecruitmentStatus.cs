using System.Runtime.Serialization;

namespace SE26Project_18.Backend.Models.Enums;

public enum RecruitmentStatus
{
    [EnumMember(Value = "招募中")]
    Open,

    [EnumMember(Value = "已关闭")]
    Closed,

    [EnumMember(Value = "已删除")]
    Deleted,
}
