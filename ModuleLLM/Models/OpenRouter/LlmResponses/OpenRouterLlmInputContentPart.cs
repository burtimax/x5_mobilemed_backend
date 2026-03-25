using System.Text.Json.Serialization;

namespace ModuleLLM.Models.OpenRouter.LlmResponses;

/// <summary>
/// Элемент массива <c>content</c> у сообщения user: либо <c>input_text</c>, либо <c>file</c> (как в JSON OpenRouter).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(OpenRouterLlmInputTextPart), "input_text")]
[JsonDerivedType(typeof(OpenRouterLlmInputFilePart), "file")]
public abstract class OpenRouterLlmInputContentPart
{
}

/// <summary><c>{"type":"input_text","text":"..."}</c></summary>
public sealed class OpenRouterLlmInputTextPart : OpenRouterLlmInputContentPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary><c>{"type":"file","file":"..."}</c></summary>
public sealed class OpenRouterLlmInputFilePart : OpenRouterLlmInputContentPart
{
    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;
}
