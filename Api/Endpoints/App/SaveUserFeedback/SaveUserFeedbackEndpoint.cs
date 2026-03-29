using System.Text.Json;
using Api.Extensions;
using Application.Services.UserFeedback;
using FastEndpoints;
using Infrastructure.Db.App.Entities;
using Shared.Contracts;

namespace Api.Endpoints.App.SaveUserFeedback;

sealed class SaveUserFeedbackEndpoint : Endpoint<SaveUserFeedbackRequest, Result<UserFeedbackEntity>>
{
    private readonly IUserFeedbackService _userFeedbackService;

    public SaveUserFeedbackEndpoint(IUserFeedbackService userFeedbackService)
    {
        _userFeedbackService = userFeedbackService;
    }

    public override void Configure()
    {
        Post("user-feedback");
        Group<AppGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Сохранение фидбека пользователя";
            s.Description = "Сохраняет JSON-фидбек текущего пользователя в базу данных.";
        });
    }

    public override async Task HandleAsync(SaveUserFeedbackRequest req, CancellationToken ct)
    {
        if (req.Feedback.ValueKind is JsonValueKind.Undefined)
        {
            await SendAsync(Result.Failure<UserFeedbackEntity>("Не указано поле feedback."), cancellation: ct);
            return;
        }

        var userId = HttpContext.TokenData().UserId;
        var json = req.Feedback.GetRawText();

        var entity = await _userFeedbackService.SaveAsync(userId, json, ct);
        await SendAsync(Result.Success(entity), cancellation: ct);
    }
}
