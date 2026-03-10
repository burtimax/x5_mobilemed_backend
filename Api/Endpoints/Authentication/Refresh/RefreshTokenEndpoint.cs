using Api.Extensions;
using Application.Models.Auth;
using Application.Services.User;
using FastEndpoints;

namespace Api.Endpoints.Authentication.Refresh;

/// <summary>
/// Эндпоинт для обновления (refresh) JWT токена
/// </summary>
public sealed class RefreshTokenEndpoint : EndpointWithoutRequest<LoginResponse>
{
    private readonly IUserService _userService;

    public RefreshTokenEndpoint(IUserService userService)
    {
        _userService = userService;
    }

    public override void Configure()
    {
        Post("refresh-token");
        // Обновление токена доступно только аутентифицированным пользователям
        Group<AuthGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Обновление JWT токена";
            s.Description = "Обновляет JWT токен на основе текущего аутентифицированного пользователя.";
        });
    }

    public override async Task HandleAsync(CancellationToken c)
    {
        var tokenData = HttpContext.TokenData();

        var result = await _userService.RefreshTokenAsync(tokenData.UserId, c);

        await SendAsync(result, cancellation: c);
    }
}

