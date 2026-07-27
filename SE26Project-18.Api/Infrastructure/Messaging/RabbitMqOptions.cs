using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Infrastructure.Messaging;

internal sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public required string HostName { get; init; }

    [Range(1, 65535)]
    public int Port { get; init; }

    [Required]
    public required string UserName { get; init; }

    [Required]
    public required string Password { get; init; }

    [Required]
    public required string VirtualHost { get; init; }

    [Required]
    public required string ExchangeName { get; init; }

    [Required]
    public required string DeadLetterExchangeName { get; init; }

    [Range(1, ushort.MaxValue)]
    public int PrefetchCount { get; init; }

    public bool UseTls { get; init; }

    [Range(1, 300)]
    public int ConnectionTimeoutSeconds { get; init; }

    [Range(1, 300)]
    public int RecoveryDelaySeconds { get; init; }

    [Range(0, 100)]
    public int MaxRetryAttempts { get; init; }

    [Range(1, 3600)]
    public int RetryDelaySeconds { get; init; }

    [Range(1, int.MaxValue)]
    public int DeadLetterQueueMaxLength { get; init; }
}
