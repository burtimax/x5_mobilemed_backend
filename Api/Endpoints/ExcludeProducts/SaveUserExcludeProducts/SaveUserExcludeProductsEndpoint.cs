using Api.Extensions;
using Application.Models.UserExcludeProducts;
using Application.Services.UserExcludeProducts;
using FastEndpoints;
using Shared.Contracts;

namespace Api.Endpoints.User.AddUserExcludeProducts;

/// <summary>
/// Эндпоинт для добавления пачки продуктов в исключения пользователя.
/// </summary>
sealed class SaveUserExcludeProductsEndpoint : Endpoint<AddUserExcludeProductsRequest, Result<AddUserExcludeProductsResponse>>
{
    private readonly IUserExcludeProductsService _service;

    public SaveUserExcludeProductsEndpoint(IUserExcludeProductsService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Post("save-for-user");
        Group<ExcludeProductsGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Сохранение продуктов в исключения";
            s.Description = "Сохраняет пачку продуктов в исключения текущего пользователя";
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
