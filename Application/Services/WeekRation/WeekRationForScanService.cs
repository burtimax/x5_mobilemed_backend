using Application.Models.WeekRation;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.WeekRation;

public sealed class WeekRationForScanService : IWeekRationForScanService
{
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
    public async Task<WeekRationEntity?> GetStoredRationAsync(
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

        return RationOrNull(ration);
    }

    /// <inheritdoc />
    public async Task<WeekRationEntity?> GetStoredRationByIdAsync(
        Guid rationId,
        CancellationToken cancellationToken = default)
    {
        var ration = await WeekRationsWithDetails()
            .FirstOrDefaultAsync(w => w.Id == rationId, cancellationToken);

        return RationOrNull(ration);
    }

    /// <inheritdoc />
    public async Task<WeekRationOwnerResponse?> GetRationOwnerWithExcludeProductsAsync(
        Guid rationId,
        CancellationToken cancellationToken = default)
    {
        var ration = await _db.WeekRations
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == rationId, cancellationToken);
        if (ration == null)
            return null;

        var scan = await _db.UserRppgScans
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == ration.RppgScanId, cancellationToken);
        if (scan == null)
            return null;

        var userId = scan.UserId;

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            return null;

        var excludeProducts = await _db.UserExcludeProducts
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.ExcludeProduct)
            .ToListAsync(cancellationToken);

        return new WeekRationOwnerResponse
        {
            User = user,
            ExcludeProducts = excludeProducts
        };
    }

    /// <inheritdoc />
    public async Task<WeekRationEntity?> ReplaceWeekRationItemAsync(
        Guid weekRationItemId,
        long newProductId,
        int newWeigth,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var productExists = await _db.Products
            .AnyAsync(p => p.Id == newProductId, cancellationToken);
        if (!productExists)
            return null;

        var item = await _db.WeekRationItems
            .Include(i => i.Replaces)
            .Include(i => i.WeekRation)
            .FirstOrDefaultAsync(i => i.Id == weekRationItemId, cancellationToken);

        if (item?.WeekRation == null || item.WeekRation.UserId != userId)
            return null;

        var oldProductId = item.ProductId;
        var oldWeigth = item.Weigth;
        var oldReason = item.Reason;

        if (oldProductId == newProductId)
        {
            item.Weigth = newWeigth;
            await _db.SaveChangesAsync(cancellationToken);
            return await ReloadRationEntityAsync(item.WeekRationId, cancellationToken);
        }

        var promoted = item.Replaces.Where(r => r.ProductId == newProductId).ToList();
        if (promoted.Count > 0)
        {
            foreach (var r in promoted)
                item.Replaces.Remove(r);
            item.Reason = promoted.First().Reason;
        }
        else
            item.Reason = null;

        if (!item.Replaces.Any(r => r.ProductId == oldProductId))
        {
            item.Replaces.Add(new WeekRationItemReplaceEntity
            {
                ProductId = oldProductId,
                Weight = oldWeigth,
                Reason = oldReason
            });
        }

        item.ProductId = newProductId;
        item.Weigth = newWeigth;

        await _db.SaveChangesAsync(cancellationToken);
        return await ReloadRationEntityAsync(item.WeekRationId, cancellationToken);
    }

    private async Task<WeekRationEntity?> ReloadRationEntityAsync(Guid rationId, CancellationToken cancellationToken)
    {
        var ration = await WeekRationsWithDetails()
            .FirstOrDefaultAsync(w => w.Id == rationId, cancellationToken);
        return RationOrNull(ration);
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
            .Include(w => w.Items.OrderBy(i => i.Order))
            .ThenInclude(i => i.Replaces)
            .ThenInclude(r => r.Product)
            .Include(w => w.Items.OrderBy(i => i.Order))
            .ThenInclude(i => i.Product)
            .OrderBy(i => i.CreatedAt);
    }

    private static WeekRationEntity? RationOrNull(WeekRationEntity? ration)
    {
        return ration?.Items is { Count: > 0 } ? ration : null;
    }
}
