using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class LandingWaitlistEntry : BaseEntity
{
    public string Phone { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public Guid? RouteId { get; set; }
    public string? RouteFrom { get; set; }
    public string? RouteTo { get; set; }
    public string Source { get; set; } = "landing";

    public Route? Route { get; set; }
}
