namespace Shared.Configs;

/// <summary>
/// Конфигурация прокси-сервера для HttpClient
/// </summary>
public class ProxyConfiguration
{
    public const string Section = "Proxy";

    /// <summary>
    /// Включен ли прокси
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Протокол прокси (socks5, http, https)
    /// </summary>
    public string Protocol { get; set; } = "socks5";

    /// <summary>
    /// IP адрес прокси-сервера
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Порт прокси-сервера
    /// </summary>
    public int Port { get; set; } = 1080;

    /// <summary>
    /// Имя пользователя для аутентификации (опционально)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Пароль для аутентификации (опционально)
    /// </summary>
    public string? Password { get; set; }
}

