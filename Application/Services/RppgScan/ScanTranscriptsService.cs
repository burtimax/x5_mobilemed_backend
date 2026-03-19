using Application.Models.RppgScan;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.RppgScan;

/// <summary>
/// Сервис формирования расшифровки результатов сканирования (Transcripts).
/// Сопоставляет показатели с зонами биомаркеров с учётом пола, возраста и веса пользователя.
/// </summary>
public class ScanTranscriptsService : IScanTranscriptsService
{
    private readonly AppDbContext _db;

    public ScanTranscriptsService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<List<ScanTranscriptItem>> BuildTranscriptsAsync(
        IReadOnlyList<UserRppgScanResultItemEntity> scanItems,
        int? userAge,
        Gender? userGender,
        decimal? userWeight,
        CancellationToken ct = default)
    {
        if (scanItems.Count == 0)
            return [];

        var age = userAge ?? 30; // fallback для heartAge и подбора шкалы
        var genderInt = userGender.HasValue ? (int)userGender.Value : 0;
        var weight = userWeight ?? 70m;

        var biomarkerKeys = scanItems.Select(i => i.Key).Distinct().ToList();
        var biomarkers = await _db.Biomarkers
            .AsNoTracking()
            .Where(b => biomarkerKeys.Contains(b.Key))
            .Include(b => b.Scales)
                .ThenInclude(s => s.Zones)
            .ToListAsync(ct);

        var result = new List<ScanTranscriptItem>();

        foreach (var item in scanItems)
        {
            var biomarker = biomarkers.FirstOrDefault(b =>
                string.Equals(b.Key, item.Key, StringComparison.OrdinalIgnoreCase));

            if (biomarker == null)
            {
                result.Add(CreateUnknownTranscriptItem(item));
                continue;
            }

            var scale = SelectMatchingScale(biomarker, genderInt, age, weight);
            if (scale == null)
            {
                result.Add(CreateTranscriptItemWithoutZones(biomarker, item));
                continue;
            }

            var zones = GetThreeClosestZones(scale, (double)item.Value, age);
            var matchingZone = GetMatchingZone(zones, (double)item.Value);
            result.Add(new ScanTranscriptItem
            {
                Key = item.Key,
                Value = item.Value,
                Name = biomarker.Name,
                Unit = biomarker.Unit ?? item.Unit ?? string.Empty,
                DescriptionUser = biomarker.DescriptionUser,
                CommentUser = matchingZone?.CommentUser ?? string.Empty,
                Color = matchingZone?.ZoneKey ?? string.Empty,
                ConfidenceLevel = item.ConfidenceLevel ?? 0,
                Zones = zones,
                ScaleMetadata = BuildScaleMetadata(zones, item.Value)
            });
        }

        return result;
    }

    /// <summary>
    /// Выбирает шкалу по полу, возрасту и весу пользователя.
    /// </summary>
    private static BiomarkerScaleEntity? SelectMatchingScale(
        BiomarkerEntity biomarker,
        int genderInt,
        int age,
        decimal weight)
    {
        return biomarker.Scales.FirstOrDefault(s =>
            genderInt >= s.GenderFrom && genderInt <= s.GenderTo &&
            age >= s.AgeFrom && age <= s.AgeTo &&
            weight >= s.WeightFrom && weight <= s.WeightTo);
    }

    /// <summary>
    /// Возвращает 3 зоны: зону, в которой находится показатель, и две соседние.
    /// Если показатель в 1-й зоне — берём зоны 1,2,3. Если в последней — берём 3 предыдущие.
    /// </summary>
    private static List<ScanTranscriptItemZone> GetThreeClosestZones(
        BiomarkerScaleEntity scale,
        double value,
        int userAge)
    {
        var zones = scale.Zones.Where(z => z.ZoneKey != null)
            .OrderBy(z => z.ValueFrom)
            .ToList();
        if (zones.Count == 0)
            return [];
        if (zones.Count <= 3)
            return zones.Select(z => MapToTranscriptZone(z, userAge)).ToList();

        var withDistance = zones.Select(z =>
        {
            double distance;
            if (!string.IsNullOrEmpty(z.Rule))
            {
                distance = GetDistanceForRuleZone(z, value, userAge);
            }
            else
            {
                var from = (double)(z.ValueFrom ?? 0m);
                var to = z.ValueTo.HasValue ? (double)z.ValueTo.Value : double.MaxValue;
                distance = GetDistanceToInterval(value, from, to);
            }
            return (Zone: z, Distance: distance);
        }).ToList();

        // Индекс зоны, в которой значение (distance=0) или ближайшей к нему
        var centerIndex = withDistance
            .Select((x, i) => (Index: i, x.Distance))
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Index)
            .First()
            .Index;

        // Центральная зона + две соседние; на краях — две с одной стороны
        int startIndex;
        if (centerIndex < 2)
            startIndex = 0;
        else if (centerIndex > zones.Count - 3)
            startIndex = zones.Count - 3;
        else
            startIndex = centerIndex - 1;

        return withDistance
            .Skip(startIndex)
            .Take(3)
            .Select(x => MapToTranscriptZone(x.Zone, userAge))
            .ToList();
    }

    /// <summary>
    /// Расстояние от value до интервала [from, to].
    /// Внутри интервала = 0.
    /// </summary>
    private static double GetDistanceToInterval(double value, double from, double to)
    {
        if (value >= from && value <= to) return 0;
        if (value < from) return from - value;
        return value - to;
    }

    /// <summary>
    /// Расстояние для зоны с Rule (например, heartAge).
    /// Если зона подходит под правило — 0, иначе — расстояние до границы.
    /// </summary>
    private static double GetDistanceForRuleZone(BiomarkerZoneEntity zone, double value, int age)
    {
        var rule = zone.Rule ?? string.Empty;

        // heartAge: "value <= age" | "value >= age + 1 && value <= age + 5" | "value > age + 5"
        if (rule.Contains("value <= age", StringComparison.Ordinal))
        {
            if (value <= age) return 0;
            return value - age;
        }
        if (rule.Contains("value >= age + 1") && rule.Contains("value <= age + 5"))
        {
            var low = age + 1;
            var high = age + 5;
            if (value >= low && value <= high) return 0;
            if (value < low) return low - value;
            return value - high;
        }
        if (rule.Contains("value > age + 5"))
        {
            var boundary = age + 5;
            if (value > boundary) return 0;
            return boundary - value;
        }

        return double.MaxValue; // неизвестное правило — считаем далёкой
    }

    private static ScanTranscriptItemZone MapToTranscriptZone(BiomarkerZoneEntity zone, int userAge)
    {
        double from, to;
        if (!string.IsNullOrEmpty(zone.Rule))
        {
            (from, to) = GetFromToForRule(zone.Rule, userAge);
        }
        else
        {
            from = (double)(zone.ValueFrom ?? 0m);
            to = (double)(zone.ValueTo ?? 0m);
        }
        return new ScanTranscriptItemZone
        {
            ZoneKey = zone.ZoneKey,
            From = from,
            To = to,
            CommentUser = zone.CommentUser ?? string.Empty
        };
    }

    private static (double From, double To) GetFromToForRule(string rule, int age)
    {
        if (rule.Contains("value <= age"))
            return (0, age);
        if (rule.Contains("value >= age + 1") && rule.Contains("value <= age + 5"))
            return (age + 1, age + 5);
        if (rule.Contains("value > age + 5"))
            return (age + 5, 120);
        return (0, 0);
    }

    /// <summary>
    /// Возвращает зону, в которой находится значение, или среднюю зону (ближайшую), если значение вне интервалов.
    /// </summary>
    private static ScanTranscriptItemZone? GetMatchingZone(List<ScanTranscriptItemZone> zones, double value)
    {
        if (zones == null || zones.Count == 0)
            return null;

        var matching = zones.FirstOrDefault(z => value >= z.From && value <= z.To);
        if (matching != null)
            return matching;

        var centerIndex = zones.Count / 2;
        return zones[centerIndex];
    }

    /// <summary>
    /// Показывает в процентах насколько показатель близко к зелёной зоне.
    /// В зелёной зоне — 100%. Ниже зелёной — % от минимума до середины зелёной зоны. Выше — % от середины до максимума.
    /// </summary>
    private static int ComputeBiomarkerPercentage(
        List<ScanTranscriptItemZone> zones,
        double value,
        double scaleMin,
        double scaleMax)
    {
        var green = zones.FirstOrDefault(z =>
            string.Equals(z.ZoneKey, "green", StringComparison.OrdinalIgnoreCase));
        if (green == null)
            return 0;

        var greenCenter = (green.From + green.To) / 2;

        if (value <= greenCenter)
        {
            var distToGreenCenter = greenCenter - scaleMin;
            if (distToGreenCenter <= 0) return 0;
            return (int)Math.Round(Math.Clamp((value - scaleMin) / distToGreenCenter * 100, 0, 100));
        }

        // value > green.To
        var distFromGreenCenter = scaleMax - greenCenter;
        if (distFromGreenCenter <= 0) return 0;
        return (int)Math.Round(Math.Clamp((scaleMax - value) / distFromGreenCenter * 100, 0, 100));
    }

    /// <summary>
    /// Формирует данные для отрисовки шкалы здоровья.
    /// Берём 3 зоны Zones, Min(From) — начало шкалы, Max(To) — конец шкалы.
    /// По значениям зон вычисляем PercentFrom/PercentTo и формируем Items.
    /// </summary>
    private static ScanResultScaleData? BuildScaleMetadata(
        List<ScanTranscriptItemZone> zones,
        decimal value)
    {
        if (zones == null || zones.Count == 0)
            return null;

        var scaleMin = zones.Min(z => z.From);
        var scaleMax = zones.Max(z => z.To);
        var range = scaleMax - scaleMin;

        if (range <= 0)
            return null;

        var valueDouble = (double)value;
        var valuePercent = (int)Math.Round(Math.Clamp(
            (valueDouble - scaleMin) / range * 100, 0, 100));

        var items = zones
            .OrderBy(z => z.From)
            .Select(z => new ScanResultScaleDataItem
            {
                From = z.From,
                To = z.To,
                PercentFrom = (int)Math.Round(Math.Clamp((z.From - scaleMin) / range * 100, 0, 100)),
                PercentTo = (int)Math.Round(Math.Clamp((z.To - scaleMin) / range * 100, 0, 100)),
                Color = z.ZoneKey
            })
            .ToList();

        var biomarkerPercentage = ComputeBiomarkerPercentage(zones, valueDouble, scaleMin, scaleMax);

        return new ScanResultScaleData
        {
            ValuePercentLabel = valuePercent,
            BiomarkerScore = biomarkerPercentage,
            Items = items
        };
    }

    private static ScanTranscriptItem CreateUnknownTranscriptItem(UserRppgScanResultItemEntity item)
    {
        return new ScanTranscriptItem
        {
            Key = item.Key,
            Value = item.Value,
            Name = item.Key,
            Unit = item.Unit ?? string.Empty,
            DescriptionUser = string.Empty,
            CommentUser = string.Empty,
            Color = string.Empty,
            ConfidenceLevel = item.ConfidenceLevel ?? 0,
            Zones = []
        };
    }

    private static ScanTranscriptItem CreateTranscriptItemWithoutZones(
        BiomarkerEntity biomarker,
        UserRppgScanResultItemEntity item)
    {
        return new ScanTranscriptItem
        {
            Key = item.Key,
            Value = item.Value,
            Name = biomarker.Name,
            Unit = biomarker.Unit ?? item.Unit ?? string.Empty,
            DescriptionUser = biomarker.DescriptionUser,
            CommentUser = string.Empty,
            Color = string.Empty,
            ConfidenceLevel = item.ConfidenceLevel ?? 0,
            Zones = []
        };
    }
}
