namespace Elektrika.Application.DTOs;

public sealed class OrderLineDto
{
    public string LegacyKey { get; init; } = string.Empty;

    public int Quantity { get; init; }
}
