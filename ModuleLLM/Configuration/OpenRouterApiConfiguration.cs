using ModuleLLM.Models.OpenRouter;

namespace ModuleLLM.Configuration;

public class OpenRouterApiConfiguration
{
    public const string Section = "OpenRouter";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";

    /// <summary>Модель по умолчанию для chat completions (например google/gemini-2.5-flash).</summary>
    public string Model { get; set; } = "google/gemini-2.5-flash";

    public int Timeout { get; set; } = 120;

    public int MaxRetryAttempts { get; set; } = 3;

    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Секция <c>provider</c> для <c>/chat/completions</c>. Подставляется в тело запроса,
    /// если в <see cref="OpenRouterChatRequest.Provider"/> не задано значение.
    /// </summary>
    public OpenRouterProviderSettings? Provider { get; set; }
}
