using Elektrika.Application.DTOs;
using Elektrika.Domain.Enums;

namespace Elektrika.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<OrderDto?> UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken cancellationToken = default);
}
