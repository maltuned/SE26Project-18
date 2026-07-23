using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Responses;

public sealed record ChatResponse(
    long Id,
    long RecruitmentId,
    long User1Id,
    long User2Id,
    ChatStatus Status,
    int NewMsgsCntForUser1,
    int NewMsgsCntForUser2,
    MessageResponse? LastMessage
);
