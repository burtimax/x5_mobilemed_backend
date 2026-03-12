using System.Text;
using Api.Extensions;
using Application.Services.StatEvent;
using FastEndpoints;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Shared.Contracts;

namespace Api.Endpoints.App.SaveStatEvent;

/// <summary>
/// Endpoint для отправки ошибки с фронта.
/// </summary>
sealed class SendErrorEndpoint : Endpoint<SendErrorRequest>
{
    private readonly ILogger<SendErrorEndpoint> _logger;

    public SendErrorEndpoint(ILogger<SendErrorEndpoint> logger)
    {
        _logger = logger;
    }

    public override void Configure()
    {
        Post("send-error");
        Group<AppGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Отправка ошибки";
            s.Description = "Отправка ошибки";
        });
    }

    public override async Task HandleAsync(SendErrorRequest req, CancellationToken ct)
    {
        var sessionId = HttpContext.TokenData().SessionId;
        var utm = HttpContext.TokenData().Utm;
        var userId = HttpContext.TokenData().UserId;

        StringBuilder sb = new();
        sb.AppendLine("FRONTEND ERROR");
        sb.AppendLine("User Id: " + userId);
        sb.AppendLine("Session Id: " + sessionId);
        if(string.IsNullOrEmpty(req.ErrorMessage) == false)
            sb.AppendLine("Error Message: " + req.ErrorMessage);
        if(string.IsNullOrEmpty(req.Data) == false)
            sb.AppendLine("Error Data: " + req.Data);
        if(string.IsNullOrEmpty(req.StackTrace) == false)
            sb.AppendLine("Error Stack Trace: " + req.StackTrace);

        _logger.LogError(sb.ToString());
        await SendOkAsync();
    }
}
