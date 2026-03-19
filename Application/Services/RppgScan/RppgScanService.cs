using System.Text.Json;
using Application.Models.RppgScan;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Infrastructure.Models;
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

        return await BuildSaveRppgScanResponseAsync(scan, items, ct);
    }

    /// <inheritdoc />
    public async Task<PagedList<SaveRppgSсanResponse>> GetScansHistoryAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.UserRppgScans
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var scans = await query
            .Include(s => s.ResultItems)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = new List<SaveRppgSсanResponse>();
        foreach (var scan in scans)
        {
            var response = await BuildSaveRppgScanResponseAsync(scan, scan.ResultItems, ct);
            items.Add(response);
        }

        return new PagedList<SaveRppgSсanResponse>(items, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<SaveRppgSсanResponse> BuildSaveRppgScanResponseAsync(
        UserRppgScanEntity scan,
        IReadOnlyList<UserRppgScanResultItemEntity> items,
        CancellationToken ct = default)
    {
        var profile = await _db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == scan.UserId, ct);

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
