using Api.Endpoints.App;
using Application.Services.RppgScan;
using FastEndpoints;

namespace Api.Endpoints.Scan.GetRppgScanReportText;

/// <summary>
/// Тестовый эндпоинт: текстовый отчёт по сохранённому скану RPPG (норма / погранично / вне нормы).
/// </summary>
sealed class GetRppgScanReportTextEndpoint : Endpoint<GetRppgScanReportTextRequest>
{
    private readonly IRppgScanReportService _reportService;

    public GetRppgScanReportTextEndpoint(IRppgScanReportService reportService)
    {
        _reportService = reportService;
    }

    public override void Configure()
    {
        Get("rppg-scan/{scanId}/report-text");
        AllowAnonymous();
        Group<ScanGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Тест: текстовый отчёт по скану RPPG";
            s.Description = "По ИД скана возвращает plain text с расшифровкой показателей и зон (норма, погранично, выше/ниже нормы).";
        });
    }

    public override async Task HandleAsync(GetRppgScanReportTextRequest req, CancellationToken ct)
    {
        var report = await _reportService.GetReportTextAsync(req.ScanId, ct);
        if (report == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendStringAsync(report, contentType: "text/plain; charset=utf-8", cancellation: ct);
    }
}

public sealed class GetRppgScanReportTextRequest
{
    public Guid ScanId { get; set; }
}
