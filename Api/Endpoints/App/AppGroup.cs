using FastEndpoints;

namespace Api.Endpoints.App;

public sealed class AppGroupEndpoints : Group
{
    public AppGroupEndpoints()
    {
        Configure("app", c =>
        {
            //c.Description(d => d.WithTags("application"));
        });
    }
}