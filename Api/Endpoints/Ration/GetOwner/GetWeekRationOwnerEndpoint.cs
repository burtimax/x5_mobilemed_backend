using Api.Endpoints.Ration;
using Application.Models.WeekRation;
using Application.Services.WeekRation;
using FastEndpoints;
using Shared.Contracts;

namespace Api.Endpoints.Ration.GetOwner;

/// <summary>Пользователь по скану рациона и список исключённых продуктов. Публичный метод по ИД рациона.</summary>
public sealed class GetWeekRationOwnerEndpoint : Endpoint<WeekRationByIdRouteRequest, Result<WeekRationOwnerResponse>>
{
    private readonly IWeekRationForScanService _rationForScan;

    public GetWeekRationOwnerEndpoint(IWeekRationForScanService rationForScan)
    {
        _rationForScan = rationForScan;
    }

    public override void Configure()
    {
        Get("{rationId}/owner");
        Group<RationGroupEndpoints>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Публичный метод. Пользователь и исключения по ИД рациона";
            s.Description =
                "Находит рацион, по нему — скан RPPG, по скану — пользователя; возвращает UserEntity с профилем и список user_exclude_products.";
        });
    }

    public override async Task HandleAsync(WeekRationByIdRouteRequest req, CancellationToken ct)
    {
        var data = await _rationForScan.GetRationOwnerWithExcludeProductsAsync(req.RationId, ct);
        if (data == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(Result.Success(data), cancellation: ct);
    }
}
