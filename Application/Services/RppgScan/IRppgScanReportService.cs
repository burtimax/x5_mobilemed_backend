namespace Application.Services.RppgScan;

/// <summary>
/// Текстовый отчёт по сохранённому скану RPPG (норма / погранично / вне нормы).
/// </summary>
public interface IRppgScanReportService
{
    /// <summary>
    /// Загружает скан с профилем пользователя и показателями, строит plain text отчёт.
    /// </summary>
    /// <returns>Текст отчёта или <c>null</c>, если скан не найден.</returns>
    Task<string?> GetReportTextAsync(Guid scanId, CancellationToken cancellationToken = default);
}
