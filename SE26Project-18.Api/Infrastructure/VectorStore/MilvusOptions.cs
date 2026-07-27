using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Infrastructure.VectorStore;

internal sealed class MilvusOptions
{
    public const string SectionName = "Milvus";

    [Required]
    public required string HostName { get; init; }

    [Range(1, 65535)]
    public int Port { get; init; }

    public bool UseTls { get; init; }

    [Required]
    public required string DatabaseName { get; init; }

    public string? UserName { get; init; }

    public string? Password { get; init; }
}
