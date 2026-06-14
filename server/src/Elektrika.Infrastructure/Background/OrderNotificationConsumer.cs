using System.Text;
using System.Text.Json;
using Elektrika.Application.Interfaces;
using Elektrika.Application.Options;
using Elektrika.Domain.Enums;
using Elektrika.Infrastructure.Data;
using Elektrika.Infrastructure.Messaging;
using Elektrika.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Elektrika.Infrastructure.Background;

public sealed class OrderNotificationConsumer : BackgroundService
{
    private const int MaxTelegramAttempts = 20;
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderNotificationConsumer> _logger;

    public OrderNotificationConsumer(
        RabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<OrderNotificationConsumer> logger)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ consumer disconnected, reconnecting in {Delay}s.", ReconnectDelay.TotalSeconds);

                try
                {
                    await Task.Delay(ReconnectDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Order notification consumer stopped.");
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        IConnection connection;
        IModel channel;

        try
        {
            connection = _connectionProvider.GetConnection();
            channel = connection.CreateModel();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to RabbitMQ.");
            throw;
        }

        using var channelDisposable = channel;

        channel.QueueDeclare(
            queue: _options.OrderQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var reconnectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void RequestReconnect(object? sender, ShutdownEventArgs args)
        {
            if (args.Initiator == ShutdownInitiator.Application)
            {
                return;
            }

            _logger.LogWarning(
                "RabbitMQ channel shutdown ({Reason}), reconnecting.",
                string.IsNullOrWhiteSpace(args.ReplyText) ? args.ReplyCode.ToString() : args.ReplyText);

            reconnectTcs.TrySetResult();
        }

        connection.ConnectionShutdown += RequestReconnect;
        channel.ModelShutdown += RequestReconnect;

        try
        {
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (_, eventArgs) =>
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                HandleMessageAsync(eventArgs, channel, stoppingToken)
                    .GetAwaiter()
                    .GetResult();
            };

            channel.BasicConsume(
                queue: _options.OrderQueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("Listening to RabbitMQ queue {Queue}.", _options.OrderQueueName);

            stoppingToken.Register(() => reconnectTcs.TrySetCanceled());

            await reconnectTcs.Task;
        }
        finally
        {
            connection.ConnectionShutdown -= RequestReconnect;
            channel.ModelShutdown -= RequestReconnect;
        }
    }

    private async Task HandleMessageAsync(
        BasicDeliverEventArgs eventArgs,
        IModel channel,
        CancellationToken cancellationToken)
    {
        OrderNotificationMessage? message;

        try
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.Span);
            message = JsonSerializer.Deserialize<OrderNotificationMessage>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid RabbitMQ message, rejecting without requeue.");
            channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (message is null || message.OrderId == Guid.Empty)
        {
            channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        try
        {
            await ProcessOrderAsync(message.OrderId, cancellationToken);
            channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process order notification {OrderId}, requeueing.", message.OrderId);
            channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private async Task ProcessOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifier = scope.ServiceProvider.GetRequiredService<ITelegramNotifier>();

        var order = await context.OrderRequests
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} from queue was not found in database.", orderId);
            return;
        }

        if (order.TelegramStatus == NotificationDeliveryStatus.Sent)
        {
            return;
        }

        if (order.TelegramStatus == NotificationDeliveryStatus.Failed)
        {
            _logger.LogWarning("Order {OrderId} has failed Telegram delivery, skipping.", orderId);
            return;
        }

        var orderDto = OrderService.MapOrderPublic(order);
        var sent = await notifier.TryNotifyOrderAsync(orderDto, cancellationToken);
        var now = DateTime.UtcNow;

        order.TelegramAttempts++;
        order.UpdatedAtUtc = now;

        if (sent)
        {
            order.TelegramStatus = NotificationDeliveryStatus.Sent;
            order.TelegramSentAtUtc = now;
            order.TelegramNextRetryAtUtc = null;
        }
        else if (order.TelegramAttempts >= MaxTelegramAttempts)
        {
            order.TelegramStatus = NotificationDeliveryStatus.Failed;
            order.TelegramNextRetryAtUtc = null;
            _logger.LogError(
                "Order {OrderId} Telegram delivery failed after {Attempts} attempts.",
                orderId,
                order.TelegramAttempts);
        }
        else
        {
            order.TelegramStatus = NotificationDeliveryStatus.Pending;
            order.TelegramNextRetryAtUtc = now.AddMinutes(Math.Min(60, order.TelegramAttempts * 5));
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
