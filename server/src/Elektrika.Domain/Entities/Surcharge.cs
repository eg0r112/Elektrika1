namespace Elektrika.Domain.Entities;

public class Surcharge
{
    public Guid Id { get; set; }

    public string LegacyKey { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public int Percent { get; set; }

    public bool IsActive { get; set; } = true;
}
