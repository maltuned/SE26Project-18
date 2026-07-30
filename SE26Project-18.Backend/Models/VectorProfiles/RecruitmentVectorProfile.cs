namespace SE26Project_18.Backend.Models.VectorProfiles;

internal sealed record RecruitmentVectorProfile(
    long RecruitmentId,
    ReadOnlyMemory<float>? RecruitmentTagVector
);
