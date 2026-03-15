using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// JSON-конвертеры для колонок ProductEntity.
/// </summary>
internal static class ProductEntityJsonConverters
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    public static ValueConverter<List<string>, string> ImagesConverter => new(
        v => SerializeImages(v),
        v => DeserializeImages(v));

    public static ValueConverter<List<ProductFeatureDto>, string> FeaturesConverter => new(
        v => SerializeFeatures(v),
        v => DeserializeFeatures(v));

    private static string SerializeImages(List<string> v) => JsonSerializer.Serialize(v, Options);
    private static List<string> DeserializeImages(string v) => JsonSerializer.Deserialize<List<string>>(v) ?? new List<string>();

    private static string SerializeFeatures(List<ProductFeatureDto> v) => JsonSerializer.Serialize(v, Options);
    private static List<ProductFeatureDto> DeserializeFeatures(string v) => JsonSerializer.Deserialize<List<ProductFeatureDto>>(v) ?? new List<ProductFeatureDto>();
}
