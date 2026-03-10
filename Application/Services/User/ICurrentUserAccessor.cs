namespace Application.Services.User;

public interface ICurrentUserAccessor
{
    Guid? GetCurrentUserId();
}
