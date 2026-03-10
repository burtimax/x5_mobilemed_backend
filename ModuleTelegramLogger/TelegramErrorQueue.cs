using System.Threading.Channels;
using ModuleTelegramLogger.Models;

namespace ModuleTelegramLogger;

/// <summary>
/// Ограниченная очередь для ошибок. При переполнении удаляется самая старая запись (DropOldest).
/// Потокобезопасна.
/// </summary>
public interface ITelegramErrorQueue
{
    bool TryEnqueue(ErrorLogEntry entry);
    bool TryDequeue(out ErrorLogEntry? entry);
}

public sealed class TelegramErrorQueue : ITelegramErrorQueue
{
    private readonly Channel<ErrorLogEntry> _channel;

    public TelegramErrorQueue(int capacity)
    {
        if (capacity < 1)
            capacity = 100;

        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<ErrorLogEntry>(options);
    }

    public bool TryEnqueue(ErrorLogEntry entry)
    {
        return _channel.Writer.TryWrite(entry);
    }

    public bool TryDequeue(out ErrorLogEntry? entry)
    {
        return _channel.Reader.TryRead(out entry);
    }
}
