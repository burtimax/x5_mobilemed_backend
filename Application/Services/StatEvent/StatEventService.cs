using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;

namespace Application.Services.StatEvent;

public class StatEventService : IStatEventService
{
    private readonly AppDbContext _db;

    public StatEventService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<StatEventEntity> SaveInternalEvent(string? type, string? data, double? duration, CancellationToken ct = default)
    {
        var statEvent = new StatEventEntity
        {
            Data = (data ?? "").Substring(0, Math.Min(type?.Length ?? 0, 100)),
            Type = (type ?? "").Substring(0, Math.Min(type?.Length ?? 0, 30)),
            DurationSeconds = duration
        };

        await _db.StatEvents.AddAsync(statEvent, ct);
        await _db.SaveChangesAsync(ct);
        return statEvent;
    }

    public async Task<StatEventEntity> SaveRequestEvent(Guid? userId, long? sessionId, string? type, string? data, double? duration, CancellationToken ct = default)
    {
        var statEvent = new StatEventEntity
        {
            SessionId = sessionId,
            Data = (data ?? "").Substring(0, Math.Min(data?.Length ?? 0, 100)),
            UserId = userId,
            Type = (type ?? "").Substring(0, Math.Min(type?.Length ?? 0, 30)),
            DurationSeconds = duration
        };

        await _db.StatEvents.AddAsync(statEvent, ct);
        await _db.SaveChangesAsync(ct);
        return statEvent;
    }
}
