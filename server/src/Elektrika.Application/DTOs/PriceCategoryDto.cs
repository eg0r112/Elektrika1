namespace Elektrika.Application.DTOs;

public sealed class PriceCategoryDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public IReadOnlyList<PriceItemDto> Items { get; init; } = Array.Empty<PriceItemDto>();
}
