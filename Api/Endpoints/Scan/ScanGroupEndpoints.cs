using FastEndpoints;

namespace Api.Endpoints.App;

public sealed class ScanGroupEndpoints : Group
{
    public ScanGroupEndpoints()
    {
        Configure("scan", c =>
        {
            //c.Description(d => d.WithTags("application"));
        });
    }
}
