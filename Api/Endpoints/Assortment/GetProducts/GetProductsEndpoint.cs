using Api.Extensions;
using Application.Services.Assortment;
using FastEndpoints;
using Infrastructure.Db.App.Entities;
using Infrastructure.Models;
using Shared.Contracts;

namespace Api.Endpoints.Assortment.GetProducts;

/// <summary>
/// Эндпоинт для получения товаров с фильтрацией по категориям.
/// </summary>
sealed class GetProductsEndpoint : Endpoint<GetProductsRequest, Result<PagedList<ProductEntity>>>
{
    private readonly IAssortmentService _service;

    public GetProductsEndpoint(IAssortmentService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Get("products");
        Group<AssortmentGroupEndpoints>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Получение товаров с фильтрацией";
            s.Description = "Возвращает товары с возможностью фильтрации по категориям.";
        });
    }

    public override async Task HandleAsync(GetProductsRequest req, CancellationToken ct)
    {
        var result = await _service.GetProductsAsync(
            req.CategoryIds,
            req.PageNumber,
            req.PageSize,
            ct);

        await SendAsync(new Result<PagedList<ProductEntity>>(result, true, null), cancellation: ct);
    }
}
