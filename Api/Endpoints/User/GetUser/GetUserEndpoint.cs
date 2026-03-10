using Api.Extensions;
using Application.Models.User;
using Application.Services.User;
using FastEndpoints;
using Infrastructure.Db.App.Entities;
using Infrastructure.Models;
using Shared.Contracts;
using Shared.Models;

namespace Api.Endpoints.User.GetUser;

sealed class GetUserEndpoint : Endpoint<GetUserRequest, Result<PagedList<UserEntity>>>
{
    private readonly IUserService _userService;
    public GetUserEndpoint(IUserService userService)
    {
        _userService = userService;
    }
    public override void Configure()
    {
        Post("get");
        Group<UserGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Получение данных пользователя";
            s.Description = $"";
        });
    }

    public override async Task HandleAsync(GetUserRequest r, CancellationToken c)
    {
        var tokenData = HttpContext.TokenData();
        var result = await _userService.GetAsync(r, c);

        await SendAsync(new (result, true, null), cancellation: c);
    }
}
