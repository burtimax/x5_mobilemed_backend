using System.Text.Json.Serialization;

namespace Application.Services.BootstrapDatabase;

/// <summary>
/// DTO для десериализации JSON биомаркеров.
/// </summary>
internal record BiomarkerSeedDto(
    string Key,
    [property: JsonPropertyName("order")] int? Order,
    [property: JsonPropertyName("is_active")] bool? IsActive,
    string Name,
    string? Unit,
    string Description,
    [property: JsonPropertyName("descriptionUser")] string DescriptionUser,
    List<BiomarkerScaleSeedDto> Scales
);

internal record BiomarkerScaleSeedDto(
    [property: JsonPropertyName("genderInterval")] IntervalDto GenderInterval,
    [property: JsonPropertyName("weightInterval")] IntervalDto WeightInterval,
    [property: JsonPropertyName("ageInterval")] IntervalDto AgeInterval,
    [property: JsonPropertyName("valueIntervals")] IntervalDto ValueIntervals,
    [property: JsonPropertyName("relativeToAge")] bool? RelativeToAge,
    List<BiomarkerZoneSeedDto> Zones
);

internal record IntervalDto(double From, double To);

internal record BiomarkerZoneSeedDto(
    [property: JsonPropertyName("zone_key")] string ZoneKey,
    double? From,
    double? To,
    string? Rule,
    string Comment,
    [property: JsonPropertyName("commentUser")] string? CommentUser,
    [property: JsonPropertyName("from_to_alias")] string? FromToAlias,
    [property: JsonPropertyName("value_alias")] string? ValueAlias
);
