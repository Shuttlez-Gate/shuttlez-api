using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

/// <summary>
/// Admin-managed operational status for an aggregated bidirectional route corridor.
/// Does not modify original passenger request records.
/// </summary>
public class RouteDemandGroupState : BaseEntity
{
    public string RouteKey { get; set; } = string.Empty;
    public string Status { get; set; } = "new_demand";
    public Guid? AssignedDriverId { get; set; }
    public string? AdminNotes { get; set; }

    /// <summary>
    /// Explicit Admin link from this demand corridor to a catalog <see cref="Route"/>.
    /// Takes priority over exact RouteKey matching. Never set by fuzzy matching.
    /// </summary>
    public Guid? MappedRouteId { get; set; }

    public DateTime? MappedAt { get; set; }
    public Guid? MappedByUserId { get; set; }

    public Driver? AssignedDriver { get; set; }
    public Route? MappedRoute { get; set; }
}
