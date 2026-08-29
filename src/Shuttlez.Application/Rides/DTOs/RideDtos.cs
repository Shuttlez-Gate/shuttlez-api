namespace Shuttlez.Application.Rides.DTOs;

public record CreateRideRequest(
    double PickupLatitude,
    double PickupLongitude,
    double DestinationLatitude,
    double DestinationLongitude,
    string? PickupAddress = null,
    string? DestinationAddress = null,
    string? FromZoneKey = null,
    string? ToZoneKey = null,
    string? PaymentMethod = null);

public record RideQuoteDto(
    Guid RideFareRuleId,
    string RuleName,
    string? FromZoneKey,
    string? ToZoneKey,
    decimal FareAmount,
    decimal PlatformCommissionPercent,
    decimal CommissionAmount,
    decimal CaptainEarnings,
    decimal TotalAmount,
    string PaymentMethodHint,
    decimal? DistanceKm = null,
    decimal? BaseFare = null,
    decimal? PricePerKm = null,
    decimal? MinimumFare = null);

public record RideFareOptionDto(
    Guid Id,
    string Name,
    string? FromZoneKey,
    string? ToZoneKey,
    decimal FlatFare,
    decimal BaseFare,
    decimal PricePerKm,
    decimal? MinimumFare);

public record RideDto(
    Guid Id,
    Guid RiderUserId,
    double PickupLatitude,
    double PickupLongitude,
    string? PickupAddress,
    double DestinationLatitude,
    double DestinationLongitude,
    string? DestinationAddress,
    string? FromZoneKey,
    string? ToZoneKey,
    Guid? RideFareRuleId,
    string Status,
    decimal FareAmount,
    decimal CommissionRate,
    decimal CommissionAmount,
    decimal CaptainEarnings,
    decimal TotalAmount,
    string PaymentMethod,
    bool IsCashConfirmed,
    Guid? DriverId,
    string? DriverName,
    DateTime? AssignedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    string? ReferenceCode,
    DateTime CreatedAt,
    /// <summary>Captain user AvatarUrl when present — never invented.</summary>
    string? DriverPhotoUrl = null,
    /// <summary>Driver.RatingAverage only when RatingCount &gt; 0.</summary>
    decimal? DriverRatingAverage = null,
    /// <summary>Driver.RatingCount only when &gt; 0.</summary>
    int? DriverRatingCount = null,
    /// <summary>Driver.VehicleKind or linked Vehicle.Type.</summary>
    string? VehicleKind = null,
    /// <summary>Driver.VehicleModelName or linked Vehicle.Model.</summary>
    string? VehicleModel = null,
    /// <summary>Driver.VehicleColor when present.</summary>
    string? VehicleColor = null,
    /// <summary>Driver.PlateNumber or linked Vehicle.PlateNumber.</summary>
    string? PlateNumber = null,
    string? DriverPhone = null,
    decimal? DistanceKm = null,
    decimal? BaseFareApplied = null,
    decimal? PricePerKmApplied = null,
    /// <summary>Captain GPS while Assigned/InProgress only — null otherwise.</summary>
    double? CaptainLatitude = null,
    double? CaptainLongitude = null,
    DateTime? CaptainLocationUpdatedAt = null);

public record UpdateCaptainRideLocationRequest(double Latitude, double Longitude);

public record DriverRideLocationDto(
    Guid RideId,
    double Latitude,
    double Longitude,
    DateTime UpdatedAt,
    string Status);

public record DriverRideLifecycleDto(
    Guid RideId,
    Guid? DriverId,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? UpdatedAt,
    string Message,
    bool Idempotent = false);

public record AssignRideDriverRequest(Guid DriverId);
