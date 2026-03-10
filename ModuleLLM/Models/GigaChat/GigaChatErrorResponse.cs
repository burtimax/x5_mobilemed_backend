using System.Text.Json.Serialization;

namespace ModuleLLM.Models.GigaChat;

/// <summary>
/// Ответ об ошибке от GigaChat API
/// </summary>
public class GigaChatErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
