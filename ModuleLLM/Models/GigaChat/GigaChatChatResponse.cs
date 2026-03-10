using System.Text.Json.Serialization;

namespace ModuleLLM.Models.GigaChat;

/// <summary>
/// Ответ GigaChat API на chat completions
/// </summary>
public class GigaChatChatResponse
{
    [JsonPropertyName("choices")]
    public List<GigaChatChoice> Choices { get; set; } = new();

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("usage")]
    public GigaChatUsage? Usage { get; set; }
}

/// <summary>
/// Выбор из ответа GigaChat API
/// </summary>
public class GigaChatChoice
{
    [JsonPropertyName("message")]
    public GigaChatMessage? Message { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// Использование токенов GigaChat
/// </summary>
public class GigaChatUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("precached_prompt_tokens")]
    public int PrecachedPromptTokens { get; set; }
}
