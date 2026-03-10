using System.Text.Json.Serialization;

namespace ModuleLLM.Models;

/// <summary>
/// Модель запроса к RouterAI API для chat completions
/// </summary>
public class RouterAIChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<GroqMessage> Messages { get; set; } = new();

    [JsonPropertyName("provider")]
    public RouterAIProvider? Provider { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    [JsonPropertyName("response_format")]
    public ResponseFormatType? ResponseFormat { get; set; }
}

/// <summary>
/// Провайдер для RouterAI (страна)
/// </summary>
public class RouterAIProvider
{
    [JsonPropertyName("country")]
    public string Country { get; set; } = "ru";
}
