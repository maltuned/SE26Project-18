namespace SE26Project_18.Backend.Models.VectorProfiles;

internal sealed record GameVectorProfile(long GameId, ReadOnlyMemory<float>? GameTagVector);
