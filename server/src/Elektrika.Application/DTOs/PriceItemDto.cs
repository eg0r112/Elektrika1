namespace Elektrika.Application.DTOs;

public sealed class PriceItemDto
{
    public Guid Id { get; init; }

    public Guid CategoryId { get; init; }

    public string LegacyKey { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int SortOrder { get; init; }

    public bool IsActive { get; init; }
}
