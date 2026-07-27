namespace SE26Project_18.Api.Models.VectorProfiles;

internal sealed record UserVectorProfile(
    long UserId,
    ReadOnlyMemory<float> UserTagVector,
    ReadOnlyMemory<float> RecruitmentTagVector,
    ReadOnlyMemory<float> GameTagVector
);
