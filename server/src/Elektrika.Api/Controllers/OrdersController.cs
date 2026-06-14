using Elektrika.Application.DTOs;

using Elektrika.Application.Interfaces;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;



namespace Elektrika.Api.Controllers;



[ApiController]

[Route("api/[controller]")]

[RequestSizeLimit(32_768)]

public sealed class OrdersController : ControllerBase

{

    private readonly IOrderService _orderService;



    public OrdersController(IOrderService orderService)

    {

        _orderService = orderService;

    }



    [HttpPost]

    [EnableRateLimiting("orders")]

    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]

    [ProducesResponseType(StatusCodes.Status400BadRequest)]

    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)

    {

        if (!string.IsNullOrWhiteSpace(dto.Website))

        {

            return Created(string.Empty, new OrderDto

            {

                Id = Guid.NewGuid(),

                CustomerName = "ok",

                Phone = "+70000000000",

                CreatedAtUtc = DateTime.UtcNow,

            });

        }



        if (!dto.ClientRequestId.HasValue || dto.ClientRequestId == Guid.Empty)

        {

            return BadRequest(new { error = "Некорректный идентификатор заявки." });

        }



        if (string.IsNullOrWhiteSpace(dto.CustomerName))

        {

            return BadRequest(new { error = "Укажите имя." });

        }



        if (string.IsNullOrWhiteSpace(dto.Phone))

        {

            return BadRequest(new { error = "Укажите телефон." });

        }



        var order = await _orderService.CreateAsync(dto, cancellationToken);

        return Created(string.Empty, order);

    }

}


