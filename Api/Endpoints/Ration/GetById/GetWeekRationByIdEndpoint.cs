using Api.Endpoints.Ration;
using Api.Extensions;
using Application.Models.WeekRation;
using Application.Services.WeekRation;
using FastEndpoints;

namespace Api.Endpoints.Ration.GetById;

/// <summary>Сохранённый недельный рацион по ИД записи <c>WeekRation</c>.</summary>
public sealed class GetWeekRationByIdEndpoint : Endpoint<WeekRationByIdRouteRequest, WeekRationResponseDto>
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
        Summary(s =>
        {
            s.Summary = "Рацион по ИД";
            s.Description = "Возвращает рацион по идентификатору сохранённой записи (если она принадлежит текущему пользователю).";
        });
    }

    public override async Task HandleAsync(WeekRationByIdRouteRequest req, CancellationToken ct)
    {
        var userId = HttpContext.TokenData().UserId;
        var dto = await _rationForScan.GetStoredRationByIdAsync(req.RationId, userId, ct);
        if (dto == null || dto.Ration is not { Count: > 0 })
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(dto, cancellation: ct);
    }
}
