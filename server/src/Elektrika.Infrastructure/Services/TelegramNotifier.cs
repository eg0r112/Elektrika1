using System.Net.Http.Json;

using System.Text;

using System.Text.Json;

using System.Text.Json.Nodes;

using System.Text.Json.Serialization;

using Elektrika.Application.DTOs;

using Elektrika.Application.Interfaces;

using Elektrika.Application.Options;

using Elektrika.Domain.Enums;

using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Options;



namespace Elektrika.Infrastructure.Services;



public sealed class TelegramNotifier : ITelegramNotifier
{
    private const int ImmediateRetries = 3;
    private const int TelegramMaxMessageLength = 4096;

    private static readonly JsonSerializerOptions JsonOptions = new()

    {

        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

    };



    private readonly HttpClient _httpClient;

    private readonly TelegramOptions _options;

    private readonly ILogger<TelegramNotifier> _logger;



    public TelegramNotifier(

        HttpClient httpClient,

        IOptions<TelegramOptions> options,

        ILogger<TelegramNotifier> logger)

    {

        _httpClient = httpClient;

        _options = options.Value;

        _logger = logger;

    }



    public async Task<bool> TryNotifyOrderAsync(OrderDto order, CancellationToken cancellationToken = default)

    {

        if (!_options.Enabled)

        {

            return true;

        }



        var chatIds = _options.GetRecipientChatIds();

        if (string.IsNullOrWhiteSpace(_options.BotToken) || chatIds.Count == 0)

        {

            _logger.LogWarning("Telegram is enabled but BotToken or ChatIds are not configured.");

            return false;

        }



        var message = TruncateMessage(BuildMessage(order));

        var requestUri = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";

        var allSucceeded = true;



        foreach (var chatId in chatIds)

        {

            var sent = await SendWithRetriesAsync(requestUri, chatId, message, order.Id, cancellationToken);

            allSucceeded &= sent;

        }



        return allSucceeded;

    }



    private async Task<bool> SendWithRetriesAsync(

        string requestUri,

        string chatId,

        string message,

        Guid orderId,

        CancellationToken cancellationToken)

    {

        for (var attempt = 1; attempt <= ImmediateRetries; attempt++)

        {

            try

            {

                var payload = new TelegramSendMessageRequest

                {

                    ChatId = chatId,

                    Text = message,

                    ParseMode = "HTML",

                };



                using var response = await _httpClient.PostAsJsonAsync(

                    requestUri,

                    payload,

                    JsonOptions,

                    cancellationToken);



                if (response.IsSuccessStatusCode)

                {

                    return true;

                }



                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogWarning(

                    "Telegram API returned {StatusCode} for chat {ChatId} (attempt {Attempt}): {Body}",

                    (int)response.StatusCode,

                    chatId,

                    attempt,

                    body);



                if ((int)response.StatusCode is >= 400 and < 500 and not 429)

                {

                    return false;

                }

            }

            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)

            {

                _logger.LogWarning(ex, "Telegram send failed for order {OrderId} to chat {ChatId} (attempt {Attempt}).", orderId, chatId, attempt);

            }



            if (attempt < ImmediateRetries)

            {

                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);

            }

        }



        return false;

    }



    private static string TruncateMessage(string message)
    {
        if (message.Length <= TelegramMaxMessageLength)
        {
            return message;
        }

        const string suffix = "\n… (сообщение обрезано)";
        return message[..(TelegramMaxMessageLength - suffix.Length)] + suffix;
    }

    private static string BuildMessage(OrderDto order)

    {

        var builder = new StringBuilder();

        builder.AppendLine("<b>Новая заявка с сайта</b>");

        builder.AppendLine();

        builder.AppendLine($"<b>Клиент:</b> {EscapeHtml(order.CustomerName)}");

        builder.AppendLine($"<b>Телефон:</b> {EscapeHtml(order.Phone)}");



        AppendEstimateLines(builder, order);



        if (!string.IsNullOrWhiteSpace(order.Message))

        {

            builder.AppendLine();

            builder.AppendLine($"<b>Комментарий:</b> {EscapeHtml(order.Message)}");

        }



        if (order.Total > 0)

        {

            builder.AppendLine();

            builder.AppendLine($"<b>Подытог:</b> {order.Subtotal:N0} ₽");



            if (order.SurchargeTotal > 0)

            {

                builder.AppendLine($"<b>Надбавки:</b> {order.SurchargeTotal:N0} ₽");

            }



            if (order.VisitFee > 0)

            {

                builder.AppendLine($"<b>Выезд:</b> {order.VisitFee:N0} ₽");

            }



            builder.AppendLine($"<b>Итого:</b> {order.Total:N0} ₽");

        }



        builder.AppendLine();

        builder.AppendLine($"<b>Статус:</b> {FormatStatus(order.Status)}");

        builder.AppendLine($"<b>ID:</b> <code>{order.Id}</code>");



        return builder.ToString().TrimEnd();

    }



    private static void AppendEstimateLines(StringBuilder builder, OrderDto order)

    {

        if (string.IsNullOrWhiteSpace(order.EstimateJson))

        {

            return;

        }



        try

        {

            var root = JsonNode.Parse(order.EstimateJson)?.AsObject();

            var lines = root?["lines"]?.AsArray();

            if (lines is null || lines.Count == 0)

            {

                return;

            }



            builder.AppendLine("<b>Заказ:</b>");

            foreach (var line in lines)

            {

                var name = line?["name"]?.GetValue<string>() ?? "—";

                var unit = line?["unit"]?.GetValue<string>() ?? "";

                var quantity = line?["quantity"]?.GetValue<int>() ?? 0;

                var lineTotal = line?["lineTotal"]?.GetValue<decimal>() ?? 0m;

                builder.AppendLine($"• {EscapeHtml(name)} — {quantity} {EscapeHtml(unit)} — {lineTotal:N0} ₽");

            }

        }

        catch (JsonException)

        {

            // Ignore malformed estimate JSON.

        }

    }



    private static string FormatStatus(OrderStatus status) => status switch

    {

        OrderStatus.New => "Новая",

        OrderStatus.InProgress => "В работе",

        OrderStatus.Completed => "Завершена",

        OrderStatus.Cancelled => "Отменена",

        _ => status.ToString(),

    };



    private static string EscapeHtml(string value) =>

        value

            .Replace("&", "&amp;", StringComparison.Ordinal)

            .Replace("<", "&lt;", StringComparison.Ordinal)

            .Replace(">", "&gt;", StringComparison.Ordinal);



    private sealed class TelegramSendMessageRequest

    {

        [JsonPropertyName("chat_id")]

        public string ChatId { get; init; } = string.Empty;



        [JsonPropertyName("text")]

        public string Text { get; init; } = string.Empty;



        [JsonPropertyName("parse_mode")]

        public string ParseMode { get; init; } = string.Empty;

    }

}


