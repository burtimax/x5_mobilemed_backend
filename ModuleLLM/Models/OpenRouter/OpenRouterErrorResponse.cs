using System.Text.Json.Serialization;

namespace ModuleLLM.Models.OpenRouter;

public class OpenRouterErrorResponse
{
    [JsonPropertyName("error")]
    public OpenRouterError? Error { get; set; }
}

public class OpenRouterError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("code")]
    public object? Code { get; set; }
}
