namespace SE26Project_18.Api.Models.VectorProfiles;

internal sealed record GameVectorProfile(long GameId, ReadOnlyMemory<float> GameTagVector);
