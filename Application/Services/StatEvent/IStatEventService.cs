using Infrastructure.Db.App.Entities;

namespace Application.Services.StatEvent;

public interface IStatEventService
{
    public Task<StatEventEntity> SaveInternalEvent(string? type, string? data, double? duration, CancellationToken ct = default);
    Task<StatEventEntity> SaveRequestEvent(Guid? userId = null, long? sessionId = null, string? type = null, string? data = null, double? duration = null, CancellationToken ct = default);
}
