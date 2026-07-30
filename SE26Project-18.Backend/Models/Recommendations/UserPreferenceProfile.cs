namespace SE26Project_18.Backend.Models.Recommendations;

internal sealed record UserPreferenceProfile(
    ReadOnlyMemory<float>? RecruitmentTagVector,
    ReadOnlyMemory<float>? GameTagVector
);
