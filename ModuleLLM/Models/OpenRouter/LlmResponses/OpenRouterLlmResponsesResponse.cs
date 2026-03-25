using System.Text.Json.Serialization;

namespace ModuleLLM.Models.OpenRouter.LlmResponses;

/// <summary>Ответ POST <c>/api/v1/responses</c>.</summary>
public sealed class OpenRouterLlmResponsesResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("completed_at")]
    public long? CompletedAt { get; set; }

    [JsonPropertyName("output")]
    public List<OpenRouterLlmResponseOutputItem>? Output { get; set; }

    [JsonPropertyName("error")]
    public OpenRouterLlmResponsesNestedError? Error { get; set; }

    [JsonPropertyName("incomplete_details")]
    public object? IncompleteDetails { get; set; }

    [JsonPropertyName("usage")]
    public OpenRouterLlmResponsesUsage? Usage { get; set; }

    /// <summary>Текст первого блока <c>output_text</c> из сообщения ассистента.</summary>
    public string? GetFirstAssistantOutputText()
    {
        if (Output == null)
            return null;

        foreach (var item in Output)
        {
            if (item.Type is { Length: > 0 } kind
                && !string.Equals(kind, "message", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.Equals(item.Role, "assistant", StringComparison.OrdinalIgnoreCase))
                continue;
            if (item.Content == null)
                continue;
            foreach (var part in item.Content)
            {
                if (!string.Equals(part.Type, "output_text", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrWhiteSpace(part.Text))
                    return part.Text;
            }
        }

        return null;
    }
}

/// <summary>Элемент массива <c>output</c> (без JsonPolymorphic — у OpenRouter нет гарантии формата дискриминатора для STJ).</summary>
public sealed class OpenRouterLlmResponseOutputItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Например <c>message</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public List<OpenRouterLlmOutputContentItem>? Content { get; set; }
}

/// <summary>Элемент массива <c>content</c> у сообщения ассистента (<c>output_text</c> и др.).</summary>
public sealed class OpenRouterLlmOutputContentItem
{
    /// <summary>Например <c>output_text</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("annotations")]
    public List<object>? Annotations { get; set; }

    [JsonPropertyName("logprobs")]
    public List<object>? Logprobs { get; set; }
}

public sealed class OpenRouterLlmResponsesNestedError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("code")]
    public object? Code { get; set; }
}

public sealed class OpenRouterLlmResponsesUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("cost")]
    public double? Cost { get; set; }

    [JsonPropertyName("is_byok")]
    public bool? IsByok { get; set; }

    [JsonPropertyName("input_tokens_details")]
    public OpenRouterLlmResponsesInputTokensDetails? InputTokensDetails { get; set; }

    [JsonPropertyName("output_tokens_details")]
    public OpenRouterLlmResponsesOutputTokensDetails? OutputTokensDetails { get; set; }

    [JsonPropertyName("cost_details")]
    public OpenRouterLlmResponsesCostDetails? CostDetails { get; set; }
}

public sealed class OpenRouterLlmResponsesInputTokensDetails
{
    [JsonPropertyName("cached_tokens")]
    public int? CachedTokens { get; set; }
}

public sealed class OpenRouterLlmResponsesOutputTokensDetails
{
    [JsonPropertyName("reasoning_tokens")]
    public int? ReasoningTokens { get; set; }
}

public sealed class OpenRouterLlmResponsesCostDetails
{
    [JsonPropertyName("upstream_inference_cost")]
    public double? UpstreamInferenceCost { get; set; }

    [JsonPropertyName("upstream_inference_input_cost")]
    public double? UpstreamInferenceInputCost { get; set; }

    [JsonPropertyName("upstream_inference_output_cost")]
    public double? UpstreamInferenceOutputCost { get; set; }
}
