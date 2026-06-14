namespace Elektrika.Application.DTOs;

public sealed class SurchargeDto
{
    public Guid Id { get; init; }

    public string LegacyKey { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public int Percent { get; init; }
}
