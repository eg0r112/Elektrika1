using System.Text.Json;
using Elektrika.Application.DTOs;
using Elektrika.Application.Interfaces;
using Elektrika.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Elektrika.Admin.Pages;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IOrderService _orderService;

    public IndexModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public IReadOnlyList<OrderDto> Orders { get; private set; } = Array.Empty<OrderDto>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Orders = await _orderService.GetAllAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostStatusAsync(Guid orderId, OrderStatus status, CancellationToken cancellationToken)
    {
        await _orderService.UpdateStatusAsync(orderId, status, cancellationToken);
        return RedirectToPage();
    }

    public string StatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.New => "Новая",
        OrderStatus.InProgress => "В работе",
        OrderStatus.Completed => "Выполнена",
        OrderStatus.Cancelled => "Отменена",
        _ => status.ToString(),
    };

    public string StatusClass(OrderStatus status) => status switch
    {
        OrderStatus.New => "status-new",
        OrderStatus.InProgress => "status-progress",
        OrderStatus.Completed => "status-done",
        OrderStatus.Cancelled => "status-cancel",
        _ => string.Empty,
    };

    public OrderDetailsView? GetOrderDetails(OrderDto order)
    {
        if (string.IsNullOrWhiteSpace(order.EstimateJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(order.EstimateJson);
            var root = document.RootElement;

            var lines = root.TryGetProperty("lines", out var linesElement)
                ? linesElement.EnumerateArray()
                    .Select(line => new EstimateLineView
                    {
                        Name = line.GetProperty("name").GetString() ?? "—",
                        Quantity = line.GetProperty("quantity").GetInt32(),
                        Unit = line.GetProperty("unit").GetString() ?? "",
                        LineTotal = line.GetProperty("lineTotal").GetDecimal(),
                    })
                    .ToList()
                : [];

            var surcharges = root.TryGetProperty("surcharges", out var surchargesElement)
                ? surchargesElement.EnumerateArray()
                    .Select(s => new EstimateSurchargeView
                    {
                        Label = s.GetProperty("label").GetString() ?? "—",
                        Percent = s.GetProperty("percent").GetInt32(),
                        Amount = s.GetProperty("amount").GetDecimal(),
                    })
                    .ToList()
                : [];

            var visitFee = root.TryGetProperty("visitFee", out var visitFeeElement)
                ? visitFeeElement.GetDecimal()
                : 0m;

            if (lines.Count == 0 && surcharges.Count == 0 && visitFee <= 0)
            {
                return null;
            }

            return new OrderDetailsView
            {
                Lines = lines,
                Surcharges = surcharges,
                VisitFee = visitFee,
                Total = order.Total,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public sealed class OrderDetailsView
    {
        public IReadOnlyList<EstimateLineView> Lines { get; init; } = [];

        public IReadOnlyList<EstimateSurchargeView> Surcharges { get; init; } = [];

        public decimal VisitFee { get; init; }

        public decimal Total { get; init; }
    }

    public sealed class EstimateLineView
    {
        public string Name { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public string Unit { get; init; } = string.Empty;

        public decimal LineTotal { get; init; }
    }

    public sealed class EstimateSurchargeView
    {
        public string Label { get; init; } = string.Empty;

        public int Percent { get; init; }

        public decimal Amount { get; init; }
    }
}
