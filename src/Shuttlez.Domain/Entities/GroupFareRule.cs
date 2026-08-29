using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

/// Admin-configurable Group/charter flat fare catalog. No automatic discount.
public class GroupFareRule : BaseEntity
{
    public string Name { get; set; } = "Default";

    public string? FromZoneKey { get; set; }
    public string? ToZoneKey { get; set; }

    /// Authoritative charter total (organizer pays once).
    public decimal CharterFlatFare { get; set; }

    public decimal BaseFare { get; set; }
    public decimal PricePerKm { get; set; }
    public decimal? MinimumFare { get; set; }
    public decimal? MaximumFare { get; set; }

    /// Server-side max members/passengers for groups using this rule.
    public int MaxMembers { get; set; } = 1;

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}
