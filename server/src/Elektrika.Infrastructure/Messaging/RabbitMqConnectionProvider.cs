using Elektrika.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Elektrika.Infrastructure.Messaging;

public sealed class RabbitMqConnectionProvider : IDisposable
{
    private readonly object _sync = new();
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqConnectionProvider> _logger;
    private IConnection? _connection;

    public RabbitMqConnectionProvider(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqConnectionProvider> logger)
    {
        var config = options.Value;
        _factory = new ConnectionFactory
        {
            HostName = config.Host,
            Port = config.Port,
            UserName = config.Username,
            Password = config.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
        };
        _logger = logger;
    }

    public IConnection GetConnection()
    {
        lock (_sync)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _connection?.Dispose();
            _connection = _factory.CreateConnection();
            _logger.LogInformation("RabbitMQ connection established.");
            return _connection;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}
