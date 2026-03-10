using Api.Extensions;
using Application.Services.User;
using FastEndpoints;
using Infrastructure.Db.App.Entities;
using Shared.Contracts;

namespace Api.Endpoints.User.GetMe;

/// <summary>
/// Эндпоинт для получения данных текущего пользователя
/// </summary>
sealed class GetMeEndpoint : EndpointWithoutRequest<Result<UserEntity>>
{
    private readonly IUserService _userService;

    /// <summary>
    /// Инициализирует новый экземпляр эндпоинта
    /// </summary>
    /// <param name="userService">Сервис для работы с пользователями</param>
    public GetMeEndpoint(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Конфигурирует маршрут и метаданные эндпоинта
    /// </summary>
    public override void Configure()
    {
        Get("me");
        Group<UserGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Получение данных текущего пользователя";
            s.Description = "Возвращает данные пользователя на основе JWT токена";
        });
    }

    /// <summary>
    /// Обрабатывает запрос на получение данных текущего пользователя
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var tokenData = HttpContext.TokenData();

        var user = await _userService.GetByIdAsync(tokenData.UserId);

        await SendAsync(new(user), cancellation: ct);
    }
}
