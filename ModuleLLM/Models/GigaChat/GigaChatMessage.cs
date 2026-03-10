using System.Text.Json.Serialization;

namespace ModuleLLM.Models.GigaChat;

/// <summary>
/// Сообщение для GigaChat API
/// </summary>
public class GigaChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
