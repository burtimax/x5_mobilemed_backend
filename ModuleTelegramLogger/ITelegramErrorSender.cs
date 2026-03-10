namespace ModuleTelegramLogger;

/// <summary>
/// Сервис отправки ошибок из очереди в Telegram-группу.
/// </summary>
public interface ITelegramErrorSender
{
    /// <summary>
    /// Забирает ошибки из очереди и отправляет в Telegram (не более maxPerRun за вызов).
    /// Ошибки отправки не логируются в ILogger (защита от рекурсии).
    /// </summary>
    Task SendPendingErrorsAsync(CancellationToken cancellationToken = default);
}
