using System.Text.Json.Serialization;

namespace ModuleLLM.Models.GigaChat;

/// <summary>
/// Запрос к GigaChat API для chat completions
/// </summary>
public class GigaChatChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<GigaChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("n")]
    public int N { get; set; } = 1;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 512;

    [JsonPropertyName("repetition_penalty")]
    public double RepetitionPenalty { get; set; } = 1;

    [JsonPropertyName("update_interval")]
    public double UpdateInterval { get; set; } = 0;

    [JsonPropertyName("response_format")]
    public GigaChatResponseFormat? ResponseFormat { get; set; }
}

/// <summary>
/// Формат ответа GigaChat (если поддерживается)
/// </summary>
public class GigaChatResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "json_object";
}
