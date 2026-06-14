using Elektrika.Application.Interfaces;
using Elektrika.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elektrika.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/orders")]
public sealed class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetAllAsync(cancellationToken);
        return Ok(orders);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(OrderStatus), request.Status))
        {
            return BadRequest(new { error = "Некорректный статус." });
        }

        var order = await _orderService.UpdateStatusAsync(id, request.Status, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }
}

public sealed class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; init; }
}
