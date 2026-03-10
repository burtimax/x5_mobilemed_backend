using FastEndpoints;

namespace Api.Endpoints.User;

public sealed class UserGroupEndpoints : Group
{
    public UserGroupEndpoints()
    {
        Configure("user", c =>
        {
            //c.Description(d => d.WithTags("application"));
        });
    }
}