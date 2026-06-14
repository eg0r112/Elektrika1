using Elektrika.Domain.Enums;

namespace Elektrika.Application.DTOs;

public sealed class OrderDto
{
    public Guid Id { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string? Message { get; init; }

    public string? EstimateJson { get; init; }

    public decimal Subtotal { get; init; }

    public decimal SurchargeTotal { get; init; }

    public decimal VisitFee { get; init; }

    public decimal Total { get; init; }

    public OrderStatus Status { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}
