namespace Elektrika.Application.DTOs;

public sealed class PriceCatalogDto
{
    public IReadOnlyList<PriceCategoryDto> Categories { get; init; } = Array.Empty<PriceCategoryDto>();

    public IReadOnlyList<SurchargeDto> Surcharges { get; init; } = Array.Empty<SurchargeDto>();

    public decimal VisitFee { get; init; }
}
