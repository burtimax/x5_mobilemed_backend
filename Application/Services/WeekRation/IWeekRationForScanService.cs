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

    /// <summary>
    /// Заменяет основной товар позиции; прежний основной переносится в <c>Replaces</c>, если там ещё нет такого <c>ProductId</c>.
    /// При выборе товара из замен соответствующие записи замен удаляются.
    /// </summary>
    /// <returns>Обновлённый рацион или <c>null</c>, если позиция не найдена, нет доступа или товар отсутствует в каталоге.</returns>
    Task<WeekRationResponseDto?> ReplaceWeekRationItemAsync(
        Guid weekRationItemId,
        long newProductId,
        int newWeigth,
        Guid userId,
        CancellationToken cancellationToken = default);
}
