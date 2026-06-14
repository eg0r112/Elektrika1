namespace Elektrika.Application.Options;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; set; } = string.Empty;

    /// <summary>Устаревшее — используйте ChatIds.</summary>
    public string ChatId { get; set; } = string.Empty;

    public List<string> ChatIds { get; set; } = [];

    public bool Enabled { get; set; }

    public IReadOnlyList<string> GetRecipientChatIds()
    {
        if (ChatIds.Count > 0)
        {
            return ChatIds;
        }

        return string.IsNullOrWhiteSpace(ChatId) ? [] : [ChatId];
    }
}
