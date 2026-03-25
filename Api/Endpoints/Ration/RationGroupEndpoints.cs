using FastEndpoints;

namespace Api.Endpoints.Ration;

public sealed class RationGroupEndpoints : Group
{
    public RationGroupEndpoints()
    {
        Configure("ration", _ => { });
    }
}
