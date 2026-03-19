using Api.Extensions;
using Application.Services.UserExcludeProducts;
using FastEndpoints;
using Shared.Contracts;

namespace Api.Endpoints.User.GetUserExcludeProducts;

/// <summary>
/// Эндпоинт для получения всех продуктов-исключений текущего пользователя.
/// </summary>
sealed class GetUserExcludeProductsEndpoint : EndpointWithoutRequest<Result<IReadOnlyList<string>>>
{
    private readonly IUserExcludeProductsService _service;

    public GetUserExcludeProductsEndpoint(IUserExcludeProductsService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Get("get-for-user");
        Group<ExcludeProductsGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Получение продуктов-исключений пользователя";
            s.Description = "Возвращает список всех продуктов, добавленных пользователем в исключения";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var tokenData = HttpContext.TokenData();
        var products = await _service.GetUserExcludeProductsAsync(tokenData.UserId, ct);

        await SendAsync(new Result<IReadOnlyList<string>>(products, true, null), cancellation: ct);
    }
}
