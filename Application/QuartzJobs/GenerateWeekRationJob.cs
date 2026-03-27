using Application.Models.WeekRation;
using Application.Processing;
using Application.Services.WeekRation;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Application.QuartzJobs;

/// <summary>
/// Фоновая генерация недельного рациона для сканов с <see cref="WeekRationGenerationStatus.Pending"/>.
/// </summary>
[DisallowConcurrentExecution]
public sealed class GenerateWeekRationJob : IJob
{
    private const int MaxGenerationAttempts = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WeekRationGenerationConcurrencyGate _concurrencyGate;
    private readonly ILogger<GenerateWeekRationJob> _logger;

    public GenerateWeekRationJob(
        IServiceScopeFactory scopeFactory,
        WeekRationGenerationConcurrencyGate concurrencyGate,
        ILogger<GenerateWeekRationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _concurrencyGate = concurrencyGate;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        List<(Guid ScanId, Guid UserId)> scans;
        await using (var listScope = _scopeFactory.CreateAsyncScope())
        {
            var db = listScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rows = await db.UserRppgScans
                .AsNoTracking()
                .Where(s => s.WeekRationGenerationStatus == WeekRationGenerationStatus.Pending)
                .OrderBy(s => s.CreatedAt)
                .Select(s => new { s.Id, s.UserId })
                .ToListAsync(ct);
            scans = rows.ConvertAll(x => (ScanId: x.Id, UserId: x.UserId));
        }

        if (scans.Count == 0)
            return;

        // Список Pending снят отдельным scope. Задачи ставятся в очередь сразу; семафор из гейта ограничивает число одновременных генераций. Отдельный scope/DbContext — в ProcessOneScanAsync / TryMarkScanFailedAsync.
        using var processor = new LongTaskProcessor(_concurrencyGate.Semaphore, _concurrencyGate.MaxParallelism);
        foreach (var scan in scans)
        {
            var s = scan;
            processor.Enqueue(() => ProcessOneScanItemAsync(s, ct), ct);
        }
    }

    private async Task ProcessOneScanItemAsync((Guid ScanId, Guid UserId) scan, CancellationToken ct)
    {
        try
        {
            await ProcessOneScanAsync(scan.ScanId, scan.UserId, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Непредвиденная ошибка при генерации рациона для скана {ScanId}", scan.ScanId);
            await TryMarkScanFailedAsync(scan.ScanId, $"Исключение: {ex.Message}", ct);
        }
    }

    private async Task TryMarkScanFailedAsync(Guid scanId, string message, CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var scan = await db.UserRppgScans.FirstOrDefaultAsync(s => s.Id == scanId, ct);
            if (scan == null)
                return;
            if (scan.WeekRationGenerationStatus is not (WeekRationGenerationStatus.Pending or WeekRationGenerationStatus.InProgress))
                return;
            scan.WeekRationGenerationStatus = WeekRationGenerationStatus.Failed;
            scan.StatusMessage = Truncate(message);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось записать статус Failed для скана {ScanId}", scanId);
        }
    }

    private async Task ProcessOneScanAsync(Guid scanId, Guid userId, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var generator = sp.GetRequiredService<IWeekRationGeneratorService>();
        var persistence = sp.GetRequiredService<IWeekRationPersistenceService>();

        var claimed = await db.UserRppgScans
            .Where(s => s.Id == scanId && s.WeekRationGenerationStatus == WeekRationGenerationStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.WeekRationGenerationStatus, WeekRationGenerationStatus.InProgress)
                    .SetProperty(s => s.StatusMessage, "Подготовка к генерации рациона..."),
                ct);
        if (claimed == 0)
            return;

        var scan = await db.UserRppgScans.FirstOrDefaultAsync(s => s.Id == scanId, ct);
        if (scan == null)
        {
            _logger.LogWarning("Скан {ScanId} не найден после захвата в очередь", scanId);
            return;
        }

        WeekRationGeneratorOutcome? lastOutcome = null;

        for (var attempt = 1; attempt <= MaxGenerationAttempts; attempt++)
        {
            scan.StatusMessage = $"Генерирую рацион с помощью ИИ...";
            await db.SaveChangesAsync(ct);

            var request = new WeekRationRequest { ScanId = scanId };
            lastOutcome = await generator.GenerateAsync(request, userId, ct);

            if (lastOutcome.StatusCode == 200 && lastOutcome.Response.Ration is { Count: > 0 })
                break;

            var err = lastOutcome.Response.Error ?? "Неизвестная ошибка генерации.";
            if (attempt < MaxGenerationAttempts)
            {
                scan.StatusMessage = Truncate($"Ошибка: {err}. Повтор (попытка {attempt + 1})...");
                await db.SaveChangesAsync(ct);
            }
        }

        if (lastOutcome?.StatusCode == 200 && lastOutcome.Response.Ration is { } ration)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var newRationId = await persistence.SaveNewRationAsync(userId, scanId, ration, ct);
                await persistence.DeleteOtherRationsForScanAsync(scanId, newRationId, ct);

                scan.WeekRationGenerationStatus = WeekRationGenerationStatus.Completed;
                scan.StatusMessage = "Рацион сгенерирован и сохранён.";
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Ошибка БД при сохранении рациона для скана {ScanId}", scanId);
                scan.WeekRationGenerationStatus = WeekRationGenerationStatus.Failed;
                scan.StatusMessage = Truncate($"Ошибка сохранения рациона: {ex.Message}");
                await db.SaveChangesAsync(ct);
            }
        }
        else
        {
            var err = lastOutcome?.Response.Error
                ?? lastOutcome?.Response.RawAssistantContent
                ?? "Не удалось получить рацион от модели.";
            scan.WeekRationGenerationStatus = WeekRationGenerationStatus.Failed;
            scan.StatusMessage = Truncate(err);
            await db.SaveChangesAsync(ct);
        }
    }

    private static string Truncate(string message, int maxLen = 3950)
    {
        if (string.IsNullOrEmpty(message))
            return message;
        return message.Length <= maxLen ? message : message[..maxLen] + "…";
    }
}
