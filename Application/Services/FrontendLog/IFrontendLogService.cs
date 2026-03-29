using Infrastructure.Db.App.Entities;

namespace Application.Services.FrontendLog;

public interface IFrontendLogService
{
    Task<LogEntity> SaveAsync(
        Guid? userId,
        string? logType,
        string? logJson,
        string? logMessage,
        CancellationToken ct = default);
}
