using System.Text.Json;
using Application.Models.RppgScan;
using Infrastructure.Db.App.Entities;

namespace Application.Services.RppgScan;

public interface IRppgScanService
{
    /// <summary>
    /// Сохраняет результат сканирования Rppg из JSON SDK и возвращает с расшифровкой Transcripts.
    /// </summary>
    Task<SaveRppgSсanResponse> SaveScanAsync(Guid userId, JsonElement dto, CancellationToken ct = default);
}
