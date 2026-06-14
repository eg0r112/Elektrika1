using Elektrika.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Elektrika.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PricesController : ControllerBase
{
    private readonly IPriceService _priceService;

    public PricesController(IPriceService priceService)
    {
        _priceService = priceService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var catalog = await _priceService.GetCatalogAsync(cancellationToken);
        return Ok(catalog);
    }
}
