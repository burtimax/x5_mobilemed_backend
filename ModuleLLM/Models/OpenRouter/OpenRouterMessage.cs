using System.Text.Json.Serialization;

namespace ModuleLLM.Models.OpenRouter;

public class OpenRouterMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
