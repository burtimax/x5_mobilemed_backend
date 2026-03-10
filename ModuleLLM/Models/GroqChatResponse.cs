using System.Text.Json.Serialization;

namespace ModuleLLM.Models;

/// <summary>
/// Модель ответа от Groq API для chat completions
/// </summary>
public class GroqChatResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<GroqChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public GroqUsage? Usage { get; set; }

    [JsonPropertyName("usage_breakdown")]
    public object? UsageBreakdown { get; set; }

    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; set; }

    [JsonPropertyName("x_groq")]
    public GroqXMetadata? XGroq { get; set; }

    [JsonPropertyName("service_tier")]
    public string? ServiceTier { get; set; }
}

/// <summary>
/// Модель выбора из ответа Groq API
/// </summary>
public class GroqChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public GroqMessage? Message { get; set; }

    [JsonPropertyName("logprobs")]
    public object? Logprobs { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// Модель использования токенов
/// </summary>
public class GroqUsage
{
    [JsonPropertyName("queue_time")]
    public double? QueueTime { get; set; }

    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("prompt_time")]
    public double? PromptTime { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("completion_time")]
    public double? CompletionTime { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("total_time")]
    public double? TotalTime { get; set; }

    [JsonPropertyName("completion_tokens_details")]
    public GroqCompletionTokensDetails? CompletionTokensDetails { get; set; }
}

/// <summary>
/// Детали токенов завершения
/// </summary>
public class GroqCompletionTokensDetails
{
    [JsonPropertyName("reasoning_tokens")]
    public int? ReasoningTokens { get; set; }
}

/// <summary>
/// Метаданные Groq (x_groq)
/// </summary>
public class GroqXMetadata
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("seed")]
    public long? Seed { get; set; }
}

/// <summary>
/// Модель ошибки от Groq API
/// </summary>
public class GroqErrorResponse
{
    [JsonPropertyName("error")]
    public GroqError? Error { get; set; }
}

/// <summary>
/// Модель ошибки
/// </summary>
public class GroqError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

