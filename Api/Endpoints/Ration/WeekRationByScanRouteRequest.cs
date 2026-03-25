using FastEndpoints;

namespace Api.Endpoints.Ration;

public sealed class WeekRationByScanRouteRequest
{
    public Guid ScanId { get; set; }
}
