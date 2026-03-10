namespace ModuleLLM.Configuration;

/// <summary>
/// Конфигурация GigaChat API
/// </summary>
public class GigaChatApiConfiguration
{
    public const string Section = "GigaChat";

    /// <summary>
    /// URL для получения OAuth токена
    /// </summary>
    public string AuthUrl { get; set; } = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

    /// <summary>
    /// Базовый URL API чата
    /// </summary>
    public string ChatBaseUrl { get; set; } = "https://gigachat.devices.sberbank.ru/api/v1";

    /// <summary>
    /// Client ID для OAuth (Authorization: Basic base64(ClientId:ClientSecret))
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client Secret для OAuth
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Scope для OAuth (GIGACHAT_API_PERS)
    /// </summary>
    public string Scope { get; set; } = "GIGACHAT_API_PERS";

    /// <summary>
    /// Модель LLM
    /// </summary>
    public string Model { get; set; } = "GigaChat-2";

    /// <summary>
    /// Максимальное количество токенов в ответе
    /// </summary>
    public int MaxTokens { get; set; } = 512;

    public int Timeout { get; set; } = 120;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Запас времени в миллисекундах до истечения токена для его обновления (по умолчанию 5 мин)
    /// </summary>
    public int TokenExpiryBufferMs { get; set; } = 300_000;

    /// <summary>
    /// Игнорировать ошибки SSL-сертификата (для ngw.devices.sberbank.ru с UntrustedRoot).
    /// Использовать только в development или при доверенной сети.
    /// </summary>
    public bool IgnoreSslCertificateErrors { get; set; } = false;
}
