namespace Elektrika.Application.Interfaces;

public interface IOrderNotificationPublisher
{
    Task<bool> TryPublishAsync(Guid orderId, CancellationToken cancellationToken = default);
}
