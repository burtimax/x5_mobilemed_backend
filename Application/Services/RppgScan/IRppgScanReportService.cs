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

    /// <summary>
    /// То же, что <see cref="GetReportTextAsync"/>, но только если скан принадлежит <paramref name="userId"/>.
    /// </summary>
    Task<string?> GetReportTextForUserAsync(
        Guid scanId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Контекст для двухшаговой LLM-генерации рациона: блок фокусных показателей и отчёт без них.
    /// <c>null</c>, если скан не найден или не принадлежит пользователю.
    /// </summary>
    Task<RppgScanRationContext?> GetRationLlmContextForUserAsync(
        Guid scanId,
        Guid userId,
        IReadOnlyCollection<string> focusKeys,
        CancellationToken cancellationToken = default);
}
