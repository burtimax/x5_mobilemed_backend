using Api.Extensions;
using Microsoft.AspNetCore.Http;

namespace Application.Services.User;

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetCurrentUserId()
    {
        if (_httpContextAccessor.HttpContext?.TryGetTokenData(out var data) == true)
            return data.UserId;
        return null;
    }
}
