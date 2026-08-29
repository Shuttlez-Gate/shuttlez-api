namespace Shuttlez.Application.Subscriptions.DTOs;

public record SubscriptionPackageDto(
    Guid Id,
    string Title,
    string DurationLabel,
    decimal Price,
    decimal OldPrice,
    int TripCount,
    int ValidityDays,
    bool IsCurrent,
    int? UsedTrips = null,
    int? RemainingTrips = null,
    DateTime? ExpiresAt = null);

public record SubscribePackageResponse(
    Guid PackageId,
    string Message,
    int TripCount = 0,
    int UsedTrips = 0,
    int? RemainingTrips = null,
    DateTime? ExpiresAt = null);

public record MySubscriptionDto(
    Guid? PackageId,
    string? Title,
    int TripCount,
    int UsedTrips,
    int? RemainingTrips,
    DateTime? ExpiresAt,
    bool IsActive);
