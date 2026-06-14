namespace Elektrika.Domain.Entities;

public class PriceCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<PriceItem> PriceItems { get; set; } = new List<PriceItem>();
}
