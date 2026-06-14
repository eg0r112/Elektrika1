namespace Elektrika.Application.DTOs;

public sealed class CreateOrderDto
{
    public Guid? ClientRequestId { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string? Message { get; init; }

    /// <summary>Honeypot — must stay empty.</summary>
    public string? Website { get; init; }

    public IReadOnlyList<OrderLineDto> Lines { get; init; } = Array.Empty<OrderLineDto>();

    public IReadOnlyList<string> SurchargeKeys { get; init; } = Array.Empty<string>();

    public bool IncludeVisit { get; init; }
}
