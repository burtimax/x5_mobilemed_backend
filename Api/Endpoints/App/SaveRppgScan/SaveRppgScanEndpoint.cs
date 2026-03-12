using System.Text.Json;
using Api.Extensions;
using Application.Models.RppgScan;
using Application.Services.RppgScan;
using FastEndpoints;
using Infrastructure.Db.App.Entities;
using Shared.Contracts;

namespace Api.Endpoints.App.SaveRppgScan;

/// <summary>
/// Endpoint для сохранения результата сканирования Rppg
/// </summary>
sealed class SaveRppgScanEndpoint : Endpoint<SaveRppgScanRequest, Result<UserRppgScanEntity>>
{
    private readonly IRppgScanService _rppgScanService;

    public SaveRppgScanEndpoint(IRppgScanService rppgScanService)
    {
        _rppgScanService = rppgScanService;
    }

    public override void Configure()
    {
        Post("rppg-scan");
        Group<AppGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Сохранение результата сканирования Rppg";
            s.Description = "Сохраняет результат сканирования от Binah SDK. Тело запроса — JSON-объект с полями takenAt, source, metrics, sdkRaw.";
        });
    }

    public override async Task HandleAsync(SaveRppgScanRequest req, CancellationToken ct)
    {
        var userId = HttpContext.TokenData().UserId;

        var scan = await _rppgScanService.SaveScanAsync(userId, req.ScanResult, ct);

        await SendAsync(Result.Success(scan), cancellation: ct);
    }
}

public class SaveRppgScanRequest
{
    public JsonElement ScanResult { get; set; }
}
