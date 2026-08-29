using Shuttlez.Domain.Common;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Domain.Entities;

/// <summary>
/// Route + vehicle published fare catalog and launch economics.
/// Catalog prices (round-trip / weekly / monthly) are Admin configuration —
/// seat bookings still snapshot one-way PricePerSeat on Trip/Booking.
/// </summary>
public class PricingRule : BaseEntity
{
    public string Name { get; set; } = "Default";

    /// <summary>Null = vehicle-type default (applies when no route-specific rule).</summary>
    public Guid? RouteId { get; set; }
    public Route? Route { get; set; }

    public VehicleType VehicleType { get; set; }

    public decimal OneWayPrice { get; set; }
    public decimal RoundTripPrice { get; set; }
    public decimal WeeklyPrice { get; set; }
    public decimal MonthlyPrice { get; set; }

    /// <summary>Platform % during launch window (typically 0).</summary>
    public decimal LaunchCommissionPercent { get; set; }

    /// <summary>Platform % after launch window (configurable; seed default 10).</summary>
    public decimal PermanentCommissionPercent { get; set; } = 10m;

    public int LaunchPeriodDays { get; set; } = 90;

    /// <summary>
    /// Anchor for the launch clock. If null, uses EffectiveFrom (or CreatedAt).
    /// </summary>
    public DateTime? LaunchStartAt { get; set; }

    public int MinimumLaunchRiders { get; set; } = 1;
    public int TargetOccupancy { get; set; } = 1;

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}
