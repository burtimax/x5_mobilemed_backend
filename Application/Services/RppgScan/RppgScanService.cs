using System.Text.Json;
using Application.Models.RppgScan;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.RppgScan;

public class RppgScanService : IRppgScanService
{
    private readonly AppDbContext _db;
    private readonly IScanTranscriptsService _scanTranscriptsService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RppgScanService(AppDbContext db, IScanTranscriptsService scanTranscriptsService)
    {
        _db = db;
        _scanTranscriptsService = scanTranscriptsService;
    }

    public async Task<SaveRppgSсanResponse> SaveScanAsync(Guid userId, JsonElement sdkResult, CancellationToken ct = default)
    {
        BinahSdkResultDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<BinahSdkResultDto>(sdkResult.GetRawText(), JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Невалидный JSON результата SDK: {ex.Message}", nameof(sdkResult), ex);
        }

        if (dto == null)
            throw new ArgumentException("Не удалось распарсить результат сканирования", nameof(sdkResult));

        var (scan, items) = BinahSdkResultParser.Parse(dto, sdkResult, userId);
        scan.ResultItems = items;

        await _db.UserRppgScans.AddAsync(scan, ct);
        await _db.SaveChangesAsync(ct);

        var profile = await _db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        var transcripts = await _scanTranscriptsService.BuildTranscriptsAsync(
            items,
            profile?.Age,
            profile?.Gender,
            profile?.Weight,
            ct);

        var biomarkerScores = transcripts
            .Where(t => t.ScaleMetadata != null)
            .Select(t => t.ScaleMetadata!.BiomarkerScore)
            .ToList();
        var healthScore = biomarkerScores.Count > 0
            ? (int)Math.Round(biomarkerScores.Average())
            : (int?)null;

        return new SaveRppgSсanResponse
        {
            HealthScore = healthScore,
            Scan = scan,
            Transcripts = transcripts
        };
    }
}
