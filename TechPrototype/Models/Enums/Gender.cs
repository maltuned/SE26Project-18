using System.Runtime.Serialization;

namespace SE26Project_18.Backend.Models.Enums;

public enum Gender
{
    [EnumMember(Value = "男")]
    Male,

    [EnumMember(Value = "女")]
    Female,

    [EnumMember(Value = "其他")]
    Other,
}
