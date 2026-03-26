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
        IReadOnlyList<DayRationMealSlotDto> slots,
        CancellationToken cancellationToken = default)
    {
        await EnsureSlotProductsExistAsync(slots, cancellationToken);

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
                    ProductId = food.ProductId,
                    Weigth = food.Weigth,
                    Reason = food.Reason
                };
                foreach (var rep in food.Replace ?? [])
                {
                    item.Replaces.Add(new WeekRationItemReplaceEntity
                    {
                        ProductId = rep.ProductId,
                        Weight = rep.Weigth,
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

    /// <summary>
    /// Оставляет только товары, существующие в каталоге: основной ID или замена из <see cref="DayRationProductRefDto.Replace"/>;
    /// при подстановке замены снимает <see cref="DayRationProductRefDto.Reason"/>. Невалидные варианты замен удаляются.
    /// </summary>
    private async Task EnsureSlotProductsExistAsync(
        IReadOnlyList<DayRationMealSlotDto> slots,
        CancellationToken cancellationToken)
    {
        var allIds = new HashSet<long>();
        foreach (var slot in slots)
        {
            foreach (var food in slot.Food)
            {
                allIds.Add(food.ProductId);
                foreach (var rep in food.Replace)
                    allIds.Add(rep.ProductId);
            }
        }

        if (allIds.Count == 0)
            return;

        var existingIds = await _db.Products
            .AsNoTracking()
            .Where(p => allIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        var existing = existingIds.ToHashSet();

        foreach (var slot in slots)
        {
            foreach (var food in slot.Food)
            {
                if (!existing.Contains(food.ProductId))
                {
                    var candidate = food.Replace.FirstOrDefault(r => existing.Contains(r.ProductId));
                    if (candidate != null)
                    {
                        food.ProductId = candidate.ProductId;
                        food.Weigth = candidate.Weigth;
                        food.Reason = null;
                        food.Replace.Remove(candidate);
                    }
                }

                food.Replace.RemoveAll(r => !existing.Contains(r.ProductId));
            }

            slot.Food.RemoveAll(food => !existing.Contains(food.ProductId));
        }
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
