using System.Text.Json;
using Api.Extensions;
using Application.Models.RppgScan;
using Application.Services.RppgScan;
using FastEndpoints;
using Shared.Contracts;

namespace Api.Endpoints.App.SaveRppgScan;

/// <summary>
/// Endpoint для сохранения результата сканирования Rppg
/// </summary>
sealed class SaveRppgScanEndpoint : Endpoint<SaveRppgScanRequest, Result<SaveRppgSсanResponse>>
{
    private readonly IRppgScanService _rppgScanService;

    public SaveRppgScanEndpoint(IRppgScanService rppgScanService)
    {
        _rppgScanService = rppgScanService;
    }

    public override void Configure()
    {
        Post("save-rppg");
        Group<ScanGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Сохранение результата сканирования Rppg";
            s.Description = "Сохраняет результат сканирования от Binah SDK и возвращает с расшифровкой Transcripts. Тело запроса — JSON-объект с полями takenAt, source, metrics, sdkRaw.";
        });
    }

    public override async Task HandleAsync(SaveRppgScanRequest req, CancellationToken ct)
    {
        var userId = HttpContext.TokenData().UserId;

        var response = await _rppgScanService.SaveScanAsync(userId, req.ScanResult, ct);

        await SendAsync(Result.Success(response), cancellation: ct);
    }
}

public class SaveRppgScanRequest
{
    public JsonElement ScanResult { get; set; }
}
