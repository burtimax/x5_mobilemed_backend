using System.Text.Json.Serialization;

namespace ModuleLLM.Models;

/// <summary>
/// Модель запроса к Groq API для chat completions
/// </summary>
public class GroqChatRequest
{
    [JsonPropertyName("messages")]
    public List<GroqMessage> Messages { get; set; } = new();

    [JsonPropertyName("model")]
    public string Model { get; set; } = "openai/gpt-oss-120b";

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 1;

    [JsonPropertyName("max_completion_tokens")]
    public int MaxCompletionTokens { get; set; } = 8192;

    [JsonPropertyName("top_p")]
    public double TopP { get; set; } = 1;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    [JsonPropertyName("reasoning_effort")]
    public string? ReasoningEffort { get; set; } = "high"; // low, medium, high

    [JsonPropertyName("response_format")]
    public ResponseFormatType? ResponseFormat { get; set; }

    [JsonPropertyName("stop")]
    public object? Stop { get; set; } = null;
}

/// <summary>
/// Формат ответа от Groq API
/// </summary>
public class ResponseFormatType
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Модель сообщения для Groq API
/// </summary>
public class GroqMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("reasoning")]
    public string? Reasoning { get; set; }
}

