using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

/// Admin-configurable zone/flat Ride fare catalog. No distance math.
public class RideFareRule : BaseEntity
{
    public string Name { get; set; } = "Default";

    /// Null/empty From+To = city-wide flat fare for MVP matching.
    public string? FromZoneKey { get; set; }
    public string? ToZoneKey { get; set; }

    public decimal FlatFare { get; set; }

    public decimal BaseFare { get; set; }
    public decimal PricePerKm { get; set; }
    public decimal? MinimumFare { get; set; }
    public decimal? MaximumFare { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}
