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
    string? VehicleAsset = null);

public record DriverTripListResponseDto(
    DriverTripCardDto? CurrentTrip,
    IReadOnlyList<DriverTripCardDto> UpcomingTrips,
    IReadOnlyList<DriverTripCardDto> HistoryTrips);
