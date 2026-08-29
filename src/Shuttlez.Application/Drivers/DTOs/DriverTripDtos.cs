namespace Shuttlez.Application.Drivers.DTOs;

public record DriverTripCardDto(
    Guid Id,
    string DateLabel,
    string TimeLabel,
    string VehicleType,
    string From,
    string To,
    int Passengers,
    int Stations,
    double Earnings,
    string Status,
    DateTime ScheduledAt,
    string? RouteId = null,
    string? VehicleAsset = null,
    /// <summary>Raw TripStatus enum name (Scheduled, DriverAssigned, InProgress, …).</summary>
    string? TripStatus = null,
    decimal? PricePerSeat = null,
    int? AvailableSeats = null);

public record DriverTripListResponseDto(
    DriverTripCardDto? CurrentTrip,
    IReadOnlyList<DriverTripCardDto> UpcomingTrips,
    IReadOnlyList<DriverTripCardDto> HistoryTrips);

/// <summary>Start/Complete response — backend is source of truth for status timestamps.</summary>
public record DriverTripLifecycleDto(
    Guid TripId,
    Guid RouteId,
    Guid? DriverId,
    string TripStatus,
    string UiStatus,
    DateTime ScheduledAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    decimal PricePerSeat,
    int AvailableSeats,
    DateTime? UpdatedAt,
    string Message);
