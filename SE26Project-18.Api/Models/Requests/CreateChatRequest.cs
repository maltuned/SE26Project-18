namespace SE26Project_18.Api.Models.Requests;

public sealed record CreateChatRequest(long RecruitmentId, long User1Id, long User2Id);
