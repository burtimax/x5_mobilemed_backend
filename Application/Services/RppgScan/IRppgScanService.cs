using System.Text.Json;
using Application.Models.RppgScan;
using Infrastructure.Db.App.Entities;
using Infrastructure.Models;

namespace Application.Services.RppgScan;

public interface IRppgScanService
{
    /// <summary>
    /// Сохраняет результат сканирования Rppg из JSON SDK и возвращает с расшифровкой Transcripts.
    /// </summary>
    Task<SaveRppgSсanResponse> SaveScanAsync(Guid userId, JsonElement dto, CancellationToken ct = default);

    /// <summary>
    /// Возвращает историю сканов пользователя с пагинацией.
    /// </summary>
    Task<PagedList<SaveRppgSсanResponse>> GetScansHistoryAsync(Guid userId, int pageNumber, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Преобразует скан и его результаты в SaveRppgSсanResponse с Transcripts и HealthScore.
    /// </summary>
    Task<SaveRppgSсanResponse> BuildSaveRppgScanResponseAsync(
        UserRppgScanEntity scan,
        IReadOnlyList<UserRppgScanResultItemEntity> items,
        CancellationToken ct = default);
}
