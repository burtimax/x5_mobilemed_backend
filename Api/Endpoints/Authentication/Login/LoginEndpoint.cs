using Application.Models.Auth;
using Application.Services.User;
using FastEndpoints;

namespace Api.Endpoints.Authentication.Login;

sealed public class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    private readonly IUserService _userService;

    public LoginEndpoint(IUserService userService)
    {
        _userService = userService;
    }

    public override void Configure()
    {
        Post("login");
        AllowAnonymous();
        Group<AuthGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Авторизация пользователя по email и паролю";
            s.Description = "Авторизует пользователя по email и паролю. Возвращает JWT токен для дальнейшей работы с API.";
        });
    }

    public override async Task HandleAsync(LoginRequest r, CancellationToken c)
    {
        var result = await _userService.LoginAsync(r.Id, c);

        await SendAsync(result, cancellation: c);
    }
}

