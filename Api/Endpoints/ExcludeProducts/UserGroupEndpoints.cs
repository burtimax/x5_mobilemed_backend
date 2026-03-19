using FastEndpoints;

namespace Api.Endpoints.User;

public sealed class ExcludeProductsGroupEndpoints : Group
{
    public ExcludeProductsGroupEndpoints()
    {
        Configure("exclude-products", c =>
        {
            //c.Description(d => d.WithTags("application"));
        });
    }
}
