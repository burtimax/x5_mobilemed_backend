using Api.Endpoints.Ration;
using Api.Extensions;
using Application.Models.WeekRation;
using Application.Services.WeekRation;
using FastEndpoints;
using Infrastructure.Db.App.Entities;

namespace Api.Endpoints.Ration.GetById;

/// <summary>Сохранённый недельный рацион по ИД записи <c>WeekRation</c>.</summary>
public sealed class GetWeekRationByIdEndpoint : Endpoint<WeekRationByIdRouteRequest, WeekRationEntity>
{
    private readonly IWeekRationForScanService _rationForScan;

    public GetWeekRationByIdEndpoint(IWeekRationForScanService rationForScan)
    {
        _rationForScan = rationForScan;
    }

    public override void Configure()
    {
        Get("{rationId}");
        Group<RationGroupEndpoints>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Публичный метод. Рацион по ИД";
            s.Description = "Возвращает рацион по идентификатору сохранённой записи (если она принадлежит текущему пользователю).";
        });
    }

    public override async Task HandleAsync(WeekRationByIdRouteRequest req, CancellationToken ct)
    {
        //var userId = HttpContext.TokenData().UserId;
        var ration = await _rationForScan.GetStoredRationByIdAsync(req.RationId, ct);
        if (ration == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(ration, cancellation: ct);
    }
}
