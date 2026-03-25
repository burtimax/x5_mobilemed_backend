using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModuleLLM.Models.OpenRouter;

public class OpenRouterChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OpenRouterMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("response_format")]
    public OpenRouterResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// Полный объект <c>response_format</c> в виде JSON-строки (например
    /// <c>{"type":"json_schema","json_schema":{"name":"...","strict":true,"schema":{...}}}</c>).
    /// Если задан, подставляется в теле запроса и перекрывает <see cref="ResponseFormat"/>.
    /// </summary>
    [JsonIgnore]
    public string? ResponseFormatJson { get; set; }
}

public class OpenRouterResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("json_schema")]
    public OpenRouterJsonSchemaResponseFormat? JsonSchema { get; set; }
}

public class OpenRouterJsonSchemaResponseFormat
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("strict")]
    public bool? Strict { get; set; }

    [JsonPropertyName("schema")]
    public JsonElement? Schema { get; set; }
}
