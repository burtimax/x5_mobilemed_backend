using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Models.RppgScan;

/// <summary>
/// DTO для парсинга результата сканирования от Binah SDK.
/// SDK присылает только те поля, которые смог определить.
/// </summary>
public class BinahSdkResultDto
{
    [JsonPropertyName("takenAt")]
    public string? TakenAt { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("userInfo")]
    public BinahUserInfoDto? UserInfo { get; set; }

    [JsonPropertyName("metrics")]
    public JsonElement? Metrics { get; set; }

    [JsonPropertyName("sdkRaw")]
    public BinahSdkRawDto? SdkRaw { get; set; }
}

public class BinahUserInfoDto
{
    [JsonPropertyName("sex")]
    public string? Sex { get; set; }

    [JsonPropertyName("age")]
    public int? Age { get; set; }

    [JsonPropertyName("heightCm")]
    public int? HeightCm { get; set; }

    [JsonPropertyName("weightKg")]
    public decimal? WeightKg { get; set; }

    [JsonPropertyName("smokingStatus")]
    public string? SmokingStatus { get; set; }
}

public class BinahSdkRawDto
{
    [JsonPropertyName("results")]
    public JsonElement? Results { get; set; }
}
