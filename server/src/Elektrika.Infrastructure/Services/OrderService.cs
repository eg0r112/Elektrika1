using System.Text.Json;

using Elektrika.Application.DTOs;

using Elektrika.Application.Interfaces;

using Elektrika.Domain.Entities;

using Elektrika.Domain.Enums;

using Elektrika.Infrastructure.Data;

using Elektrika.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;



namespace Elektrika.Infrastructure.Services;



public sealed class OrderService : IOrderService

{

    private const int MaxMessageLength = 4000;



    private static readonly JsonSerializerOptions JsonOptions = new()

    {

        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

        WriteIndented = false,

    };



    private readonly AppDbContext _context;
    private readonly IOrderNotificationPublisher _publisher;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        AppDbContext context,
        IOrderNotificationPublisher publisher,
        ILogger<OrderService> logger)
    {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }



    public async Task<OrderDto> CreateAsync(

        CreateOrderDto dto,

        CancellationToken cancellationToken = default)

    {

        if (!dto.ClientRequestId.HasValue || dto.ClientRequestId == Guid.Empty)

        {

            throw new ArgumentException("ClientRequestId is required.");

        }



        if (string.IsNullOrWhiteSpace(dto.CustomerName))

        {

            throw new ArgumentException("Укажите имя.");

        }



        if (string.IsNullOrWhiteSpace(dto.Phone))

        {

            throw new ArgumentException("Укажите телефон.");

        }



        var existingOrder = await _context.OrderRequests

            .AsNoTracking()

            .FirstOrDefaultAsync(o => o.ClientRequestId == dto.ClientRequestId, cancellationToken);



        if (existingOrder is not null)

        {

            return MapOrderPublic(existingOrder);

        }



        var customerName = InputSanitizer.Trim(dto.CustomerName, 200);

        var phone = PhoneNormalizer.Normalize(dto.Phone);

        var message = InputSanitizer.TrimOptional(dto.Message, MaxMessageLength);



        if (dto.Lines.Count == 0 && string.IsNullOrWhiteSpace(message))

        {

            throw new ArgumentException("Добавьте позиции в калькулятор или напишите комментарий.");

        }



        OrderEstimate estimate;

        decimal subtotal;

        decimal surchargeTotal;

        decimal visitFee;

        decimal total;



        if (dto.Lines.Count == 0)

        {

            estimate = new OrderEstimate();

            subtotal = 0;

            surchargeTotal = 0;

            visitFee = 0;

            total = 0;

        }

        else

        {

            (estimate, subtotal, surchargeTotal, visitFee, total) =

                await BuildEstimateAsync(dto, cancellationToken);

        }



        var order = new OrderRequest

        {

            Id = Guid.NewGuid(),

            ClientRequestId = dto.ClientRequestId,

            CustomerName = customerName,

            Phone = phone,

            Message = message,

            EstimateJson = dto.Lines.Count == 0 ? null : JsonSerializer.Serialize(estimate, JsonOptions),

            Subtotal = subtotal,

            SurchargeTotal = surchargeTotal,

            VisitFee = visitFee,

            Total = total,

            Status = OrderStatus.New,

            TelegramStatus = NotificationDeliveryStatus.Pending,

            TelegramAttempts = 0,

            CreatedAtUtc = DateTime.UtcNow,

        };



        _context.OrderRequests.Add(order);



        try

        {

            await _context.SaveChangesAsync(cancellationToken);

        }

        catch (DbUpdateException)

        {

            var racedOrder = await _context.OrderRequests

                .AsNoTracking()

                .FirstOrDefaultAsync(o => o.ClientRequestId == dto.ClientRequestId, cancellationToken);



            if (racedOrder is not null)

            {

                return MapOrderPublic(racedOrder);

            }



            throw;

        }

        var published = await _publisher.TryPublishAsync(order.Id, cancellationToken);
        if (!published)
        {
            _logger.LogWarning(
                "Order {OrderId} saved but RabbitMQ publish failed; republish worker will retry.",
                order.Id);
        }

        return MapOrderPublic(order);

    }



    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default)

    {

        var orders = await _context.OrderRequests

            .OrderByDescending(o => o.CreatedAtUtc)

            .ToListAsync(cancellationToken);



        return orders.Select(MapOrderPublic).ToList();

    }



    public async Task<OrderDto?> UpdateStatusAsync(

        Guid id,

        OrderStatus status,

        CancellationToken cancellationToken = default)

    {

        var order = await _context.OrderRequests

            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);



        if (order is null)

        {

            return null;

        }



        order.Status = status;

        order.UpdatedAtUtc = DateTime.UtcNow;



        await _context.SaveChangesAsync(cancellationToken);



        return MapOrderPublic(order);

    }



    public static OrderDto MapOrderPublic(OrderRequest order) => new()

    {

        Id = order.Id,

        CustomerName = order.CustomerName,

        Phone = order.Phone,

        Message = order.Message,

        EstimateJson = order.EstimateJson,

        Subtotal = order.Subtotal,

        SurchargeTotal = order.SurchargeTotal,

        VisitFee = order.VisitFee,

        Total = order.Total,

        Status = order.Status,

        CreatedAtUtc = order.CreatedAtUtc,

        UpdatedAtUtc = order.UpdatedAtUtc,

    };



    private async Task<(OrderEstimate Estimate, decimal Subtotal, decimal SurchargeTotal, decimal VisitFee, decimal Total)>

        BuildEstimateAsync(CreateOrderDto dto, CancellationToken cancellationToken)

    {

        var legacyKeys = dto.Lines

            .Select(l => l.LegacyKey)

            .Distinct(StringComparer.Ordinal)

            .ToList();



        var priceItems = await _context.PriceItems

            .Where(i => i.IsActive && legacyKeys.Contains(i.LegacyKey))

            .ToDictionaryAsync(i => i.LegacyKey, StringComparer.Ordinal, cancellationToken);



        foreach (var line in dto.Lines)

        {

            if (!priceItems.ContainsKey(line.LegacyKey))

            {

                throw new KeyNotFoundException($"Active price item '{line.LegacyKey}' was not found.");

            }



            if (line.Quantity <= 0 || line.Quantity > 9999)

            {

                throw new ArgumentException($"Quantity must be between 1 and 9999 for item '{line.LegacyKey}'.");

            }

        }



        var estimateLines = dto.Lines

            .Select(line =>

            {

                var item = priceItems[line.LegacyKey];

                var lineTotal = item.Price * line.Quantity;



                return new EstimateLine

                {

                    LegacyKey = item.LegacyKey,

                    Name = item.Name,

                    Unit = item.Unit,

                    Price = item.Price,

                    Quantity = line.Quantity,

                    LineTotal = lineTotal,

                };

            })

            .ToList();



        var subtotal = estimateLines.Sum(l => l.LineTotal);



        var surchargeKeys = dto.SurchargeKeys

            .Distinct(StringComparer.Ordinal)

            .ToList();



        var surcharges = surchargeKeys.Count == 0

            ? []

            : await _context.Surcharges

                .Where(s => s.IsActive && surchargeKeys.Contains(s.LegacyKey))

                .ToListAsync(cancellationToken);



        foreach (var key in surchargeKeys)

        {

            if (surcharges.All(s => s.LegacyKey != key))

            {

                throw new KeyNotFoundException($"Active surcharge '{key}' was not found.");

            }

        }



        decimal surchargeTotal = 0m;

        var appliedSurcharges = new List<EstimateSurcharge>();



        foreach (var surcharge in surcharges)

        {

            var amount = Math.Round(subtotal * surcharge.Percent / 100m, MidpointRounding.AwayFromZero);

            surchargeTotal += amount;

            appliedSurcharges.Add(new EstimateSurcharge

            {

                LegacyKey = surcharge.LegacyKey,

                Label = surcharge.Label,

                Percent = surcharge.Percent,

                Amount = amount,

            });

        }



        var visitFee = dto.IncludeVisit ? PriceSeedData.VisitFee : 0m;

        var total = subtotal + surchargeTotal + visitFee;



        var estimate = new OrderEstimate

        {

            Lines = estimateLines,

            Surcharges = appliedSurcharges,

            Subtotal = subtotal,

            SurchargeTotal = surchargeTotal,

            VisitFee = visitFee,

            Total = total,

            IncludeVisit = dto.IncludeVisit,

        };



        return (estimate, subtotal, surchargeTotal, visitFee, total);

    }



    private sealed class OrderEstimate

    {

        public IReadOnlyList<EstimateLine> Lines { get; init; } = [];



        public IReadOnlyList<EstimateSurcharge> Surcharges { get; init; } = [];



        public decimal Subtotal { get; init; }



        public decimal SurchargeTotal { get; init; }



        public decimal VisitFee { get; init; }



        public decimal Total { get; init; }



        public bool IncludeVisit { get; init; }

    }



    private sealed class EstimateLine

    {

        public string LegacyKey { get; init; } = string.Empty;



        public string Name { get; init; } = string.Empty;



        public string Unit { get; init; } = string.Empty;



        public decimal Price { get; init; }



        public int Quantity { get; init; }



        public decimal LineTotal { get; init; }

    }



    private sealed class EstimateSurcharge

    {

        public string LegacyKey { get; init; } = string.Empty;



        public string Label { get; init; } = string.Empty;



        public int Percent { get; init; }



        public decimal Amount { get; init; }

    }

}


