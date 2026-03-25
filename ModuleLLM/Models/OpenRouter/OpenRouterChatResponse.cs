using System.Text.Json.Serialization;

namespace ModuleLLM.Models.OpenRouter;

public class OpenRouterChatResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenRouterChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public OpenRouterUsage? Usage { get; set; }
}

public class OpenRouterChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("logprobs")]
    public object? Logprobs { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("native_finish_reason")]
    public string? NativeFinishReason { get; set; }

    [JsonPropertyName("message")]
    public OpenRouterAssistantMessage? Message { get; set; }
}

public class OpenRouterAssistantMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("refusal")]
    public string? Refusal { get; set; }

    [JsonPropertyName("reasoning")]
    public string? Reasoning { get; set; }
}

public class OpenRouterUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("cost")]
    public double? Cost { get; set; }

    [JsonPropertyName("is_byok")]
    public bool? IsByok { get; set; }

    [JsonPropertyName("prompt_tokens_details")]
    public OpenRouterPromptTokensDetails? PromptTokensDetails { get; set; }

    [JsonPropertyName("cost_details")]
    public OpenRouterCostDetails? CostDetails { get; set; }

    [JsonPropertyName("completion_tokens_details")]
    public OpenRouterCompletionTokensDetails? CompletionTokensDetails { get; set; }
}

public class OpenRouterPromptTokensDetails
{
    [JsonPropertyName("cached_tokens")]
    public int? CachedTokens { get; set; }

    [JsonPropertyName("cache_write_tokens")]
    public int? CacheWriteTokens { get; set; }

    [JsonPropertyName("audio_tokens")]
    public int? AudioTokens { get; set; }

    [JsonPropertyName("video_tokens")]
    public int? VideoTokens { get; set; }
}

public class OpenRouterCostDetails
{
    [JsonPropertyName("upstream_inference_cost")]
    public double? UpstreamInferenceCost { get; set; }

    [JsonPropertyName("upstream_inference_prompt_cost")]
    public double? UpstreamInferencePromptCost { get; set; }

    [JsonPropertyName("upstream_inference_completions_cost")]
    public double? UpstreamInferenceCompletionsCost { get; set; }
}

public class OpenRouterCompletionTokensDetails
{
    [JsonPropertyName("reasoning_tokens")]
    public int? ReasoningTokens { get; set; }

    [JsonPropertyName("image_tokens")]
    public int? ImageTokens { get; set; }

    [JsonPropertyName("audio_tokens")]
    public int? AudioTokens { get; set; }
}
