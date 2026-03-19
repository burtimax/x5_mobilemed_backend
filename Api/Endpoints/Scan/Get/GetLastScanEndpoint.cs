using Api.Endpoints.App;
using Api.Extensions;
using Application.Models.RppgScan;
using Application.Services.RppgScan;
using FastEndpoints;
using Infrastructure.Models;
using Shared.Contracts;

namespace Api.Endpoints.Scan.GetLastScan;

/// <summary>
/// Endpoint для получения истории сканов пользователя с пагинацией.
/// </summary>
sealed class GetScansEndpoint : Endpoint<GetScansHistoryRequest, Result<PagedList<SaveRppgSсanResponse>>>
{
    private readonly IRppgScanService _rppgScanService;

    public GetScansEndpoint(IRppgScanService rppgScanService)
    {
        _rppgScanService = rppgScanService;
    }

    public override void Configure()
    {
        Get("get");
        Group<ScanGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Получение истории сканов пользователя";
            s.Description = "Возвращает пагинированный список сканов с расшифровкой Transcripts и HealthScore.";
        });
    }

    public override async Task HandleAsync(GetScansHistoryRequest req, CancellationToken ct)
    {
        var userId = HttpContext.TokenData().UserId;

        var result = await _rppgScanService.GetScansHistoryAsync(
            userId,
            req.PageNumber,
            req.PageSize,
            ct);

        await SendAsync(new Result<PagedList<SaveRppgSсanResponse>>(result, true, null), cancellation: ct);
    }
}
