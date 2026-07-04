namespace Shuttlez.Application.Subscriptions.DTOs;

public record SubscriptionPackageDto(
    Guid Id,
    string Title,
    string DurationLabel,
    decimal Price,
    decimal OldPrice,
    int TripCount,
    int ValidityDays);

public record SubscribePackageResponse(
    Guid PackageId,
    string Message);
