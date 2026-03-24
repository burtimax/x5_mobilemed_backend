using System.Net;
using System.Text.Json;
using ClosedXML.Excel;
using Npgsql;
using SixLabors.ImageSharp;

namespace Scripts.DbTableProductsToExcel;

/// <summary>
/// Экспорт таблицы products.prods3 (только priority = 1000) в .xlsx с первым изображением из images.
/// Запуск: dotnet run --project Scripts --to-excel [строка_подключения] [путь_к_output.xlsx]
/// Логи: только консоль (краткие сообщения).
/// </summary>
public static class Prods3ToExcel
{
    private const string DefaultConnectionString =
        "Host=127.0.0.1;Port=5432;Database=perekrestok_parsing;Username=postgres;Password=123;Include Error Detail=true";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const int ImageColumn = 5;

    /// <summary>Целевой размер превью в пикселях (сторона квадрата).</summary>
    private const int PreviewSizePx = 120;

    private const int ImageDownloadTimeoutSec = 45;

    public static async Task RunAsync(string? connectionString = null, string? outputPath = null)
    {
        var conn = connectionString ?? DefaultConnectionString;
        var outPath = outputPath ?? Path.Combine(
            "C:\\Users\\timof\\Desktop\\генерация рациона",
            $"prods3_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

        var fullPath = Path.GetFullPath(outPath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        Console.WriteLine("Экспорт products.prods3 в Excel");
        Console.WriteLine($"БД: {conn.Split(';').FirstOrDefault() ?? "?"}...");
        Console.WriteLine($"Файл: {fullPath}");

        var rows = await LoadRowsAsync(conn);
        Console.WriteLine($"Строк из БД (priority=1000): {rows.Count}");

        using var http = CreateHttpClient();
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("prods3");

        WriteHeader(sheet);

        var rowIndex = 2;
        var doneImages = 0;
        var skipNoUrl = 0;
        var skipDownload = 0;
        var skipDecode = 0;
        var skipInsert = 0;
        int c = 0;
        int count = rows.Count;

        foreach (var row in rows)
        {
            Console.WriteLine($"Обработка {++c} / {count}");
            WriteDataCells(sheet, rowIndex, row);
            sheet.Row(rowIndex).Height = PreviewSizePx * 72.0 / 96.0;

            var url = row.FirstImageUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                skipNoUrl++;
            }
            else
            {
                var bytes = await DownloadImageAsync(http, url);
                if (bytes is not { Length: > 0 })
                {
                    skipDownload++;
                }
                else
                {
                    var png = TryDecodeAsPng(bytes);
                    if (png is null)
                    {
                        skipDecode++;
                    }
                    else
                    {
                        try
                        {
                            using var ms = new MemoryStream(png, writable: false);
                            var picture = sheet.AddPicture(ms);
                            picture.MoveTo(sheet.Cell(rowIndex, ImageColumn));
                            picture.WithSize(PreviewSizePx, PreviewSizePx);
                            doneImages++;
                        }
                        catch
                        {
                            skipInsert++;
                        }
                    }
                }
            }

            rowIndex++;
            if ((rowIndex - 2) % 200 == 0)
                Console.WriteLine($"Обработано строк: {rowIndex - 2} / {rows.Count}");
        }

        Console.WriteLine(
            $"Картинки: вставлено {doneImages}; без URL: {skipNoUrl}; " +
            $"не скачалось: {skipDownload}; не декодится: {skipDecode}; вставка в Excel: {skipInsert}");

        sheet.Column(ImageColumn).Width = PreviewSizePx / 7.0;
        sheet.SheetView.FreezeRows(1);
        workbook.SaveAs(fullPath);

        Console.WriteLine("Готово.");
    }

    private static void WriteHeader(IXLWorksheet sheet)
    {
        var headers = new[]
        {
            "id", "category_id", "plu", "title", "image_preview", "images_json", "labels", "rating",
            "kcal_per_100_g", "proteins_g_per_100_g", "fats_g_per_100_g", "carbs_g_per_100_g",
            "allergens", "main_ingrediants", "full_ingrediants", "features_json",
            "price", "product_type", "manufacturer", "brand", "country",
            "shelf_life_days", "weight_g", "unit_name", "volume_ml",
            "is_alcohol", "is_tobacco", "is_adult_content", "priority", "is_active"
        };

        for (var c = 0; c < headers.Length; c++)
        {
            var cell = sheet.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }
    }

    private static void WriteDataCells(IXLWorksheet sheet, int row, Prods3Row r)
    {
        var col = 1;
        sheet.Cell(row, col++).Value = r.Id;
        sheet.Cell(row, col++).Value = r.CategoryId;
        sheet.Cell(row, col++).Value = r.Plu;
        sheet.Cell(row, col++).Value = r.Title;
        col++; // image_preview
        sheet.Cell(row, col++).Value = r.ImagesJson;
        sheet.Cell(row, col++).Value = r.Labels ?? string.Empty;
        if (r.Rating.HasValue)
            sheet.Cell(row, col).Value = r.Rating.Value;
        col++;
        SetDecimal(sheet.Cell(row, col++), r.KcalPer100G);
        SetDecimal(sheet.Cell(row, col++), r.ProteinsGPer100G);
        SetDecimal(sheet.Cell(row, col++), r.FatsGPer100G);
        SetDecimal(sheet.Cell(row, col++), r.CarbsGPer100G);
        sheet.Cell(row, col++).Value = r.Allergens ?? string.Empty;
        sheet.Cell(row, col++).Value = r.MainIngrediants ?? string.Empty;
        sheet.Cell(row, col++).Value = r.FullIngrediants ?? string.Empty;
        sheet.Cell(row, col++).Value = r.FeaturesJson;
        if (r.Price.HasValue)
            sheet.Cell(row, col).Value = r.Price.Value;
        col++;
        sheet.Cell(row, col++).Value = r.ProductType ?? string.Empty;
        sheet.Cell(row, col++).Value = r.Manufacturer ?? string.Empty;
        sheet.Cell(row, col++).Value = r.Brand ?? string.Empty;
        sheet.Cell(row, col++).Value = r.Country ?? string.Empty;
        if (r.ShelfLifeDays.HasValue)
            sheet.Cell(row, col).Value = r.ShelfLifeDays.Value;
        col++;
        if (r.WeightG.HasValue)
            sheet.Cell(row, col).Value = r.WeightG.Value;
        col++;
        sheet.Cell(row, col++).Value = r.UnitName ?? string.Empty;
        SetDecimal(sheet.Cell(row, col++), r.VolumeMl);
        sheet.Cell(row, col++).Value = FormatBool(r.IsAlcohol);
        sheet.Cell(row, col++).Value = FormatBool(r.IsTobacco);
        sheet.Cell(row, col++).Value = FormatBool(r.IsAdultContent);
        sheet.Cell(row, col++).Value = r.Priority;
        sheet.Cell(row, col++).Value = r.IsActive;
    }

    private static void SetDecimal(IXLCell cell, decimal? v)
    {
        if (v.HasValue)
            cell.Value = v.Value;
    }

    private static string FormatBool(bool? v) => v switch
    {
        true => "true",
        false => "false",
        _ => string.Empty
    };

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            AutomaticDecompression = DecompressionMethods.All
        };
        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(ImageDownloadTimeoutSec)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (compatible; Prods3ToExcel/1.0; +https://localhost)");
        return client;
    }

    /// <summary>Скачивание без исключений: сеть, таймаут, не-HTTP 200 — возвращаем null.</summary>
    private static async Task<byte[]?> DownloadImageAsync(HttpClient http, string url)
    {
        try
        {
            using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!resp.IsSuccessStatusCode)
                return null;

            Console.WriteLine("Получил изображение - " + url);
            return await resp.Content.ReadAsByteArrayAsync();
        }
        catch
        {
            Console.WriteLine("Ошибка получения изображения - " + url);
            return null;
        }
    }

    /// <summary>Декодирует JPEG/PNG/WebP/GIF/BMP и т.д. и кодирует в PNG для ClosedXML.</summary>
    private static byte[]? TryDecodeAsPng(ReadOnlySpan<byte> raw)
    {
        try
        {
            using var image = Image.Load(raw);
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static async Task<List<Prods3Row>> LoadRowsAsync(string connectionString)
    {
        var list = new List<Prods3Row>();

        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync();

        const string sql = """
            SELECT id, category_id, plu, title, images::text, labels, rating,
                   kcal_per_100_g, proteins_g_per_100_g, fats_g_per_100_g, carbs_g_per_100_g,
                   allergens, main_ingrediants, full_ingrediants, features::text, price,
                   product_type, manufacturer, brand, country, shelf_life_days, weight_g,
                   unit_name, volume_ml, is_alcohol, is_tobacco, is_adult_content,
                   priority, is_active
            FROM products.prods3
            WHERE priority = 1000
            ORDER BY id
            """;

        await using var cmd = new NpgsqlCommand(sql, cn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var imagesJson = reader.GetString(4);
            list.Add(new Prods3Row
            {
                Id = reader.GetInt64(0),
                CategoryId = reader.GetInt32(1),
                Plu = reader.GetString(2),
                Title = reader.GetString(3),
                ImagesJson = imagesJson,
                FirstImageUrl = GetFirstImageUrl(imagesJson),
                Labels = GetStringOrNull(reader, 5),
                Rating = GetInt32OrNull(reader, 6),
                KcalPer100G = GetDecimalOrNull(reader, 7),
                ProteinsGPer100G = GetDecimalOrNull(reader, 8),
                FatsGPer100G = GetDecimalOrNull(reader, 9),
                CarbsGPer100G = GetDecimalOrNull(reader, 10),
                Allergens = GetStringOrNull(reader, 11),
                MainIngrediants = GetStringOrNull(reader, 12),
                FullIngrediants = GetStringOrNull(reader, 13),
                FeaturesJson = reader.GetString(14),
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

        return list;
    }

    private static string? GetFirstImageUrl(string imagesJson)
    {
        try
        {
            var urls = JsonSerializer.Deserialize<List<string>>(imagesJson, JsonOptions);
            return urls?.FirstOrDefault(static u => !string.IsNullOrWhiteSpace(u));
        }
        catch
        {
            return null;
        }
    }

    private static string? GetStringOrNull(NpgsqlDataReader reader, int i) =>
        reader.IsDBNull(i) ? null : reader.GetString(i);

    private static int? GetInt32OrNull(NpgsqlDataReader reader, int i) =>
        reader.IsDBNull(i) ? null : reader.GetInt32(i);

    private static decimal? GetDecimalOrNull(NpgsqlDataReader reader, int i) =>
        reader.IsDBNull(i) ? null : reader.GetDecimal(i);

    private static bool? GetBooleanOrNull(NpgsqlDataReader reader, int i) =>
        reader.IsDBNull(i) ? null : reader.GetBoolean(i);

    private sealed class Prods3Row
    {
        public long Id { get; init; }
        public int CategoryId { get; init; }
        public string Plu { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string ImagesJson { get; init; } = "[]";
        public string? FirstImageUrl { get; init; }
        public string? Labels { get; init; }
        public int? Rating { get; init; }
        public decimal? KcalPer100G { get; init; }
        public decimal? ProteinsGPer100G { get; init; }
        public decimal? FatsGPer100G { get; init; }
        public decimal? CarbsGPer100G { get; init; }
        public string? Allergens { get; init; }
        public string? MainIngrediants { get; init; }
        public string? FullIngrediants { get; init; }
        public string FeaturesJson { get; init; } = "[]";
        public int? Price { get; init; }
        public string? ProductType { get; init; }
        public string? Manufacturer { get; init; }
        public string? Brand { get; init; }
        public string? Country { get; init; }
        public int? ShelfLifeDays { get; init; }
        public int? WeightG { get; init; }
        public string? UnitName { get; init; }
        public decimal? VolumeMl { get; init; }
        public bool? IsAlcohol { get; init; }
        public bool? IsTobacco { get; init; }
        public bool? IsAdultContent { get; init; }
        public int Priority { get; init; }
        public bool IsActive { get; init; }
    }
}
