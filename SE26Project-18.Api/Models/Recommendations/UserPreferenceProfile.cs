namespace SE26Project_18.Api.Models.Recommendations;

internal sealed record UserPreferenceProfile(
    ReadOnlyMemory<float>? OwnUserTagVector,
    ReadOnlyMemory<float>? InterestedUserTagVector,
    ReadOnlyMemory<float>? RecruitmentTagVector,
    ReadOnlyMemory<float>? GameTagVector
);
