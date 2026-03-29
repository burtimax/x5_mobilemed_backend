using System.Text.Json;
using Api.Extensions;
using Application.Services.FrontendLog;
using FastEndpoints;
using Infrastructure.Db.App.Entities;
using Shared.Contracts;

namespace Api.Endpoints.App.SaveFrontendLog;

/// <summary>Сохранение критичного лога фронтенда (опционально с привязкой к пользователю по JWT).</summary>
sealed class SaveFrontendLogEndpoint : Endpoint<SaveFrontendLogRequest, Result<LogEntity>>
{
    private readonly IFrontendLogService _frontendLogService;

    public SaveFrontendLogEndpoint(IFrontendLogService frontendLogService)
    {
        _frontendLogService = frontendLogService;
    }

    public override void Configure()
    {
        Post("save-log");
        Group<AppGroupEndpoints>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Сохранение лога фронтенда";
            s.Description = "Публичный метод. При передаче валидного Bearer-токена в запись добавляется UserId. LogSource всегда frontend.";
        });
    }

    public override async Task HandleAsync(SaveFrontendLogRequest req, CancellationToken ct)
    {
        string? logJson = req.Log.ValueKind switch
        {
            JsonValueKind.Undefined or JsonValueKind.Null => null,
            _ => req.Log.GetRawText(),
        };

        var hasType = string.IsNullOrWhiteSpace(req.LogType) == false;
        var hasMessage = string.IsNullOrWhiteSpace(req.LogMessage) == false;
        if (!hasType && logJson == null && !hasMessage)
        {
            await SendAsync(
                Result.Failure<LogEntity>("Укажите хотя бы одно из полей: logType, log, logMessage."),
                cancellation: ct);
            return;
        }

        Guid? userId = HttpContext.TryGetUserId(out var uid) ? uid : null;

        var entity = await _frontendLogService.SaveAsync(
            userId,
            req.LogType,
            logJson,
            req.LogMessage,
            ct);

        await SendAsync(Result.Success(entity), cancellation: ct);
    }
}
