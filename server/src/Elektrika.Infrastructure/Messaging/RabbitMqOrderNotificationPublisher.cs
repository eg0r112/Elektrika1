using System.Text;
using System.Text.Json;
using Elektrika.Application.Interfaces;
using Elektrika.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Elektrika.Infrastructure.Messaging;

public sealed class RabbitMqOrderNotificationPublisher : IOrderNotificationPublisher
{
    private const int ConfirmTimeoutSeconds = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqOrderNotificationPublisher> _logger;
    private readonly object _channelSync = new();
    private IModel? _channel;

    public RabbitMqOrderNotificationPublisher(
        RabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqOrderNotificationPublisher> logger)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
        _logger = logger;
    }

    public Task<bool> TryPublishAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = GetOrCreateChannel();
            var message = new OrderNotificationMessage(orderId);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = orderId.ToString();

            channel.BasicPublish(
                exchange: string.Empty,
                routingKey: _options.OrderQueueName,
                mandatory: false,
                basicProperties: properties,
                body: body);

            if (!channel.WaitForConfirms(TimeSpan.FromSeconds(ConfirmTimeoutSeconds)))
            {
                _logger.LogError(
                    "RabbitMQ did not confirm publish for order {OrderId} within {Timeout}s.",
                    orderId,
                    ConfirmTimeoutSeconds);
                ResetChannel();
                return Task.FromResult(false);
            }

            _logger.LogInformation("Order {OrderId} published and confirmed to queue {Queue}.", orderId, _options.OrderQueueName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish order {OrderId} to RabbitMQ.", orderId);
            ResetChannel();
            return Task.FromResult(false);
        }
    }

    private IModel GetOrCreateChannel()
    {
        lock (_channelSync)
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            ResetChannelUnsafe();

            var connection = _connectionProvider.GetConnection();
            _channel = connection.CreateModel();
            _channel.ConfirmSelect();
            _channel.QueueDeclare(
                queue: _options.OrderQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            return _channel;
        }
    }

    private void ResetChannel()
    {
        lock (_channelSync)
        {
            ResetChannelUnsafe();
        }
    }

    private void ResetChannelUnsafe()
    {
        try
        {
            _channel?.Close();
        }
        catch
        {
            // Ignore shutdown errors on a broken channel.
        }

        _channel?.Dispose();
        _channel = null;
    }
}
