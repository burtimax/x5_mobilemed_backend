using System.Text.Json;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;

namespace Scripts.CategoriesAndProductsToJson;

/// <summary>
/// Экспорт категорий с товарами из AppDbContext в JSON.
/// Запуск: dotnet run --project Scripts [строка_подключения] [путь_к_output_json]
/// </summary>
public static class CategoriesAndProductsToJson
{
    private const string DefaultConnectionString =
        "Host=127.0.0.1;Port=5432;Database=x5_mobilemed_db_1;Username=postgres;Password=123;Include Error Detail=true";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public static async Task RunAsync(string? connectionString = null, string? outputPath = null)
    {
        var conn = connectionString ?? DefaultConnectionString;
        //var path = outputPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "docs", "categories_and_products.json");

        Console.WriteLine("Экспорт категорий с товарами в JSON");
        Console.WriteLine($"БД: {conn.Split(';').FirstOrDefault() ?? "?"}...");
        Console.WriteLine("Фильтр товаров: Priority < 100");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(conn)
            .Options;

        await using var ctx = new AppDbContext(options);

        var categories = await ctx.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .OrderBy(c => c.Id)
            .ToListAsync();

        var dtos = categories.Select(c => new CategoryWithProductsDto
        {
            Id = c.Id,
            Title = c.Title,
            Products = c.Products.Where(p => p.Priority < 100).Select(MapProduct).ToList()
        }).ToList();

        dtos = dtos.Where(d => d.Products.Any()).ToList();

        List<string> excludeFeatures = new List<string>()
        {
            "osnovnye-ingredienty",
            "sostav-polnyi",
            "sostav",
            "kkal",
            "jiry",
            "belki",
            "uglevody",
            "brand",
            "strana"
        };

        for (int i = 0; i < dtos.Count; i++)
        {
            for (int j = 0; j < dtos[i].Products.Count; j++)
            {
                var prod = dtos[i].Products[j];
                prod.Features = prod.Features.Where(f => !excludeFeatures.Contains(f.Key)).ToList();
            }
        }

        var json = JsonSerializer.Serialize(dtos, JsonOptions);



        //var fullPath = Path.GetFullPath(path);
        //var dir = Path.GetDirectoryName(fullPath);
        // if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        //     Directory.CreateDirectory(dir);
        //
        // await File.WriteAllTextAsync(fullPath, json);

        Console.WriteLine($"Категорий: {categories.Count}, товаров: {dtos.Sum(c => c.Products.Count)}");
        //Console.WriteLine($"JSON записан: {fullPath}");
    }

    private static ProductDto MapProduct(ProductEntity p) => new()
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
        public List<ProductDto> Products { get; init; } = [];
    }

    private sealed class ProductDto
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
