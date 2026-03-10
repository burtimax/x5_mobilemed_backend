using Api.Extensions;
using Application.Services.StatEvent;
using FastEndpoints;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Shared.Contracts;

namespace Api.Endpoints.App.SaveStatEvent;

/// <summary>
/// Endpoint для сохранения статистического события
/// </summary>
sealed class SaveStatEventEndpoint : Endpoint<SaveStatEventRequest, Result<StatEventEntity>>
{
    private readonly IStatEventService _statEventService;

    public SaveStatEventEndpoint(IStatEventService statEventService)
    {
        _statEventService = statEventService;
    }

    public override void Configure()
    {
        Post("stat-event");
        Group<AppGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Сохранение события статистики";
            s.Description = "Сохраняет событие использования приложения для аналитики";
        });
    }

    public override async Task HandleAsync(SaveStatEventRequest req, CancellationToken ct)
    {
        var sessionId = HttpContext.TokenData().SessionId;
        var utm = HttpContext.TokenData().Utm;
        var userId = HttpContext.TokenData().UserId;

        var statEvent = await _statEventService.SaveRequestEvent(userId: userId, sessionId: sessionId, type:req.Type, data: req.Data, duration: req.DurationSeconds, ct);

        await SendAsync(new Result<StatEventEntity>(statEvent), cancellation: ct);
    }
}
