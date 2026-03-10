using Microsoft.Extensions.Options;
using ModuleTelegramLogger.Configuration;
using ModuleTelegramLogger.Models;
using Serilog.Core;
using Serilog.Events;

namespace ModuleTelegramLogger;

/// <summary>
/// Serilog Sink: добавляет в очередь только события уровня Error и выше.
/// Не блокирует вызывающий поток — TryEnqueue выполняется мгновенно.
/// </summary>
public sealed class TelegramErrorSerilogSink : ILogEventSink
{
    private readonly ITelegramErrorQueue _queue;
    private readonly TelegramErrorConfiguration _config;

    public TelegramErrorSerilogSink(
        ITelegramErrorQueue queue,
        IOptions<TelegramErrorConfiguration> config)
    {
        _queue = queue;
        _config = config.Value;
    }

    public void Emit(LogEvent logEvent)
    {
        if (!_config.Enabled)
            return;

        if (logEvent.Level < LogEventLevel.Error)
            return;

        var entry = ToErrorLogEntry(logEvent);
        _queue.TryEnqueue(entry);
    }

    private ErrorLogEntry ToErrorLogEntry(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage();
        var ex = logEvent.Exception;
        var props = new Dictionary<string, object?>();

        foreach (var (key, value) in logEvent.Properties)
        {
            props[key] = value.ToString();
        }

        return new ErrorLogEntry
        {
            ServiceTitle = _config.ServiceTitle,
            ServiceName = _config.ServiceName,
            Level = logEvent.Level.ToString(),
            Message = message,
            ExceptionType = ex?.GetType().FullName,
            ExceptionToString = ex?.ToString(),
            TimestampUtc = logEvent.Timestamp.UtcDateTime.ToString("O"),
            Properties = props
        };
    }
}
