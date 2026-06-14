using Elektrika.Application.DTOs;
using Elektrika.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Elektrika.Admin.Pages;

[Authorize]
public sealed class PricesModel : PageModel
{
    private readonly IPriceService _priceService;

    public PricesModel(IPriceService priceService)
    {
        _priceService = priceService;
    }

    public PriceCatalogDto Catalog { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Catalog = await _priceService.GetAllAdminAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        Guid id,
        Guid categoryId,
        string name,
        string unit,
        decimal price,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        await _priceService.UpdateItemAsync(new UpdatePriceItemDto
        {
            Id = id,
            CategoryId = categoryId,
            Name = name,
            Unit = unit,
            Price = price,
            SortOrder = sortOrder,
        }, cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        await _priceService.SetItemActiveAsync(id, isActive, cancellationToken);
        return RedirectToPage();
    }
}
