using Application.Models.WeekRation;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.WeekRation;

public sealed class WeekRationForScanService : IWeekRationForScanService
{
    private static readonly string[] MealTypeOrder = ["breakfast", "lunch", "snack", "dinner"];

    private readonly AppDbContext _db;

    public WeekRationForScanService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<WeekRationGenerationStatusResponseDto?> GetGenerationStatusAsync(
        Guid scanId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var scan = await _db.UserRppgScans
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scanId && s.UserId == userId, cancellationToken);

        if (scan == null)
            return null;

        return new WeekRationGenerationStatusResponseDto
        {
            Status = scan.WeekRationGenerationStatus,
            StatusMessage = scan.StatusMessage
        };
    }

    /// <inheritdoc />
    public async Task<WeekRationResponseDto?> GetStoredRationAsync(
        Guid scanId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var owned = await _db.UserRppgScans
            .AsNoTracking()
            .AnyAsync(s => s.Id == scanId && s.UserId == userId, cancellationToken);
        if (!owned)
            return null;

        var ration = await WeekRationsWithDetails()
            .Where(w => w.RppgScanId == scanId && w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return MapRationToResponse(ration);
    }

    /// <inheritdoc />
    public async Task<WeekRationResponseDto?> GetStoredRationByIdAsync(
        Guid rationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var ration = await WeekRationsWithDetails()
            .FirstOrDefaultAsync(w => w.Id == rationId && w.UserId == userId, cancellationToken);

        return MapRationToResponse(ration);
    }

    /// <inheritdoc />
    public async Task<bool> TryQueueRegenerationAsync(
        Guid scanId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var scan = await _db.UserRppgScans
            .FirstOrDefaultAsync(s => s.Id == scanId && s.UserId == userId, cancellationToken);

        if (scan == null)
            return false;

        scan.WeekRationGenerationStatus = WeekRationGenerationStatus.Pending;
        scan.StatusMessage = "Повторная генерация рациона поставлена в очередь.";
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<WeekRationEntity> WeekRationsWithDetails()
    {
        return _db.WeekRations
            .AsNoTracking()
            .Include(w => w.Items)
            .ThenInclude(i => i.Replaces)
            .ThenInclude(r => r.Product)
            .Include(w => w.Items)
            .ThenInclude(i => i.Product);
    }

    private static WeekRationResponseDto? MapRationToResponse(WeekRationEntity? ration)
    {
        if (ration?.Items is not { Count: > 0 })
            return null;

        var slots = MapItemsToSlots(ration.Items);
        return new WeekRationResponseDto
        {
            Id = ration.Id,
            RppgScanId = ration.RppgScanId,
            CreatedAt = ration.CreatedAt,
            Ration = slots
        };
    }

    private static List<WeekRationMealSlotDto> MapItemsToSlots(IReadOnlyList<WeekRationItemEntity> items)
    {
        return items
            .GroupBy(i => (i.Day, TypeKey: i.Type.Trim().ToLowerInvariant()))
            .OrderBy(g => g.Key.Day)
            .ThenBy(g => MealOrdinal(g.Key.TypeKey))
            .Select(g =>
            {
                var first = g.First();
                return new WeekRationMealSlotDto
                {
                    Day = g.Key.Day,
                    Type = first.Type,
                    Food = g.Select(MapFood).ToList()
                };
            })
            .ToList();
    }

    private static WeekRationProductRefDto MapFood(WeekRationItemEntity item)
    {
        return new WeekRationProductRefDto
        {
            Id = item.ProductId,
            Reason = item.Reason,
            PortionGrams = item.Weigth,
            Product = item.Product,
            Replace = item.Replaces
                .OrderBy(r => r.Id)
                .Select(r => new WeekRationProductReplaceCandidateDto
                {
                    Id = r.ProductId,
                    PortionGrams = r.Weight,
                    Reason = r.Reason,
                    Product = r.Product
                })
                .ToList()
        };
    }

    private static int MealOrdinal(string typeNormalized)
    {
        var i = Array.IndexOf(MealTypeOrder, typeNormalized);
        return i >= 0 ? i : 99;
    }
}
