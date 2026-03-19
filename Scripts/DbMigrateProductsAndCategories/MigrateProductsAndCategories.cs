using System.Text.Json;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Scripts.DbMigrateProductsAndCategories;

/// <summary>
/// Миграция данных из БД perekrestok_parsing (products.prods3, products.cats) в AppDbContext.
/// Перед запуском: создайте миграции для ProductEntity/CategoryEntity (если ещё нет) и примените их.
/// Запуск: dotnet run --project Scripts [строка_подключения_к_целевой_БД]
/// </summary>
public static class MigrateProductsAndCategories
{
    private const string SourceConnectionString =
        "Host=127.0.0.1;Port=5432;Database=perekrestok_parsing;Username=postgres;Password=123;Include Error Detail=true";

    private const string TargetConnectionString =
        "Host=127.0.0.1;Port=5432;Database=x5_mobilemed_db_1;Username=postgres;Password=123;Include Error Detail=true";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static async Task RunAsync(string? targetConnectionString = null)
    {
        var targetConn = targetConnectionString ?? TargetConnectionString;

        Console.WriteLine("Миграция: products.prods3 + products.cats -> AppDbContext");
        Console.WriteLine($"Источник: {SourceConnectionString.Split(';')[0]}...");
        Console.WriteLine($"Приёмник: {targetConn.Split(';')[0]}...");

        // Сначала категории (товары имеют FK на категории), затем товары
        await MigrateCategoriesAsync(targetConn);
        await MigrateProductsAsync(targetConn);

        Console.WriteLine("Миграция завершена.");
    }

    private static async Task MigrateCategoriesAsync(string targetConnectionString)
    {
        await using var sourceConn = new NpgsqlConnection(SourceConnectionString);
        await sourceConn.OpenAsync();

        var categories = new List<CategoryEntity>();

        await using (var cmd = new NpgsqlCommand(
            "SELECT id, parent_id, title, image_url FROM products.cats ORDER BY id",
            sourceConn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                categories.Add(new CategoryEntity
                {
                    Id = reader.GetInt32(0),
                    ParentId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    Title = reader.GetString(2),
                    ImageUrl = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
        }

        Console.WriteLine($"Загружено категорий: {categories.Count}");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(targetConnectionString)
            .Options;

        await using var ctx = new AppDbContext(options);

        // Сначала удаляем товары (FK на категории), затем категории
        ctx.Products.RemoveRange(await ctx.Products.ToListAsync());
        ctx.Categories.RemoveRange(await ctx.Categories.ToListAsync());
        await ctx.SaveChangesAsync();

        ctx.Categories.AddRange(categories);
        await ctx.SaveChangesAsync();

        Console.WriteLine($"Записано категорий: {categories.Count}");
    }

    private static async Task MigrateProductsAsync(string targetConnectionString)
    {
        await using var sourceConn = new NpgsqlConnection(SourceConnectionString);
        await sourceConn.OpenAsync();

        var products = new List<ProductEntity>();

        const string sql = """
            SELECT id, category_id, plu, title, images, labels, rating,
                   kcal_per_100_g, proteins_g_per_100_g, fats_g_per_100_g, carbs_g_per_100_g,
                   allergens, main_ingrediants, full_ingrediants, features, price,
                   product_type, manufacturer, brand, country, shelf_life_days, weight_g,
                   unit_name, volume_ml, is_alcohol, is_tobacco, is_adult_content,
                   priority, is_active
            FROM products.prods3
            ORDER BY id
            """;

        await using (var cmd = new NpgsqlCommand(sql, sourceConn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var images = ParseJson<List<string>>(reader.GetString(4)) ?? new List<string>();
                var features = ParseJson<List<ProductFeatureDto>>(reader.GetString(14)) ?? new List<ProductFeatureDto>();

                products.Add(new ProductEntity
                {
                    Id = reader.GetInt64(0),
                    CategoryId = reader.GetInt32(1),
                    Plu = reader.GetString(2),
                    Title = reader.GetString(3),
                    Images = images,
                    Labels = GetStringOrNull(reader, 5),
                    Rating = GetInt32OrNull(reader, 6),
                    KcalPer100G = GetDecimalOrNull(reader, 7),
                    ProteinsGPer100G = GetDecimalOrNull(reader, 8),
                    FatsGPer100G = GetDecimalOrNull(reader, 9),
                    CarbsGPer100G = GetDecimalOrNull(reader, 10),
                    Allergens = GetStringOrNull(reader, 11),
                    MainIngrediants = GetStringOrNull(reader, 12),
                    FullIngrediants = GetStringOrNull(reader, 13),
                    Features = features,
                    Price = GetInt32OrNull(reader, 15),
                    ProductType = GetStringOrNull(reader, 16),
                    Manufacturer = GetStringOrNull(reader, 17),
                    Brand = GetStringOrNull(reader, 18),
                    Country = GetStringOrNull(reader, 19),
                    ShelfLifeDays = GetInt32OrNull(reader, 20),
                    WeightG = GetInt32OrNull(reader, 21),
                    UnitName = GetStringOrNull(reader, 22),
                    VolumeMl = GetDecimalOrNull(reader, 23),
                    IsAlcohol = GetBooleanOrNull(reader, 24),
                    IsTobacco = GetBooleanOrNull(reader, 25),
                    IsAdultContent = GetBooleanOrNull(reader, 26),
                    Priority = reader.GetInt32(27),
                    IsActive = reader.GetBoolean(28)
                });
            }
        }

        Console.WriteLine($"Загружено товаров: {products.Count}");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(targetConnectionString)
            .Options;

        await using var ctx = new AppDbContext(options);

        const int batchSize = 1000;
        for (var i = 0; i < products.Count; i += batchSize)
        {
            var batch = products.Skip(i).Take(batchSize).ToList();
            ctx.Products.AddRange(batch);
            await ctx.SaveChangesAsync();
            Console.WriteLine($"  Записано товаров: {Math.Min(i + batchSize, products.Count)}/{products.Count}");
        }
    }

    private static T? ParseJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]") return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private static string? GetStringOrNull(NpgsqlDataReader reader, int i)
    {
        return reader.IsDBNull(i) ? null : reader.GetString(i);
    }

    private static int? GetInt32OrNull(NpgsqlDataReader reader, int i)
    {
        return reader.IsDBNull(i) ? null : reader.GetInt32(i);
    }

    private static decimal? GetDecimalOrNull(NpgsqlDataReader reader, int i)
    {
        return reader.IsDBNull(i) ? null : reader.GetDecimal(i);
    }

    private static bool? GetBooleanOrNull(NpgsqlDataReader reader, int i)
    {
        return reader.IsDBNull(i) ? null : reader.GetBoolean(i);
    }
}
