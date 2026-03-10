using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Shared.Extensions;

public static class ObjectExtensions
{
    public static string ToJson(this object obj)
    {
        string json = JsonSerializer.Serialize(obj, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        return Regex.Unescape(json);
    }
    
    public static T? FromJson<T>(this string jsonStr)
    {
        T? obj = JsonSerializer.Deserialize<T>(jsonStr, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        return obj;
    }
}