using System.Text.Json.Serialization;

namespace ModuleLLM.Models.GigaChat;

/// <summary>
/// Ответ OAuth API GigaChat на запрос access token
/// </summary>
public class GigaChatTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }
}
