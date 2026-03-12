using System.Collections.Generic;
using System.Text.Json;
using Application.Models.RppgScan;
using Infrastructure.Db.App.Entities;
using Shared.Extensions;

namespace Application.Services.RppgScan;

/// <summary>
/// Парсер результата Binah SDK в сущности БД.
/// Извлекает все показатели из metrics и sdkRaw.results.
/// </summary>
public static class BinahSdkResultParser
{
    /// <summary>
    /// Парсит JSON результат SDK и создает сущности сканирования.
    /// </summary>
    public static (UserRppgScanEntity Scan, List<UserRppgScanResultItemEntity> Items) Parse(
        BinahSdkResultDto dto,
        JsonElement rawResult,
        Guid userId)
    {
        var takenAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrEmpty(dto.TakenAt) && DateTimeOffset.TryParse(dto.TakenAt, out var parsed))
            takenAt = parsed;

        var scan = new UserRppgScanEntity
        {
            UserId = userId,
            SdkResult = rawResult.ToJson(),
        };

        var items = new List<UserRppgScanResultItemEntity>();
        var processedKeys = new HashSet<string>();

        // Собираем данные из metrics (value, unit)
        var metricsData = new Dictionary<string, (decimal? Value, string? Unit)>();
        if (dto.Metrics?.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in dto.Metrics.Value.EnumerateObject())
            {
                foreach (var (key, value, unit) in ExtractMetricValues(prop.Name, prop.Value))
                {
                    if (!metricsData.ContainsKey(key))
                        metricsData[key] = (value, unit);
                }
            }
        }

        // Собираем данные из sdkRaw.results (value, confidenceLevel)
        var sdkRawData = new Dictionary<string, (decimal? Value, int? ConfidenceLevel)>();
        if (dto.SdkRaw?.Results?.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in dto.SdkRaw.Results.Value.EnumerateObject())
            {
                foreach (var (key, value, confidence) in ExtractSdkRawValues(prop.Name, prop.Value))
                {
                    if (!sdkRawData.ContainsKey(key))
                        sdkRawData[key] = (value, confidence);
                }
            }
        }

        // Объединяем все уникальные ключи
        var allKeys = new HashSet<string>();
        foreach (var k in metricsData.Keys) allKeys.Add(k);
        foreach (var k in sdkRawData.Keys) allKeys.Add(k);

        foreach (var key in allKeys)
        {
            if (processedKeys.Contains(key)) continue;
            if (key.Length > 30) continue; // UserRppgScanResultItemEntity.Key has MaxLength(30)

            decimal? value = null;
            int? confidenceLevel = null;
            string? unit = null;

            if (sdkRawData.TryGetValue(key, out var sdkData))
            {
                value = sdkData.Value;
                confidenceLevel = sdkData.ConfidenceLevel;
            }
            if (metricsData.TryGetValue(key, out var metricsDataItem))
            {
                value ??= metricsDataItem.Value;
                unit ??= metricsDataItem.Unit;
            }

            // Пропускаем если нет значения
            if (value == null) continue;

            items.Add(new UserRppgScanResultItemEntity
            {
                Key = key,
                Value = value.Value,
                ConfidenceLevel = confidenceLevel,
                Unit = unit?.Length > 30 ? unit[..30] : unit
            });
            processedKeys.Add(key);
        }

        return (scan, items);
    }

    /// <summary>
    /// Извлекает значения из metrics. Для bloodPressure возвращает два элемента: bloodPressureSystolic и bloodPressureDiastolic.
    /// </summary>
    private static IEnumerable<(string Key, decimal? Value, string? Unit)> ExtractMetricValues(string key, JsonElement element)
    {
        string? unit = null;
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("unit", out var unitProp))
            unit = unitProp.GetString();

        if (key == "bloodPressure" && element.ValueKind == JsonValueKind.Object)
        {
            var sys = element.TryGetProperty("systolic", out var sysProp) ? GetDecimalFromElement(sysProp) : null;
            var dia = element.TryGetProperty("diastolic", out var diaProp) ? GetDecimalFromElement(diaProp) : null;
            if (sys.HasValue)
                yield return ("bloodPressureSystolic", sys, unit);
            if (dia.HasValue)
                yield return ("bloodPressureDiastolic", dia, unit);
        }
        else
        {
            var value = element.ValueKind == JsonValueKind.Object && element.TryGetProperty("value", out var valProp)
                ? GetDecimalFromElement(valProp)
                : null;
            yield return (key, value, unit);
        }
    }

    /// <summary>
    /// Извлекает значения из sdkRaw.results. Для bloodPressure возвращает два элемента.
    /// </summary>
    private static IEnumerable<(string Key, decimal? Value, int? ConfidenceLevel)> ExtractSdkRawValues(string key, JsonElement element)
    {
        int? confidenceLevel = null;
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("confidenceLevel", out var confProp))
            confidenceLevel = confProp.ValueKind == JsonValueKind.Number ? confProp.GetInt32() : null;

        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty("value", out var valueProp))
            yield break;

        if (key == "bloodPressure" && valueProp.ValueKind == JsonValueKind.Object)
        {
            var sys = valueProp.TryGetProperty("systolic", out var sysProp) ? GetDecimalFromElement(sysProp) : null;
            var dia = valueProp.TryGetProperty("diastolic", out var diaProp) ? GetDecimalFromElement(diaProp) : null;
            if (sys.HasValue)
                yield return ("bloodPressureSystolic", sys, confidenceLevel);
            if (dia.HasValue)
                yield return ("bloodPressureDiastolic", dia, confidenceLevel);
        }
        else
        {
            var value = GetDecimalFromElement(valueProp);
            yield return (key, value, confidenceLevel);
        }
    }

    private static decimal? GetDecimalFromElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetDecimal(out var d) => d,
            JsonValueKind.String when decimal.TryParse(element.GetString(), out var d) => d,
            _ => null
        };
    }
}
