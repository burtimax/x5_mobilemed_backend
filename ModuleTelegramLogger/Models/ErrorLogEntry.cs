namespace ModuleTelegramLogger.Models;

/// <summary>
/// Запись об ошибке для отправки в Telegram.
/// </summary>
public sealed class ErrorLogEntry
{
    public required string ServiceTitle { get; init; }
    public required string ServiceName { get; init; }
    public required string Level { get; init; }
    public required string Message { get; init; }
    public string? ExceptionType { get; init; }
    public string? ExceptionToString { get; init; }
    public required string TimestampUtc { get; init; }
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
}
