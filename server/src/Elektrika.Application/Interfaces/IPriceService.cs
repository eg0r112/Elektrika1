using Elektrika.Application.DTOs;

namespace Elektrika.Application.Interfaces;

public interface IPriceService
{
    Task<PriceCatalogDto> GetCatalogAsync(CancellationToken cancellationToken = default);

    Task<PriceCatalogDto> GetAllAdminAsync(CancellationToken cancellationToken = default);

    Task<PriceItemDto> UpdateItemAsync(UpdatePriceItemDto dto, CancellationToken cancellationToken = default);

    Task<PriceItemDto> CreateItemAsync(
        Guid categoryId,
        string legacyKey,
        string name,
        string unit,
        decimal price,
        int sortOrder,
        CancellationToken cancellationToken = default);

    Task<PriceItemDto> SetItemActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
