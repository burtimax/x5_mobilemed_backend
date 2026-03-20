using Api.Extensions;
using Application.Models.UserExcludeProducts;
using Application.Services.UserExcludeProducts;
using FastEndpoints;
using Shared.Contracts;

namespace Api.Endpoints.User.AddUserExcludeProducts;

/// <summary>
/// Эндпоинт для полного сохранения списка продуктов-исключений пользователя.
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
            s.Description = "Полностью перезаписывает список исключений текущего пользователя; пустой список очищает исключения";
        });
    }

    public override async Task HandleAsync(AddUserExcludeProductsRequest req, CancellationToken ct)
    {
        var tokenData = HttpContext.TokenData();
        var addedCount = await _service.SaveUserExcludeProductsAsync(
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
