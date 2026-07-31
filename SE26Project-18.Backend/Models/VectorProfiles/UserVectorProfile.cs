namespace SE26Project_18.Backend.Models.VectorProfiles;

internal sealed record UserVectorProfile(
    long UserId,
    ReadOnlyMemory<float>? RecruitmentTagVector,
    ReadOnlyMemory<float>? GameTagVector
);
