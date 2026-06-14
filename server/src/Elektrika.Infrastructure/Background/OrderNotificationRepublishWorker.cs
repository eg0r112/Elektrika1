using Elektrika.Application.Interfaces;
using Elektrika.Domain.Enums;
using Elektrika.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elektrika.Infrastructure.Background;

/// <summary>
/// Republishes orders to RabbitMQ when the broker was unavailable at order creation time.
/// </summary>
public sealed class OrderNotificationRepublishWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderNotificationRepublishWorker> _logger;

    public OrderNotificationRepublishWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderNotificationRepublishWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RepublishPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Order notification republish worker failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task RepublishPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IOrderNotificationPublisher>();
        var now = DateTime.UtcNow;

        var pendingOrders = await context.OrderRequests
            .Where(o =>
                o.TelegramStatus != NotificationDeliveryStatus.Sent &&
                o.TelegramStatus != NotificationDeliveryStatus.Failed &&
                (o.TelegramNextRetryAtUtc == null || o.TelegramNextRetryAtUtc <= now))
            .OrderBy(o => o.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var order in pendingOrders)
        {
            var published = await publisher.TryPublishAsync(order.Id, cancellationToken);
            if (published)
            {
                continue;
            }

            order.TelegramNextRetryAtUtc = now.AddMinutes(2);
            order.UpdatedAtUtc = now;
        }

        if (pendingOrders.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
