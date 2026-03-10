namespace ModuleLLM.Configuration;

/// <summary>
/// Конфигурация RouterAI API
/// </summary>
public class RouterAIApiConfiguration
{
    public const string Section = "RouterAi";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://routerai.ru/api/v1";
    public string Model { get; set; } = "yandex/gpt-lite-5";
    public string ProviderCountry { get; set; } = "ru";
    public int Timeout { get; set; } = 120;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}
