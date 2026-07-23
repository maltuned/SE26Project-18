using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Dtos;

public sealed record ChatDto(
    long Id,
    long? RecruitmentId,
    long RecruiterId,
    long ResponserId,
    ChatStatus Status,
    int NewMsgsCntForRecruiter,
    int NewMsgsCntForResponser,
    LastMessageDto? LastMessage
);

