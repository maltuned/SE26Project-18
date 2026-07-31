using System.Runtime.Serialization;

namespace SE26Project_18.Backend.Models.Enums;

public enum ReviewStatus
{
    [EnumMember(Value = "显示")]
    Visible,

    [EnumMember(Value = "隐藏")]
    Hidden,
}