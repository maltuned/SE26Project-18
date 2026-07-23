namespace SE26Project_18.Api.Models.Dtos;

public sealed record CreateChatRequest(long[] UserIds, long RecruitmentId = 0);

