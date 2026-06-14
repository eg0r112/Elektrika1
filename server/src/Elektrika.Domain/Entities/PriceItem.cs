namespace Elektrika.Domain.Entities;

public class PriceItem
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public string LegacyKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public PriceCategory? Category { get; set; }
}
