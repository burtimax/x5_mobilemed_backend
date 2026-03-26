using Infrastructure.Db.App.Entities;

namespace Application.Models.WeekRation;

/// <summary>Сбор id и привязка <see cref="ProductEntity"/> к позициям и заменам.</summary>
public static class WeekRationEnrichment
{
    public static IReadOnlyList<long> CollectProductIds(IEnumerable<DayRationMealSlotDto> slots)
    {
        var ids = new HashSet<long>();
        foreach (var slot in slots)
        {
            foreach (var item in slot.Food ?? [])
            {
                ids.Add(item.ProductId);
                foreach (var r in item.Replace ?? [])
                    ids.Add(r.ProductId);
            }
        }

        return ids.ToList();
    }

    public static void AttachProducts(
        IList<DayRationMealSlotDto> slots,
        IReadOnlyDictionary<long, ProductEntity> productsById)
    {
        foreach (var slot in slots)
        {
            foreach (var item in slot.Food ?? [])
            {
                item.Product = productsById.TryGetValue(item.ProductId, out var p) ? p : null;
                foreach (var r in item.Replace ?? [])
                    r.Product = productsById.TryGetValue(r.ProductId, out var rp) ? rp : null;
            }
        }
    }
}
