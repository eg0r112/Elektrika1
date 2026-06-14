using Elektrika.Application.DTOs;
using Elektrika.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elektrika.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/prices")]
public sealed class AdminPricesController : ControllerBase
{
    private readonly IPriceService _priceService;

    public AdminPricesController(IPriceService priceService)
    {
        _priceService = priceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var catalog = await _priceService.GetAllAdminAsync(cancellationToken);
        return Ok(catalog);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePriceItemDto dto, CancellationToken cancellationToken)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { error = "Id в URL и теле запроса не совпадают." });
        }

        var item = await _priceService.UpdateItemAsync(dto, cancellationToken);
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePriceItemRequest request, CancellationToken cancellationToken)
    {
        var item = await _priceService.CreateItemAsync(
            request.CategoryId,
            request.LegacyKey,
            request.Name,
            request.Unit,
            request.Price,
            request.SortOrder,
            cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { id = item.Id }, item);
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetActiveRequest request, CancellationToken cancellationToken)
    {
        var item = await _priceService.SetItemActiveAsync(id, request.IsActive, cancellationToken);
        return Ok(item);
    }
}

public sealed class CreatePriceItemRequest
{
    public Guid CategoryId { get; init; }

    public string LegacyKey { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int SortOrder { get; init; }
}

public sealed class SetActiveRequest
{
    public bool IsActive { get; init; }
}
