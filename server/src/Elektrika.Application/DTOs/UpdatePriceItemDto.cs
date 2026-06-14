namespace Elektrika.Application.DTOs;

public sealed class UpdatePriceItemDto
{
    public Guid Id { get; init; }

    public Guid CategoryId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int SortOrder { get; init; }
}
