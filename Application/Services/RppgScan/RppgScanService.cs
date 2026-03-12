using System.Text.Json;
using Application.Models.RppgScan;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;

namespace Application.Services.RppgScan;

public class RppgScanService : IRppgScanService
{
    private readonly AppDbContext _db;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RppgScanService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserRppgScanEntity> SaveScanAsync(Guid userId, JsonElement sdkResult, CancellationToken ct = default)
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

        return scan;
    }
}
