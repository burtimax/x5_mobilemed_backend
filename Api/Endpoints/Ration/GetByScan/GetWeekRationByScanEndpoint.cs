using Api.Endpoints.Ration;
using Api.Extensions;
using Application.Models.WeekRation;
using Application.Services.WeekRation;
using FastEndpoints;
using Shared.Contracts;

namespace Api.Endpoints.Ration.GetByScan;

/// <summary>Сохранённый недельный рацион по ИД скана RPPG.</summary>
public sealed class GetWeekRationByScanEndpoint : Endpoint<WeekRationByScanRouteRequest, Result<WeekRationResponseDto>>
{
    private readonly IWeekRationForScanService _rationForScan;

    public GetWeekRationByScanEndpoint(IWeekRationForScanService rationForScan)
    {
        _rationForScan = rationForScan;
    }

    public override void Configure()
    {
        Get("scan/{scanId}");
        Group<RationGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Рацион по скану";
            s.Description = "Возвращает последний сохранённый рацион для скана (слоты с продуктами и заменами из БД).";
        });
    }

    public override async Task HandleAsync(WeekRationByScanRouteRequest req, CancellationToken ct)
    {
        var userId = HttpContext.TokenData().UserId;
        var dto = await _rationForScan.GetStoredRationAsync(req.ScanId, userId, ct);
        if (dto == null || dto.Ration is not { Count: > 0 })
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(Result.Success(dto), cancellation: ct);
    }
}
