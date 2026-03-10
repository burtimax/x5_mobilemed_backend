

using Api.Endpoints.App.SaveStatEvent;
using Api.Extensions;
using Application.Services.StatEvent;
using Shared.Const;
using Shared.Models;

namespace Api.Middleware;

public class StatRequestMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<StatRequestMiddleware> _logger;

    public StatRequestMiddleware(RequestDelegate next, ILogger<StatRequestMiddleware> logger)
    {
        this.next = next;
        this._logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IStatEventService statEventService)
    {
        Duration sec = new();
        await next.Invoke(context);

        if (context.TryGetTokenData(out var tokenData))
        {
            Guid userId = tokenData.UserId;
            long sessionId = tokenData.SessionId;
            string? utm = tokenData.Utm;
            string data = context.Request.Path;
            await statEventService.SaveRequestEvent(userId: userId, sessionId: sessionId, type: AppConstants.StatEvents.ApiRequestAuthorized, data: data, duration: sec.GetSeconds());
            _logger.LogInformation($"Запрос: {context.Request.Path} [{userId.ToString()}]");
        }
        else
        {
            string data = context.Request.Path;
            await statEventService.SaveRequestEvent(type: AppConstants.StatEvents.ApiRequestUnauthorized, data: data, duration: sec.GetSeconds());
            _logger.LogInformation($"Запрос: {context.Request.Path}");
        }
    }
}
