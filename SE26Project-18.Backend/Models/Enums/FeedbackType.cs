using System.Runtime.Serialization;

namespace SE26Project_18.Backend.Models.Enums;

public enum FeedbackType
{
    [EnumMember(Value = "内容反馈")]
    ContentFeedback,

    [EnumMember(Value = "体验反馈")]
    ExperienceFeedback,
}