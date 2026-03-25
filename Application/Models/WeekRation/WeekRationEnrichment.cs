using Infrastructure.Db.App.Entities;

namespace Application.Models.WeekRation;

/// <summary>Сбор идентификаторов и привязка <see cref="WeekRationProductRefDto.Product"/> из БД.</summary>
public static class WeekRationEnrichment
{
    public static IReadOnlyList<long> CollectProductIds(IEnumerable<WeekRationDayDto> days)
    {
        var ids = new HashSet<long>();
        foreach (var day in days)
        {
            AddMeal(day.Breakfast);
            AddMeal(day.Lunch);
            AddMeal(day.Snack);
            AddMeal(day.Dinner);
        }

        return ids.ToList();

        void AddMeal(List<WeekRationProductRefDto>? meal)
        {
            if (meal == null)
                return;
            foreach (var item in meal)
                ids.Add(item.Id);
        }
    }

    public static void AttachProducts(
        IList<WeekRationDayDto> days,
        IReadOnlyDictionary<long, ProductEntity> productsById)
    {
        foreach (var day in days)
        {
            AttachMeal(day.Breakfast);
            AttachMeal(day.Lunch);
            AttachMeal(day.Snack);
            AttachMeal(day.Dinner);
        }

        void AttachMeal(List<WeekRationProductRefDto>? meal)
        {
            if (meal == null)
                return;
            foreach (var item in meal)
                item.Product = productsById.TryGetValue(item.Id, out var p) ? p : null;
        }
    }
}
