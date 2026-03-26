using Api.Extensions;
using Application.Models.WeekRation;
using Application.Services.WeekRation;
using FastEndpoints;
using Infrastructure.Db.App.Entities;
using Shared.Contracts;

namespace Api.Endpoints.Ration.ReplaceItem;

/// <summary>Замена товара в позиции рациона с переносом прежнего основного в список замен.</summary>
public sealed class ReplaceWeekRationItemEndpoint : Endpoint<ReplaceWeekRationItemRequest, Result<WeekRationEntity>>
{
    private readonly IWeekRationForScanService _rationForScan;

    public ReplaceWeekRationItemEndpoint(IWeekRationForScanService rationForScan)
    {
        _rationForScan = rationForScan;
    }

    public override void Configure()
    {
        Post("item/replace");
        Group<RationGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Заменить товар в позиции рациона";
            s.Description =
                "Принимает Id позиции, новый ProductId и вес. Прежний основной товар добавляется в замены, если записи с таким ProductId ещё нет. "
                + "Если новый товар выбран из существующих замен, соответствующие строки замен удаляются; остальные замены сохраняются.";
        });
    }

    public override async Task HandleAsync(ReplaceWeekRationItemRequest req, CancellationToken ct)
    {
        var userId = HttpContext.TokenData().UserId;
        var ration = await _rationForScan.ReplaceWeekRationItemAsync(req.Id, req.ProductId, req.Weigth, userId, ct);
        if (ration == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(Result.Success(ration), cancellation: ct);
    }
}
