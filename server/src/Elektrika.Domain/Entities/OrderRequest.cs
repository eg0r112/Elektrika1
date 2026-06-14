using Elektrika.Domain.Enums;

namespace Elektrika.Domain.Entities;

public class OrderRequest
{
    public Guid Id { get; set; }

    public Guid? ClientRequestId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Message { get; set; }

    public string? EstimateJson { get; set; }

    public decimal Subtotal { get; set; }

    public decimal SurchargeTotal { get; set; }

    public decimal VisitFee { get; set; }

    public decimal Total { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.New;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public NotificationDeliveryStatus TelegramStatus { get; set; } = NotificationDeliveryStatus.Pending;

    public int TelegramAttempts { get; set; }

    public DateTime? TelegramSentAtUtc { get; set; }

    public DateTime? TelegramNextRetryAtUtc { get; set; }
}
