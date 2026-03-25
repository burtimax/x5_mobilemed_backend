using Api.Endpoints.Ration;
using Api.Extensions;
using Application.Models.WeekRation;
using Application.Services.WeekRation;
using FastEndpoints;
using Shared.Contracts;

namespace Api.Endpoints.Ration.GetGenerationStatus;

/// <summary>Статус генерации недельного рациона по ИД скана RPPG.</summary>
public sealed class GetWeekRationGenerationStatusEndpoint
    : Endpoint<WeekRationByScanRouteRequest, Result<WeekRationGenerationStatusResponseDto>>
{
    private readonly IWeekRationForScanService _rationForScan;

    public GetWeekRationGenerationStatusEndpoint(IWeekRationForScanService rationForScan)
    {
        _rationForScan = rationForScan;
    }

    public override void Configure()
    {
        Get("scan/{scanId}/generation-status");
        Group<RationGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Статус генерации рациона по скану";
            s.Description = "Возвращает WeekRationGenerationStatus и StatusMessage для указанного скана.";
        });
    }

    public override async Task HandleAsync(WeekRationByScanRouteRequest req, CancellationToken ct)
    {
        var userId = HttpContext.TokenData().UserId;
        var dto = await _rationForScan.GetGenerationStatusAsync(req.ScanId, userId, ct);
        if (dto == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(Result.Success(dto), cancellation: ct);
    }
}
