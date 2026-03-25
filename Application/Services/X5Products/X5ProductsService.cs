using System.Globalization;
using System.Text;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.X5Products;

/// <inheritdoc cref="IX5ProductsService" />
public sealed class X5ProductsService : IX5ProductsService
{
    private static readonly string[] ExcludeFeatureKeys =
    [
        "osnovnye-ingredienty",
        "sostav-polnyi",
        "sostav",
        "kkal",
        "jiry",
        "belki",
        "uglevody",
        "brand",
        "strana"
    ];

    private readonly AppDbContext _db;

    public X5ProductsService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<string> GetProductsCatalogTextAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);

        var dtos = categories.Select(c => new CategoryWithProductsDto
        {
            Id = c.Id,
            Title = c.Title,
            Products = c.Products
                .Where(p => p.Priority < 100)
                .OrderBy(p => p.Title)
                .Select(MapProduct)
                .ToList()
        }).ToList();

        dtos = dtos.Where(d => d.Products.Count > 0).ToList();

        foreach (var cat in dtos)
        {
            foreach (var prod in cat.Products)
            {
                prod.Features = prod.Features
                    .Where(f => !ExcludeFeatureKeys.Contains(f.Key, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("Каталог товаров");
        sb.AppendLine($"Сформировано: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine(new string('═', 72));
        sb.AppendLine();

        for (var i = 0; i < dtos.Count; i++)
        {
            var cat = dtos[i];
            sb.AppendLine($"Категория: {cat.Title}");
            sb.AppendLine($"ID категории: {cat.Id}");
            sb.AppendLine(new string('─', 72));

            for (var j = 0; j < cat.Products.Count; j++)
            {
                AppendProduct(sb, cat.Products[j], j + 1);
                sb.AppendLine();
            }

            if (i < dtos.Count - 1)
            {
                sb.AppendLine(new string('·', 72));
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<long, ProductEntity>> GetProductsByIdsAsync(
        IReadOnlyCollection<long> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds == null || productIds.Count == 0)
            return new Dictionary<long, ProductEntity>();

        var distinctIds = productIds.Distinct().ToList();
        var list = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => distinctIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return list.ToDictionary(p => p.Id);
    }

    private static void AppendProduct(StringBuilder sb, ProductExportRow p, int indexInCategory)
    {
        sb.AppendLine($"  [{indexInCategory}] {p.Title}");
        sb.AppendLine($"      ID: {p.Id}    PLU: {p.Plu}");

        var sizeLine = FormatWeightVolume(p);
        if (sizeLine != null)
            sb.AppendLine($"      {sizeLine}");

        AppendNutrition(sb, p);

        if (!string.IsNullOrWhiteSpace(p.Allergens))
            sb.AppendLine($"      Аллергены: {NormalizeWhitespace(p.Allergens)}");

        var composition = !string.IsNullOrWhiteSpace(p.FullIngrediants)
            ? p.FullIngrediants
            : p.MainIngrediants;
        if (!string.IsNullOrWhiteSpace(composition))
        {
            sb.AppendLine("      Состав:");
            foreach (var line in WrapForIndent(composition!, 66))
                sb.AppendLine($"         {line}");
        }
    }

    private static void AppendNutrition(StringBuilder sb, ProductExportRow p)
    {
        var hasAny = p.KcalPer100G.HasValue || p.ProteinsGPer100G.HasValue || p.FatsGPer100G.HasValue
                     || p.CarbsGPer100G.HasValue;
        if (!hasAny)
            return;

        sb.AppendLine(
            $"      КБЖУ на 100 г: {FormatKbjLine(p.KcalPer100G, p.ProteinsGPer100G, p.FatsGPer100G, p.CarbsGPer100G)}");

        if (p.WeightG is > 0 and var grams)
        {
            var factor = grams / 100m;
            sb.AppendLine(
                $"      КБЖУ на упаковку (~{grams} г): " +
                $"{FormatKbjLine(Mul(p.KcalPer100G, factor), Mul(p.ProteinsGPer100G, factor), Mul(p.FatsGPer100G, factor), Mul(p.CarbsGPer100G, factor))}");
        }
    }

    private static string? FormatWeightVolume(ProductExportRow p)
    {
        var parts = new List<string>();

        if (p.WeightG is > 0 and var g)
        {
            var w = $"{g} г";
            if (!string.IsNullOrWhiteSpace(p.UnitName))
                w += $" ({p.UnitName.Trim()})";
            parts.Add($"Вес нетто: {w}");
        }

        if (p.VolumeMl is > 0)
            parts.Add($"Объём: {FormatDecimal(p.VolumeMl.Value)} мл");

        return parts.Count == 0 ? null : string.Join("    ", parts);
    }

    private static string FormatKbjLine(decimal? kcal, decimal? protein, decimal? fat, decimal? carbs)
    {
        var k = kcal.HasValue ? $"{FormatDecimal(kcal.Value)} ккал" : "— ккал";
        var b = protein.HasValue ? $"{FormatDecimal(protein.Value)} г белков" : "— г белков";
        var f = fat.HasValue ? $"{FormatDecimal(fat.Value)} г жиров" : "— г жиров";
        var u = carbs.HasValue ? $"{FormatDecimal(carbs.Value)} г углеводов" : "— г углеводов";
        return $"{k}; {b}; {f}; {u}";
    }

    private static decimal? Mul(decimal? per100, decimal factor) =>
        per100.HasValue ? decimal.Round(per100.Value * factor, 2, MidpointRounding.AwayFromZero) : null;

    private static string FormatDecimal(decimal value) =>
        value == decimal.Truncate(value)
            ? decimal.Truncate(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string NormalizeWhitespace(string s) =>
        string.Join(' ', s.Split((string[]?)null!, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static IEnumerable<string> WrapForIndent(string text, int maxLen)
    {
        text = NormalizeWhitespace(text);
        if (text.Length <= maxLen)
        {
            yield return text;
            yield break;
        }

        var start = 0;
        while (start < text.Length)
        {
            var rest = text.Length - start;
            if (rest <= maxLen)
            {
                yield return text[start..];
                yield break;
            }

            var slice = text.AsSpan(start, maxLen);
            var lastSpace = slice.LastIndexOf(' ');
            var take = lastSpace > maxLen / 4 ? lastSpace : maxLen;
            yield return text.Substring(start, take).TrimEnd();
            start += take;
            while (start < text.Length && text[start] == ' ')
                start++;
        }
    }

    private static ProductExportRow MapProduct(ProductEntity p) => new()
    {
        Id = p.Id,
        CategoryId = p.CategoryId,
        Plu = p.Plu,
        Title = p.Title,
        Labels = p.Labels,
        Rating = p.Rating,
        KcalPer100G = p.KcalPer100G,
        ProteinsGPer100G = p.ProteinsGPer100G,
        FatsGPer100G = p.FatsGPer100G,
        CarbsGPer100G = p.CarbsGPer100G,
        Allergens = p.Allergens,
        MainIngrediants = p.MainIngrediants,
        FullIngrediants = p.FullIngrediants,
        Features = p.Features,
        Price = Convert.ToInt32(Math.Round((p.Price ?? 0) / 100.0)),
        ProductType = p.ProductType,
        Manufacturer = p.Manufacturer,
        Brand = p.Brand,
        Country = p.Country,
        ShelfLifeDays = p.ShelfLifeDays,
        WeightG = p.WeightG,
        UnitName = p.UnitName,
        VolumeMl = p.VolumeMl,
    };

    private sealed class CategoryWithProductsDto
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public List<ProductExportRow> Products { get; init; } = [];
    }

    private sealed class ProductExportRow
    {
        public long Id { get; init; }
        public int CategoryId { get; init; }
        public string Plu { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? Labels { get; init; }
        public int? Rating { get; init; }
        public decimal? KcalPer100G { get; init; }
        public decimal? ProteinsGPer100G { get; init; }
        public decimal? FatsGPer100G { get; init; }
        public decimal? CarbsGPer100G { get; init; }
        public string? Allergens { get; init; }
        public string? MainIngrediants { get; init; }
        public string? FullIngrediants { get; init; }
        public List<ProductFeatureDto> Features { get; set; } = [];
        public int? Price { get; init; }
        public string? ProductType { get; init; }
        public string? Manufacturer { get; init; }
        public string? Brand { get; init; }
        public string? Country { get; init; }
        public int? ShelfLifeDays { get; init; }
        public int? WeightG { get; init; }
        public string? UnitName { get; init; }
        public decimal? VolumeMl { get; init; }
    }
}
