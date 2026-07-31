using SE26Project_18.Backend.Models;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Tests.Services;

public class EnumExtensionsTests
{
    // ==================== Gender ====================
    [Theory]
    [InlineData(Gender.Male, "男")]
    [InlineData(Gender.Female, "女")]
    [InlineData(Gender.Other, "其他")]
    public void Gender_ToDtoString(Gender input, string expected)
        => Assert.Equal(expected, input.ToDtoString());

    [Theory]
    [InlineData("男", Gender.Male)]
    [InlineData("女", Gender.Female)]
    [InlineData("其他", Gender.Other)]
    [InlineData("unknown", Gender.Other)]
    public void ToGender(string input, Gender expected)
        => Assert.Equal(expected, input.ToGender());

    // ==================== UserStatus ====================
    [Theory]
    [InlineData("正常", UserStatus.Normal)]
    [InlineData("封禁", UserStatus.Banned)]
    [InlineData("注销", UserStatus.Deleted)]
    [InlineData("unknown", UserStatus.Normal)]
    public void ToUserStatus(string input, UserStatus expected)
        => Assert.Equal(expected, input.ToUserStatus());

    // ==================== RecruitmentStatus ====================
    [Theory]
    [InlineData("招募中", RecruitmentStatus.Open)]
    [InlineData("已关闭", RecruitmentStatus.Closed)]
    [InlineData("已删除", RecruitmentStatus.Deleted)]
    public void ToRecruitmentStatus(string input, RecruitmentStatus expected)
        => Assert.Equal(expected, input.ToRecruitmentStatus());

    // ==================== ChatStatus ====================
    [Theory]
    [InlineData("限制", ChatStatus.Restricted)]
    [InlineData("开放", ChatStatus.Open)]
    [InlineData("关闭", ChatStatus.Closed)]
    public void ToChatStatus(string input, ChatStatus expected)
        => Assert.Equal(expected, input.ToChatStatus());

    // ==================== ResponseStatus ====================
    [Theory]
    [InlineData("已回应", ResponseStatus.Responded)]
    [InlineData("已删除", ResponseStatus.Deleted)]
    public void ToResponseStatus(string input, ResponseStatus expected)
        => Assert.Equal(expected, input.ToResponseStatus());

    // ==================== ReportStatus ====================
    [Theory]
    [InlineData("待处理", ReportStatus.Pending)]
    [InlineData("已处理", ReportStatus.Resolved)]
    [InlineData("驳回", ReportStatus.Rejected)]
    [InlineData("pending", ReportStatus.Pending)]
    [InlineData("resolved", ReportStatus.Resolved)]
    [InlineData("rejected", ReportStatus.Rejected)]
    [InlineData("unknown", ReportStatus.Pending)]
    public void ToReportStatus(string input, ReportStatus expected)
        => Assert.Equal(expected, input.ToReportStatus());

    // ==================== FeedbackStatus ====================
    [Theory]
    [InlineData("待处理", FeedbackStatus.Pending)]
    [InlineData("已处理", FeedbackStatus.Resolved)]
    [InlineData("pending", FeedbackStatus.Pending)]
    [InlineData("resolved", FeedbackStatus.Resolved)]
    [InlineData("unknown", FeedbackStatus.Pending)]
    public void ToFeedbackStatus(string input, FeedbackStatus expected)
        => Assert.Equal(expected, input.ToFeedbackStatus());
}
