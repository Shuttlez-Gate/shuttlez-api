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

    public Driver? AssignedDriver { get; set; }
}
