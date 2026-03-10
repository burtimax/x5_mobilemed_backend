using System.Text;
using Microsoft.Extensions.Options;
using ModuleTelegramLogger.Configuration;
using ModuleTelegramLogger.Models;

namespace ModuleTelegramLogger;

/// <summary>
/// Реализация отправки ошибок в Telegram через Bot API sendDocument.
/// </summary>
internal sealed class TelegramErrorSender : ITelegramErrorSender
{
    private const int TelegramCaptionMaxLength = 1024;
    private const int MaxPerRun = 10;

    private readonly ITelegramErrorQueue _queue;
    private readonly TelegramErrorConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public TelegramErrorSender(
        ITelegramErrorQueue queue,
        IOptions<TelegramErrorConfiguration> config,
        IHttpClientFactory httpClientFactory)
    {
        _queue = queue;
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendPendingErrorsAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
            return;

        var token = _config.BotToken ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_config.ChatId))
            return;

        var client = _httpClientFactory.CreateClient();
        var processed = 0;

        while (processed < MaxPerRun && _queue.TryDequeue(out var entry) && entry is not null)
        {
            try
            {
                await SendToTelegramAsync(client, token, entry, cancellationToken);
                await Task.Delay(100);
                processed++;
            }
            catch
            {
                try
                {
                    Console.Error.WriteLine($"[TelegramErrorSender] Не удалось отправить ошибку в Telegram: {entry.Message}");
                }
                catch { /* игнорируем */ }
            }
        }
    }

    private async Task SendToTelegramAsync(
        HttpClient client,
        string token,
        ErrorLogEntry entry,
        CancellationToken ct)
    {
        var caption = BuildCaption(entry);
        var txtContent = BuildTxtContent(entry);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(_config.ChatId), "chat_id");
        content.Add(new StringContent(caption), "caption");
        content.Add(new StringContent("html"), "parse_mode");

        var fileName = $"error_{entry.TimestampUtc.Replace(":", "-").Replace(".", "-")}.txt";
        var txtBytes = Encoding.UTF8.GetBytes(txtContent);
        var streamContent = new StreamContent(new MemoryStream(txtBytes));
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain")
        {
            CharSet = "utf-8"
        };
        content.Add(streamContent, "document", fileName);

        var url = $"https://api.telegram.org/bot{token}/sendDocument";
        using var response = await client.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();
    }

    private static string BuildCaption(ErrorLogEntry entry)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🛑 <b>{entry.ServiceTitle}</b>");
        sb.AppendLine($"<b>Сервис:</b> {entry.ServiceName}");
        sb.Append("<b>Уровень:</b> ").AppendLine(entry.Level);
        if (!string.IsNullOrEmpty(entry.ExceptionType))
            sb.Append("<b>Исключение:</b> ").AppendLine(entry.ExceptionType);
        sb.Append("<b>Сообщение:</b> ").Append(TruncateMessage(entry.Message, 200));

        var result = sb.ToString();
        if (result.Length > TelegramCaptionMaxLength)
            result = result[..TelegramCaptionMaxLength];
        return result;
    }

    private static string TruncateMessage(string msg, int maxLen)
    {
        if (string.IsNullOrEmpty(msg)) return string.Empty;
        if (msg.Length <= maxLen) return msg;
        return msg[..maxLen] + "...";
    }

    private static string BuildTxtContent(ErrorLogEntry entry)
    {
        var sb = new StringBuilder();
        sb.Append("Title: ").AppendLine(entry.ServiceTitle);
        sb.Append("Service: ").AppendLine(entry.ServiceName);
        sb.Append("Level: ").AppendLine(entry.Level);
        sb.Append("Timestamp: ").AppendLine(entry.TimestampUtc);
        sb.AppendLine();
        sb.AppendLine("Message:");
        sb.AppendLine(entry.Message);
        sb.AppendLine();

        if (!string.IsNullOrEmpty(entry.ExceptionToString))
        {
            sb.AppendLine("Exception:");
            sb.AppendLine(entry.ExceptionToString);
            sb.AppendLine();
        }

        if (entry.Properties.Count > 0)
        {
            sb.AppendLine("Properties:");
            foreach (var (key, value) in entry.Properties)
            {
                sb.Append(key).Append(": ").AppendLine(value?.ToString() ?? "(null)");
            }
        }

        return sb.ToString();
    }
}
