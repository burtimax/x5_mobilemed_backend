using Application.Models.RppgScan;
using Infrastructure.Db.App.Entities;

namespace Application.Services.RppgScan;

/// <summary>
/// Сервис формирования расшифровки результатов сканирования (Transcripts).
/// </summary>
public interface IScanTranscriptsService
{
    /// <summary>
    /// Формирует список Transcripts для результатов сканирования.
    /// Сопоставляет показатели с зонами биомаркеров с учётом пола, возраста и веса.
    /// </summary>
    /// <param name="scanItems">Результаты сканирования</param>
    /// <param name="userAge">Возраст пользователя (важно для heartAge)</param>
    /// <param name="userGender">Пол пользователя</param>
    /// <param name="userWeight">Вес пользователя (кг)</param>
    /// <param name="ct">Токен отмены</param>
    Task<List<ScanTranscriptItem>> BuildTranscriptsAsync(
        IReadOnlyList<UserRppgScanResultItemEntity> scanItems,
        int? userAge,
        Gender? userGender,
        decimal? userWeight,
        CancellationToken ct = default);
}
