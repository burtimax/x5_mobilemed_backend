using Api.Extensions;
using Application.Services.Assortment;
using FastEndpoints;
using Shared.Contracts;

namespace Api.Endpoints.Assortment.SetProductActive;

/// <summary>
/// Эндпоинт для включения/отключения активности товара.
/// </summary>
sealed class SetProductActiveEndpoint : Endpoint<SetProductActiveRequest, Result<bool>>
{
    private readonly IAssortmentService _service;

    public SetProductActiveEndpoint(IAssortmentService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Patch("products/{id:long}/active");
        Group<AssortmentGroupEndpoints>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Изменение активности товара";
            s.Description = "Включает или отключает активность товара (IsActive)";
        });
    }

    public override async Task HandleAsync(SetProductActiveRequest req, CancellationToken ct)
    {
        var updated = await _service.SetProductActiveAsync(req.Id, req.IsActive, ct);

        if (!updated)
        {
            await SendAsync(
                new Result<bool>(false, false, "Товар не найден"),
                cancellation: ct);
            return;
        }

        await SendAsync(new Result<bool>(true, true, null), cancellation: ct);
    }
}
