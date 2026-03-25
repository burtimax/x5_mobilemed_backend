using Application.Models.WeekRation;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.WeekRation;

public sealed class WeekRationPersistenceService : IWeekRationPersistenceService
{
    private readonly AppDbContext _db;

    public WeekRationPersistenceService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<Guid> SaveNewRationAsync(
        Guid userId,
        Guid rppgScanId,
        IReadOnlyList<WeekRationMealSlotDto> slots,
        CancellationToken cancellationToken = default)
    {
        var ration = new WeekRationEntity
        {
            UserId = userId,
            RppgScanId = rppgScanId
        };

        foreach (var slot in slots)
        {
            foreach (var food in slot.Food ?? [])
            {
                var item = new WeekRationItemEntity
                {
                    Type = slot.Type,
                    Day = slot.Day,
                    ProductId = food.Id,
                    Weigth = food.PortionGrams,
                    Reason = food.Reason
                };
                foreach (var rep in food.Replace ?? [])
                {
                    item.Replaces.Add(new WeekRationItemReplaceEntity
                    {
                        ProductId = rep.Id,
                        Weight = rep.PortionGrams,
                        Reason = null
                    });
                }

                ration.Items.Add(item);
            }
        }

        _db.WeekRations.Add(ration);
        await _db.SaveChangesAsync(cancellationToken);
        return ration.Id;
    }

    /// <inheritdoc />
    public async Task DeleteOtherRationsForScanAsync(
        Guid rppgScanId,
        Guid keepRationId,
        CancellationToken cancellationToken = default)
    {
        var toRemove = await _db.WeekRations
            .Include(w => w.Items)
            .ThenInclude(i => i.Replaces)
            .Where(w => w.RppgScanId == rppgScanId && w.Id != keepRationId)
            .ToListAsync(cancellationToken);

        if (toRemove.Count == 0)
            return;

        _db.WeekRations.RemoveRange(toRemove);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
