using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;

namespace Application.Services.FrontendLog;

public class FrontendLogService : IFrontendLogService
{
    private readonly AppDbContext _db;

    public FrontendLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LogEntity> SaveAsync(
        Guid? userId,
        string? logType,
        string? logJson,
        string? logMessage,
        CancellationToken ct = default)
    {
        var entity = new LogEntity
        {
            UserId = userId,
            LogSource = LogSource.Frontend,
            LogType = logType,
            Log = logJson,
            LogMessage = logMessage,
        };

        await _db.Logs.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return entity;
    }
}
