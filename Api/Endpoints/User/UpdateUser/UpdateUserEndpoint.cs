using Api.Extensions;
using Application.Models.User;
using Application.Services.User;
using FastEndpoints;
using Infrastructure.Db.App.Entities;
using Shared.Const;
using Shared.Contracts;
using IMapper = MapsterMapper.IMapper;

namespace Api.Endpoints.User.UpdateUser;

sealed class UpdateUserEntpoint : Endpoint<UpdateUserRequest, Result<UserEntity>>
{
    private readonly IUserService _userService;
    private readonly IMapper _mapper;

    public UpdateUserEntpoint(IUserService userService, IMapper mapper)
    {
        _userService = userService;
        _mapper = mapper;
    }

    public override void Configure()
    {
        Put("update");
        Group<UserGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Редактирование пользователя";
            s.Description = $"";
        });
    }

    public override async Task HandleAsync(UpdateUserRequest r, CancellationToken c)
    {
        var tokenData = HttpContext.TokenData();

        var result = await _userService.UpdateAsync(tokenData.UserId, r, c);

        await SendAsync(new(result), cancellation: c);
    }
}
