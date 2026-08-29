using Shuttlez.Domain.Common;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Domain.Entities;

/// Private-car Ride request. Separate from Shuttle Trip.
public class RideRequest : BaseEntity
{
    public Guid RiderUserId { get; set; }

    public double PickupLatitude { get; set; }
    public double PickupLongitude { get; set; }
    public string? PickupAddress { get; set; }

    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public string? DestinationAddress { get; set; }

    public string? FromZoneKey { get; set; }
    public string? ToZoneKey { get; set; }
    public Guid? RideFareRuleId { get; set; }

    public RideRequestStatus Status { get; set; } = RideRequestStatus.Requested;

    /// Snapshot at create — immutable thereafter.
    public decimal? DistanceKm { get; set; }
    public decimal? BaseFareApplied { get; set; }
    public decimal? PricePerKmApplied { get; set; }
    public decimal? MinimumFareApplied { get; set; }

    /// Snapshot at CASH confirmation — immutable thereafter.
    public decimal FareAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CaptainEarnings { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public bool IsCashConfirmed { get; set; }

    public Guid? DriverId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>Latest captain GPS while Assigned/InProgress — written only by assigned captain.</summary>
    public double? CaptainLatitude { get; set; }
    public double? CaptainLongitude { get; set; }
    public DateTime? CaptainLocationUpdatedAt { get; set; }

    public string? ReferenceCode { get; set; }

    public User Rider { get; set; } = null!;
    public Driver? Driver { get; set; }
    public RideFareRule? RideFareRule { get; set; }
}
