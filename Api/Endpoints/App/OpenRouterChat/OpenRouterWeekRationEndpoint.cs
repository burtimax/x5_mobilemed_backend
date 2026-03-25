using Api.Extensions;
using Application.Models.WeekRation;
using Application.Services.WeekRation;
using FastEndpoints;

namespace Api.Endpoints.App.OpenRouterChat;

/// <summary>
/// Недельный рацион по результатам RPPG-скана: отчёт скана, каталог X5, исключения пользователя, профиль (калораж).
/// </summary>
public sealed class OpenRouterWeekRationEndpoint : Endpoint<WeekRationRequest, WeekRationResponseDto>
{
    private readonly IWeekRationGeneratorService _weekRationGenerator;

    public OpenRouterWeekRationEndpoint(IWeekRationGeneratorService weekRationGenerator)
    {
        _weekRationGenerator = weekRationGenerator;
    }

    public override void Configure()
    {
        Post("openrouter/week-ration");
        Group<AppGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Рацион на неделю по скану RPPG (OpenRouter)";
            s.Description =
                "По ID скана строит текст отчёта, подмешивает каталог товаров X5 и исключения пользователя; возвращает JSON рациона на 7 дней.";
        });
    }

    public override async Task HandleAsync(WeekRationRequest req, CancellationToken ct)
    {
        var userId = HttpContext.TokenData().UserId;
        var outcome = await _weekRationGenerator.GenerateAsync(req, userId, ct);
        await SendAsync(outcome.Response, statusCode: outcome.StatusCode, cancellation: ct);
    }
}
