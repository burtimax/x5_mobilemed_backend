using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModuleLLM.Models.OpenRouter.LlmResponses;

/// <summary>Содержимое сообщения: строка (например system) или массив частей (user).</summary>
[JsonConverter(typeof(OpenRouterLlmResponsesInputContentJsonConverter))]
public sealed class OpenRouterLlmResponsesInputContent
{
    public string? StringValue { get; }

    public IReadOnlyList<OpenRouterLlmInputContentPart>? Parts { get; }

    private OpenRouterLlmResponsesInputContent(string? stringValue, IReadOnlyList<OpenRouterLlmInputContentPart>? parts)
    {
        StringValue = stringValue;
        Parts = parts;
    }

    public static OpenRouterLlmResponsesInputContent FromString(string text) =>
        new(text, null);

    public static OpenRouterLlmResponsesInputContent FromParts(params OpenRouterLlmInputContentPart[] parts) =>
        new(null, parts);

    public static OpenRouterLlmResponsesInputContent FromParts(IReadOnlyList<OpenRouterLlmInputContentPart> parts) =>
        new(null, parts);

    private sealed class OpenRouterLlmResponsesInputContentJsonConverter : JsonConverter<OpenRouterLlmResponsesInputContent>
    {
        public override OpenRouterLlmResponsesInputContent Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => FromString(reader.GetString() ?? string.Empty),
                JsonTokenType.StartArray => FromParts(
                    JsonSerializer.Deserialize<List<OpenRouterLlmInputContentPart>>(ref reader, options) ?? []),
                JsonTokenType.Null => new OpenRouterLlmResponsesInputContent(null, null),
                _ => throw new JsonException($"Неожиданный JSON для content: {reader.TokenType}")
            };
        }

        public override void Write(Utf8JsonWriter writer, OpenRouterLlmResponsesInputContent value, JsonSerializerOptions options)
        {
            if (value.StringValue != null)
            {
                writer.WriteStringValue(value.StringValue);
                return;
            }

            if (value.Parts != null)
            {
                JsonSerializer.Serialize(writer, value.Parts, options);
                return;
            }

            writer.WriteNullValue();
        }
    }
}
