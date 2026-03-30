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
        var orderedItems = scanItems
            .OrderBy(i =>
            {
                var b = biomarkers.FirstOrDefault(x =>
                    string.Equals(x.Key, i.Key, StringComparison.OrdinalIgnoreCase));
                return b?.Order ?? int.MaxValue;
            })
            .ThenBy(i => i.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var item in orderedItems)
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

            var zones = string.Equals(item.Key, "heartAge", StringComparison.OrdinalIgnoreCase)
                ? BuildHeartAgeZones(scale, age)
                : GetThreeClosestZones(scale, (double)item.Value);
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
    /// Динамические зоны для heartAge: зелёная (&lt; возраста), жёлтая (возраст — возраст+5), красная (&gt; возраст+5).
    /// </summary>
    private static List<ScanTranscriptItemZone> BuildHeartAgeZones(BiomarkerScaleEntity scale, int age)
    {
        var comments = scale.Zones
            .Where(z => z.ZoneKey != null)
            .ToDictionary(z => z.ZoneKey!, z => z.CommentUser ?? string.Empty);

        return
        [
            new ScanTranscriptItemZone
            {
                ZoneKey = "green",
                From = age - 20,
                To = age - 1e-6,
                CommentUser = comments.GetValueOrDefault("green", string.Empty),
                FromToAlias = $"< {age}",
                ValueAlias = null
            },
            new ScanTranscriptItemZone
            {
                ZoneKey = "yellow",
                From = age,
                To = age + 5 - 1e-6,
                CommentUser = comments.GetValueOrDefault("yellow", string.Empty),
                FromToAlias = $"{age} - {age + 5}",
                ValueAlias = null
            },
            new ScanTranscriptItemZone
            {
                ZoneKey = "red",
                From = age + 5,
                To = age + 20,
                CommentUser = comments.GetValueOrDefault("red", string.Empty),
                FromToAlias = $"{age + 5} +",
                ValueAlias = null
            }
        ];
    }

    /// <summary>
    /// Возвращает 3 зоны: зону, в которой находится показатель, и две соседние.
    /// Если показатель в 1-й зоне — берём зоны 1,2,3. Если в последней — берём 3 предыдущие.
    /// </summary>
    private static List<ScanTranscriptItemZone> GetThreeClosestZones(BiomarkerScaleEntity scale, double value)
    {
        var zones = scale.Zones
            .Where(z => z.ZoneKey != null && z.ValueFrom.HasValue)
            .OrderBy(z => z.ValueFrom)
            .ToList();
        if (zones.Count == 0)
            return [];
        if (zones.Count <= 3)
            return zones.Select(MapToTranscriptZone).ToList();

        var withDistance = zones.Select(z =>
        {
            var from = (double)(z.ValueFrom ?? 0m);
            var to = z.ValueTo.HasValue ? (double)z.ValueTo.Value : double.MaxValue;
            return (Zone: z, Distance: GetDistanceToInterval(value, from, to));
        }).ToList();

        var centerIndex = withDistance
            .Select((x, i) => (Index: i, x.Distance))
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Index)
            .First()
            .Index;

        int startIndex = centerIndex < 2 ? 0
            : centerIndex > zones.Count - 3 ? zones.Count - 3
            : centerIndex - 1;

        return withDistance
            .Skip(startIndex)
            .Take(3)
            .Select(x => MapToTranscriptZone(x.Zone))
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

    private static ScanTranscriptItemZone MapToTranscriptZone(BiomarkerZoneEntity zone)
    {
        return new ScanTranscriptItemZone
        {
            ZoneKey = zone.ZoneKey,
            From = (double)(zone.ValueFrom ?? 0m),
            To = (double)(zone.ValueTo ?? 0m),
            CommentUser = zone.CommentUser ?? string.Empty,
            FromToAlias = zone.FromToAlias ?? string.Empty,
            ValueAlias = zone.ValueAlias
        };
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
                Color = z.ZoneKey,
                FromToAlias = z.FromToAlias,
                ValueAlias = z.ValueAlias
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
