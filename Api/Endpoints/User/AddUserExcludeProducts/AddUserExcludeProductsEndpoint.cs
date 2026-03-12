using Api.Extensions;
using Application.Models.UserExcludeProducts;
using Application.Services.UserExcludeProducts;
using FastEndpoints;
using Shared.Contracts;

namespace Api.Endpoints.User.AddUserExcludeProducts;

/// <summary>
/// Эндпоинт для добавления пачки продуктов в исключения пользователя.
/// </summary>
sealed class AddUserExcludeProductsEndpoint : Endpoint<AddUserExcludeProductsRequest, Result<AddUserExcludeProductsResponse>>
{
    private readonly IUserExcludeProductsService _service;

    public AddUserExcludeProductsEndpoint(IUserExcludeProductsService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Post("exclude-products");
        Group<UserGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Добавление продуктов в исключения";
            s.Description = "Добавляет пачку продуктов в исключения текущего пользователя";
        });
    }

    public override async Task HandleAsync(AddUserExcludeProductsRequest req, CancellationToken ct)
    {
        var tokenData = HttpContext.TokenData();
        var addedCount = await _service.AddUserExcludeProductsAsync(
            tokenData.UserId,
            req.Products,
            ct);

        await SendAsync(
            new Result<AddUserExcludeProductsResponse>(
                new AddUserExcludeProductsResponse { AddedCount = addedCount },
                true,
                null),
            cancellation: ct);
    }
}
