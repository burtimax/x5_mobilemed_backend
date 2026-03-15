using FastEndpoints;

namespace Api.Endpoints.Assortment;

public sealed class AssortmentGroupEndpoints : Group
{
    public AssortmentGroupEndpoints()
    {
        Configure("assortment", c =>
        {
            //c.Description(d => d.WithTags("application"));
        });
    }
}
