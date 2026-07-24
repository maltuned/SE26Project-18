using System.Runtime.Serialization;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Models;

public static class EnumExtensions
{
    public static string ToDisplayString(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attr = field?.GetCustomAttributes(typeof(EnumMemberAttribute), false)
            .Cast<EnumMemberAttribute>()
            .FirstOrDefault();
        return attr?.Value ?? value.ToString();
    }

    // Gender
    public static string ToDtoString(this Gender value) => value.ToDisplayString();
    public static Gender ToGender(this string value) => value switch
    {
        "男" => Gender.Male,
        "女" => Gender.Female,
        "其他" => Gender.Other,
        _ => Gender.Other
    };

    // UserStatus
    public static string ToDtoString(this UserStatus value) => value.ToDisplayString();
    public static UserStatus ToUserStatus(this string value) => value switch
    {
        "正常" => UserStatus.Normal,
        "封禁" => UserStatus.Banned,
        "注销" => UserStatus.Deleted,
        _ => UserStatus.Normal
    };

    // RecruitmentStatus
    public static string ToDtoString(this RecruitmentStatus value) => value.ToDisplayString();
    public static RecruitmentStatus ToRecruitmentStatus(this string value) => value switch
    {
        "招募中" => RecruitmentStatus.Open,
        "已关闭" => RecruitmentStatus.Closed,
        "已删除" => RecruitmentStatus.Deleted,
        _ => RecruitmentStatus.Open
    };

    // ChatStatus
    public static string ToDtoString(this ChatStatus value) => value.ToDisplayString();
    public static ChatStatus ToChatStatus(this string value) => value switch
    {
        "限制" => ChatStatus.Restricted,
        "开放" => ChatStatus.Open,
        "关闭" => ChatStatus.Closed,
        _ => ChatStatus.Restricted
    };

    // ResponseStatus
    public static string ToDtoString(this ResponseStatus value) => value.ToDisplayString();
    public static ResponseStatus ToResponseStatus(this string value) => value switch
    {
        "已回应" => ResponseStatus.Responded,
        "已删除" => ResponseStatus.Deleted,
        _ => ResponseStatus.Responded
    };
}
