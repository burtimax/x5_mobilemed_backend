using Api.Endpoints.Ration;
using Api.Extensions;
using Application.Services.WeekRation;
using FastEndpoints;
using Shared.Contracts;

namespace Api.Endpoints.Ration.Regenerate;

/// <summary>Повторная постановка генерации рациона в очередь.</summary>
public sealed class RequestWeekRationRegenerationEndpoint : Endpoint<WeekRationRegenerateRequest, Result<string>>
{
    private readonly IWeekRationForScanService _rationForScan;

    public RequestWeekRationRegenerationEndpoint(IWeekRationForScanService rationForScan)
    {
        _rationForScan = rationForScan;
    }

    public override void Configure()
    {
        Post("scan/regenerate");
        Group<RationGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Перегенерировать рацион";
            s.Description = "Устанавливает WeekRationGenerationStatus = Pending; фоновая задача сгенерирует рацион заново.";
        });
    }

    public override async Task HandleAsync(WeekRationRegenerateRequest req, CancellationToken ct)
    {
        var userId = HttpContext.TokenData().UserId;
        var ok = await _rationForScan.TryQueueRegenerationAsync(req.ScanId, userId, ct);
        if (!ok)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(Result.Success("Успешно"));
    }
}
