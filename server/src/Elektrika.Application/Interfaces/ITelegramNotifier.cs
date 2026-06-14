using Elektrika.Application.DTOs;

namespace Elektrika.Application.Interfaces;

public interface ITelegramNotifier
{
    Task<bool> TryNotifyOrderAsync(OrderDto order, CancellationToken cancellationToken = default);
}
