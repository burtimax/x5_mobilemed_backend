using System.Text.Json.Serialization;

namespace ModuleLLM.Models.OpenRouter.LlmResponses;

/// <summary>Тело POST <c>/api/v1/responses</c> (OpenRouter Responses API).</summary>
public sealed class OpenRouterLlmResponsesRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public List<OpenRouterLlmResponsesInputItem> Input { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("max_output_tokens")]
    public int? MaxOutputTokens { get; set; }

    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }
}

public sealed class OpenRouterLlmResponsesInputItem
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public OpenRouterLlmResponsesInputContent Content { get; set; } = null!;
}
