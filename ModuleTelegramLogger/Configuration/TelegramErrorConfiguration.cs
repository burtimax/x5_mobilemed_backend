namespace ModuleTelegramLogger.Configuration;

/// <summary>
/// Конфигурация отправки ошибок в Telegram-группу через бота.
/// </summary>
public class TelegramErrorConfiguration
{
    public const string Section = "TelegramError";

    /// <summary>
    /// Включена ли отправка ошибок в Telegram.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Заголовок/модуль сервиса (напр. "Transcriptor API").
    /// </summary>
    public string ServiceTitle { get; set; } = string.Empty;

    /// <summary>
    /// Название сервиса из конфигурации (напр. "x5-mobilemed").
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Токен Telegram-бота. Можно переопределить через Bot:TelegramToken.
    /// </summary>
    public string? BotToken { get; set; } = String.Empty;

    /// <summary>
    /// ChatId группы или супергруппы (числовой, напр. -1001234567890).
    /// </summary>
    public string ChatId { get; set; } = string.Empty;

    /// <summary>
    /// Максимальный размер очереди ошибок. При переполнении удаляется самая старая запись (DropOldest).
    /// </summary>
    public int QueueCapacity { get; set; } = 500;
}
