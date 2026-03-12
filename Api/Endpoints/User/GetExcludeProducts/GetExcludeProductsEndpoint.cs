using Api.Extensions;
using Application.Models.UserExcludeProducts;
using Application.Services.UserExcludeProducts;
using FastEndpoints;
using Infrastructure.Db.App.Entities;
using Infrastructure.Models;
using Shared.Contracts;

namespace Api.Endpoints.User.GetExcludeProducts;

/// <summary>
/// Эндпоинт для получения списка продуктов-исключений из справочника с поиском по названию.
/// </summary>
sealed class GetExcludeProductsEndpoint : Endpoint<GetExcludeProductsRequest, Result<PagedList<ExcludeProductEntity>>>
{
    private readonly IUserExcludeProductsService _service;

    public GetExcludeProductsEndpoint(IUserExcludeProductsService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Get("exclude-products");
        Group<UserGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Получение списка продуктов-исключений";
            s.Description = "Возвращает список продуктов из справочника с возможностью поиска по названию";
        });
    }

    public override async Task HandleAsync(GetExcludeProductsRequest req, CancellationToken ct)
    {
        _ = HttpContext.TokenData(); // Требует аутентификации
        var result = await _service.GetExcludeProductsAsync(
            req.Search,
            req.PageNumber,
            req.PageSize,
            ct);

        await SendAsync(new Result<PagedList<ExcludeProductEntity>>(result, true, null), cancellation: ct);
    }
}
