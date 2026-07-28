namespace SE26Project_18.Api.Models.VectorProfiles;

internal sealed record RecruitmentVectorProfile(
    long RecruitmentId,
    ReadOnlyMemory<float>? RecruitmentTagVector
);
