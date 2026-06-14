using Elektrika.Application.DTOs;
using Elektrika.Application.Interfaces;
using Elektrika.Domain.Entities;
using Elektrika.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Elektrika.Infrastructure.Services;

public sealed class PriceService : IPriceService
{
    private readonly AppDbContext _context;

    public PriceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PriceCatalogDto> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        return await BuildCatalogAsync(activeOnly: true, cancellationToken);
    }

    public async Task<PriceCatalogDto> GetAllAdminAsync(CancellationToken cancellationToken = default)
    {
        return await BuildCatalogAsync(activeOnly: false, cancellationToken);
    }

    public async Task<PriceItemDto> UpdateItemAsync(
        UpdatePriceItemDto dto,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.PriceItems
            .FirstOrDefaultAsync(i => i.Id == dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Price item '{dto.Id}' was not found.");

        var categoryExists = await _context.PriceCategories
            .AnyAsync(c => c.Id == dto.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new KeyNotFoundException($"Price category '{dto.CategoryId}' was not found.");
        }

        item.CategoryId = dto.CategoryId;
        item.Name = dto.Name;
        item.Unit = dto.Unit;
        item.Price = dto.Price;
        item.SortOrder = dto.SortOrder;

        await _context.SaveChangesAsync(cancellationToken);

        return MapItem(item);
    }

    public async Task<PriceItemDto> CreateItemAsync(
        Guid categoryId,
        string legacyKey,
        string name,
        string unit,
        decimal price,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var categoryExists = await _context.PriceCategories
            .AnyAsync(c => c.Id == categoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new KeyNotFoundException($"Price category '{categoryId}' was not found.");
        }

        var legacyKeyExists = await _context.PriceItems
            .AnyAsync(i => i.LegacyKey == legacyKey, cancellationToken);

        if (legacyKeyExists)
        {
            throw new InvalidOperationException($"Price item with legacy key '{legacyKey}' already exists.");
        }

        var item = new PriceItem
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            LegacyKey = legacyKey,
            Name = name,
            Unit = unit,
            Price = price,
            SortOrder = sortOrder,
            IsActive = true,
        };

        _context.PriceItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return MapItem(item);
    }

    public async Task<PriceItemDto> SetItemActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.PriceItems
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Price item '{id}' was not found.");

        item.IsActive = isActive;
        await _context.SaveChangesAsync(cancellationToken);

        return MapItem(item);
    }

    private async Task<PriceCatalogDto> BuildCatalogAsync(
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var categoriesQuery = _context.PriceCategories.AsQueryable();
        var itemsQuery = _context.PriceItems.AsQueryable();
        var surchargesQuery = _context.Surcharges.AsQueryable();

        if (activeOnly)
        {
            itemsQuery = itemsQuery.Where(i => i.IsActive);
            surchargesQuery = surchargesQuery.Where(s => s.IsActive);
        }

        var categories = await categoriesQuery
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

        var items = await itemsQuery
            .OrderBy(i => i.SortOrder)
            .ToListAsync(cancellationToken);

        var surcharges = await surchargesQuery
            .OrderBy(s => s.LegacyKey)
            .ToListAsync(cancellationToken);

        var itemsByCategory = items
            .GroupBy(i => i.CategoryId)
            .ToDictionary(g => g.Key, g => g.Select(MapItem).ToList());

        var categoryDtos = categories
            .Where(c => !activeOnly || itemsByCategory.ContainsKey(c.Id))
            .Select(c => new PriceCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                SortOrder = c.SortOrder,
                Items = itemsByCategory.TryGetValue(c.Id, out var categoryItems)
                    ? categoryItems
                    : Array.Empty<PriceItemDto>(),
            })
            .ToList();

        return new PriceCatalogDto
        {
            Categories = categoryDtos,
            Surcharges = surcharges.Select(MapSurcharge).ToList(),
            VisitFee = PriceSeedData.VisitFee,
        };
    }

    private static PriceItemDto MapItem(PriceItem item) => new()
    {
        Id = item.Id,
        CategoryId = item.CategoryId,
        LegacyKey = item.LegacyKey,
        Name = item.Name,
        Unit = item.Unit,
        Price = item.Price,
        SortOrder = item.SortOrder,
        IsActive = item.IsActive,
    };

    private static SurchargeDto MapSurcharge(Surcharge surcharge) => new()
    {
        Id = surcharge.Id,
        LegacyKey = surcharge.LegacyKey,
        Label = surcharge.Label,
        Percent = surcharge.Percent,
    };
}
