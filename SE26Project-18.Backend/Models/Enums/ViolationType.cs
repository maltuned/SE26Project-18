using System.Runtime.Serialization;

namespace SE26Project_18.Backend.Models.Enums;

public enum ViolationType
{
    [EnumMember(Value = "涉政")]
    Political,

    [EnumMember(Value = "谩骂")]
    Abuse,

    [EnumMember(Value = "广告")]
    Advertisement,

    [EnumMember(Value = "色情")]
    Pornography,

    [EnumMember(Value = "欺诈")]
    Fraud,

    [EnumMember(Value = "其他")]
    Other,
}