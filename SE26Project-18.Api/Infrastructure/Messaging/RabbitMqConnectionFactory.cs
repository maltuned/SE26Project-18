using RabbitMQ.Client;

namespace SE26Project_18.Api.Infrastructure.Messaging;

internal static class RabbitMqConnectionFactory
{
    public static ConnectionFactory Create(
        RabbitMqOptions options,
        string clientProvidedName,
        bool automaticRecoveryEnabled
    )
    {
        return new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            AutomaticRecoveryEnabled = automaticRecoveryEnabled,
            TopologyRecoveryEnabled = automaticRecoveryEnabled,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(options.RecoveryDelaySeconds),
            RequestedConnectionTimeout = TimeSpan.FromSeconds(options.ConnectionTimeoutSeconds),
            ContinuationTimeout = TimeSpan.FromSeconds(options.ConnectionTimeoutSeconds),
            ClientProvidedName = clientProvidedName,
            Ssl = new SslOption
            {
                Enabled = options.UseTls,
                ServerName = options.HostName,
                CheckCertificateRevocation = options.UseTls,
            },
        };
    }
}
