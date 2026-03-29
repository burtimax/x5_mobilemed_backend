using Infrastructure.Db.App;
using Microsoft.EntityFrameworkCore;

namespace Scripts.DownloadImagesToFolder;

/// <summary>
/// Скачивание изображений товаров (Priority &lt; 100) в папку с путями вида .../xdelivery/&lt;subpath&gt; → targetDir/&lt;subpath&gt;.
/// Запуск из корня backend: dotnet run --project Scripts, раскомментировав вызов в Program.cs, либо вызов RunAsync из кода.
/// </summary>
public static class DownloadImagesToFolder
{
    private const string XdeliveryMarker = "/xdelivery/";

    private const string DefaultConnectionString =
        "Host=127.0.0.1;Port=5432;Database=x5_mobilemed_db_1;Username=postgres;Password=123;Include Error Detail=true";

    /// <param name="connectionString">Строка подключения PostgreSQL; по умолчанию локальная dev.</param>
    /// <param name="targetDirectory">Корневая папка (например "/images" или полный путь).</param>
    /// <param name="overwriteExisting">Перезаписывать уже существующие файлы.</param>
    public static async Task RunAsync(
        string? connectionString = null,
        string? targetDirectory = null,
        bool overwriteExisting = false)
    {
        var conn = connectionString ?? DefaultConnectionString;
        var root = Path.GetFullPath(targetDirectory ?? Path.Combine(Environment.CurrentDirectory, "images"));
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar) || root.EndsWith(Path.AltDirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        Console.WriteLine("Скачивание картинок товаров (Priority < 100)");
        Console.WriteLine($"БД: {conn.Split(';').FirstOrDefault() ?? "?"}...");
        Console.WriteLine($"Папка: {root}");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(conn)
            .Options;

        await using var ctx = new AppDbContext(options);

        var products = await ctx.Products
            .AsNoTracking()
            .Where(p => p.Priority < 100)
            .Select(p => new { p.Id, p.Images })
            .ToListAsync();

        var relativeToUrl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var skippedUrls = new List<string>();
        foreach (var p in products)
        {
            foreach (var url in p.Images)
            {
                if (string.IsNullOrWhiteSpace(url))
                    continue;
                var rel = TryGetRelativePathAfterXdelivery(url);
                if (rel == null)
                {
                    skippedUrls.Add(url);
                    continue;
                }

                relativeToUrl.TryAdd(rel, url);
            }
        }

        if (skippedUrls.Count > 0)
        {
            Console.WriteLine(
                $"Пропущено URL без сегмента '{XdeliveryMarker.Trim('/')}': {skippedUrls.Distinct().Count()}");
        }

        Console.WriteLine($"Уникальных файлов (путей): {relativeToUrl.Count}");

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        var downloaded = 0;
        var skippedExisting = 0;
        var errors = 0;

        foreach (var (relative, url) in relativeToUrl)
        {
            try
            {
                var fullPath = Path.GetFullPath(Path.Combine(root, relative));
                if (!fullPath.StartsWith(rootPrefix, Environment.OSVersion.Platform == PlatformID.Win32NT
                        ? StringComparison.OrdinalIgnoreCase
                        : StringComparison.Ordinal))
                {
                    errors++;
                    Console.WriteLine($"Небезопасный путь, пропуск: {relative}");
                    continue;
                }

                if (!overwriteExisting && File.Exists(fullPath))
                {
                    skippedExisting++;
                    continue;
                }

                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);
                downloaded++;
            }
            catch (Exception ex)
            {
                errors++;
                Console.WriteLine($"Ошибка: {url} — {ex.Message}");
            }
        }

        Console.WriteLine($"Скачано новых: {downloaded}, пропущено (уже есть): {skippedExisting}, ошибок: {errors}");
    }

    /// <summary>
    /// Для https://.../i/800x800-fit/xdelivery/files/a/b/c.jpg возвращает files/a/b/c.jpg.
    /// </summary>
    internal static string? TryGetRelativePathAfterXdelivery(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return null;

        var path = uri.AbsolutePath;
        var idx = path.IndexOf(XdeliveryMarker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;

        var tail = path[(idx + XdeliveryMarker.Length)..].Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrEmpty(tail) || tail.Contains("..", StringComparison.Ordinal))
            return null;

        return tail.Replace('/', Path.DirectorySeparatorChar);
    }
}
