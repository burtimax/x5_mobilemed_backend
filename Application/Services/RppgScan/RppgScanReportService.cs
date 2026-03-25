using System.Globalization;
using System.Text;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.RppgScan;

/// <inheritdoc cref="IRppgScanReportService" />
public sealed class RppgScanReportService : IRppgScanReportService
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    private readonly AppDbContext _db;

    public RppgScanReportService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<string?> GetReportTextAsync(Guid scanId, CancellationToken cancellationToken = default)
    {
        var scan = await _db.UserRppgScans
            .AsNoTracking()
            .Include(s => s.User!)
                .ThenInclude(u => u.Profile)
            .Include(s => s.ResultItems)
            .FirstOrDefaultAsync(s => s.Id == scanId, cancellationToken);

        if (scan == null)
            return null;

        var profile = scan.User?.Profile;
        var age = profile?.Age ?? 30;
        var genderInt = profile?.Gender.HasValue == true ? (int)profile.Gender!.Value : 0;
        var weight = profile?.Weight ?? 70m;

        var biomarkerKeys = scan.ResultItems.Select(i => i.Key).Distinct().ToList();
        var biomarkers = biomarkerKeys.Count == 0
            ? []
            : await _db.Biomarkers
                .AsNoTracking()
                .Where(b => biomarkerKeys.Contains(b.Key))
                .Include(b => b.Scales)
                .ThenInclude(s => s.Zones)
                .ToListAsync(cancellationToken);

        return BuildReport(scan, profile, age, genderInt, weight, biomarkers);
    }

    private static string BuildReport(
        UserRppgScanEntity scan,
        UserProfileEntity? profile,
        int age,
        int genderInt,
        decimal weight,
        List<BiomarkerEntity> biomarkers)
    {
        var sb = new StringBuilder(4096);
        sb.AppendLine("ОТЧЁТ ПО СКАНУ RPPG");
        sb.AppendLine(new string('=', 48));
        sb.AppendLine($"Идентификатор скана: {scan.Id}");
        sb.AppendLine($"Создан: {scan.CreatedAt.ToString("g", Ru)}");
        sb.AppendLine($"Статус: {FormatScanStatus(scan.Status)}");
        sb.AppendLine();
        sb.AppendLine("Профиль пользователя (для подбора шкалы):");
        if (profile == null)
        {
            sb.AppendLine("  Профиль не заполнен — используются значения по умолчанию: возраст 30 лет, пол мужской, вес 70 кг.");
        }
        else
        {
            sb.AppendLine($"  Возраст: {(profile.Age?.ToString(Ru) ?? "не указан (30 по умолчанию)")} лет");
            sb.AppendLine($"  Пол: {FormatGender(profile.Gender)}");
            sb.AppendLine($"  Вес: {(profile.Weight?.ToString(Ru) ?? "не указан (70 по умолчанию)")} кг");
        }

        sb.AppendLine();
        sb.AppendLine("ПОКАЗАТЕЛИ");
        sb.AppendLine(new string('-', 48));

        if (scan.ResultItems.Count == 0)
        {
            sb.AppendLine("Нет сохранённых показателей (ResultItems пуст).");
            return sb.ToString();
        }

        var ordered = scan.ResultItems.OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase).ToList();
        var index = 0;
        foreach (var item in ordered)
        {
            index++;
            sb.AppendLine();
            sb.AppendLine($"{index}. {FormatIndicatorBlock(item, biomarkers, age, genderInt, weight)}");
        }

        return sb.ToString();
    }

    private static string FormatIndicatorBlock(
        UserRppgScanResultItemEntity item,
        List<BiomarkerEntity> biomarkers,
        int age,
        int genderInt,
        decimal weight)
    {
        var sb = new StringBuilder();
        var biomarker = biomarkers.FirstOrDefault(b =>
            string.Equals(b.Key, item.Key, StringComparison.OrdinalIgnoreCase));

        var name = biomarker?.Name ?? item.Key;
        sb.AppendLine($"{name} (ключ: {item.Key})");
        sb.AppendLine($"   Значение: {item.Value.ToString(Ru)}{(string.IsNullOrEmpty(item.Unit) ? "" : " " + item.Unit)}");
        if (item.ConfidenceLevel.HasValue)
            sb.AppendLine($"   Уровень уверенности: {item.ConfidenceLevel}%");

        if (biomarker == null)
        {
            sb.AppendLine("   В справочнике биомаркеров запись не найдена — интерпретация по зонам недоступна.");
            return sb.ToString().TrimEnd();
        }

        if (!string.IsNullOrWhiteSpace(biomarker.DescriptionUser))
            sb.AppendLine($"   Описание: {biomarker.DescriptionUser.Trim()}");

        var scale = SelectMatchingScale(biomarker, genderInt, age, weight);
        if (scale == null)
        {
            sb.AppendLine("   Подходящая шкала (пол / возраст / вес) не найдена — зоны не применены.");
            return sb.ToString().TrimEnd();
        }

        var isHeartAge = string.Equals(item.Key, "heartAge", StringComparison.OrdinalIgnoreCase);
        var valueD = (double)item.Value;
        var zones = isHeartAge
            ? BuildHeartAgeZones(scale, age)
            : BuildAllNumericZones(scale);

        if (zones.Count == 0)
        {
            sb.AppendLine("   У шкалы нет числовых зон — интерпретация недоступна.");
            return sb.ToString().TrimEnd();
        }

        var matched = FindMatchingZone(zones, valueD);
        sb.AppendLine($"   Оценка по зоне: {DescribeZoneStatus(matched, zones, valueD)}");
        if (!string.IsNullOrWhiteSpace(matched.CommentUser))
            sb.AppendLine($"   Комментарий к зоне: {matched.CommentUser.Trim()}");

        sb.AppendLine("   Шкала (все зоны):");
        foreach (var z in zones.OrderBy(z => z.From))
        {
            var range = string.IsNullOrWhiteSpace(z.FromToAlias)
                ? $"{z.From.ToString(Ru)} … {FormatUpperBound(z.To)}"
                : z.FromToAlias;
            var mark = valueD >= z.From && valueD <= z.To ? "  ← текущее значение" : "";
            sb.AppendLine($"     • {FormatZoneKeyRu(z.ZoneKey)}: {range}{mark}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatUpperBound(double to) =>
        to >= double.MaxValue / 2 ? "∞" : to.ToString(Ru);

    private static string DescribeZoneStatus(ReportZone matched, List<ReportZone> allZones, double value)
    {
        var key = matched.ZoneKey ?? "";
        var green = allZones.FirstOrDefault(z =>
            string.Equals(z.ZoneKey, "green", StringComparison.OrdinalIgnoreCase));

        if (string.Equals(key, "green", StringComparison.OrdinalIgnoreCase))
            return "в норме (зелёная зона).";

        if (string.Equals(key, "yellow", StringComparison.OrdinalIgnoreCase))
            return "пограничное значение (жёлтая зона).";

        if (string.Equals(key, "red", StringComparison.OrdinalIgnoreCase) && green != null)
        {
            if (value < green.From)
                return "вне нормы (красная зона): ниже нормативного (зелёного) диапазона.";
            if (value > green.To)
                return "вне нормы (красная зона): выше нормативного (зелёного) диапазона.";
            return "вне нормы (красная зона).";
        }

        if (string.Equals(key, "red", StringComparison.OrdinalIgnoreCase))
            return "вне нормы (красная зона).";

        return $"зона «{key}» (уточните по комментарию к зоне).";
    }

    private static string FormatZoneKeyRu(string? zoneKey) => zoneKey?.ToLowerInvariant() switch
    {
        "green" => "Норма (зелёная)",
        "yellow" => "Погранично (жёлтая)",
        "red" => "Вне нормы (красная)",
        _ => zoneKey ?? "—"
    };

    private static string FormatScanStatus(RppgScanStatus status) => status switch
    {
        RppgScanStatus.New => "новый",
        RppgScanStatus.InProgress => "в процессе",
        RppgScanStatus.Failed => "ошибка",
        RppgScanStatus.Completed => "завершён",
        _ => status.ToString()
    };

    private static string FormatGender(Gender? g) => g switch
    {
        Gender.Male => "мужской",
        Gender.Female => "женский",
        _ => "не указан (используется мужской, 0)"
    };

    private static BiomarkerScaleEntity? SelectMatchingScale(
        BiomarkerEntity biomarker,
        int genderInt,
        int age,
        decimal weight) =>
        biomarker.Scales.FirstOrDefault(s =>
            genderInt >= s.GenderFrom && genderInt <= s.GenderTo &&
            age >= s.AgeFrom && age <= s.AgeTo &&
            weight >= s.WeightFrom && weight <= s.WeightTo);

    private static List<ReportZone> BuildHeartAgeZones(BiomarkerScaleEntity scale, int age)
    {
        var comments = scale.Zones
            .Where(z => z.ZoneKey != null)
            .ToDictionary(z => z.ZoneKey!, z => z.CommentUser ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        return
        [
            new ReportZone("green", age - 20, age - 1e-6,
                comments.GetValueOrDefault("green", string.Empty), $"< {age}"),
            new ReportZone("yellow", age, age + 5 - 1e-6,
                comments.GetValueOrDefault("yellow", string.Empty), $"{age} — {age + 5}"),
            new ReportZone("red", age + 5, age + 20,
                comments.GetValueOrDefault("red", string.Empty), $"{age + 5}+")
        ];
    }

    private static List<ReportZone> BuildAllNumericZones(BiomarkerScaleEntity scale)
    {
        return scale.Zones
            .Where(z => z.ZoneKey != null && z.ValueFrom.HasValue)
            .OrderBy(z => z.ValueFrom)
            .Select(z => new ReportZone(
                z.ZoneKey!,
                (double)(z.ValueFrom ?? 0m),
                z.ValueTo.HasValue ? (double)z.ValueTo.Value : double.MaxValue,
                z.CommentUser ?? string.Empty,
                z.FromToAlias ?? string.Empty))
            .ToList();
    }

    private static ReportZone FindMatchingZone(List<ReportZone> zones, double value)
    {
        var inside = zones.FirstOrDefault(z => value >= z.From && value <= z.To);
        if (inside != null)
            return inside;

        return zones
            .OrderBy(z => DistanceToInterval(value, z.From, z.To))
            .ThenBy(z => z.From)
            .First();
    }

    private static double DistanceToInterval(double value, double from, double to)
    {
        if (value >= from && value <= to) return 0;
        if (value < from) return from - value;
        return value - to;
    }

    private sealed record ReportZone(
        string ZoneKey,
        double From,
        double To,
        string CommentUser,
        string FromToAlias);
}
