using Api.Extensions;
using Application.Services.Assortment;
using FastEndpoints;
using Infrastructure.Db.App.Entities;
using Shared.Contracts;

namespace Api.Endpoints.Assortment.GetCategories;

/// <summary>
/// Эндпоинт для получения всех категорий, для которых есть товары.
/// </summary>
sealed class GetCategoriesEndpoint : EndpointWithoutRequest<Result<List<CategoryEntity>>>
{
    private readonly IAssortmentService _service;

    public GetCategoriesEndpoint(IAssortmentService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Get("categories");
        Group<AssortmentGroupEndpoints>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Получение категорий с товарами";
            s.Description = "Возвращает все категории, для которых есть активные товары";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var categories = await _service.GetCategoriesWithProductsAsync(ct);
        await SendAsync(new Result<List<CategoryEntity>>(categories, true, null), cancellation: ct);
    }
}
