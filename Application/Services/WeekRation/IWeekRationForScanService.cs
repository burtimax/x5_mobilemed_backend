using Application.Models.WeekRation;

namespace Application.Services.WeekRation;

public interface IWeekRationForScanService
{
    Task<WeekRationGenerationStatusResponseDto?> GetGenerationStatusAsync(
        Guid scanId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<WeekRationResponseDto?> GetStoredRationAsync(
        Guid scanId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Сохранённый рацион по ИД записи рациона в БД.</summary>
    Task<WeekRationResponseDto?> GetStoredRationByIdAsync(
        Guid rationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ставит генерацию рациона в очередь (статус Pending).
    /// </summary>
    /// <returns><c>true</c> если скан найден и принадлежит пользователю.</returns>
    Task<bool> TryQueueRegenerationAsync(Guid scanId, Guid userId, CancellationToken cancellationToken = default);
}
