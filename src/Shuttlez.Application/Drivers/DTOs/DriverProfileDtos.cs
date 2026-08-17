namespace Shuttlez.Application.Drivers.DTOs;

public record DriverProfileDto(
    Guid Id,
    string Phone,
    string? FullName,
    string? Email,
    string? Gender,
    string? AvatarUrl,
    double RatingAverage,
    int RatingCount,
    string? VehicleType,
    string? VehicleModel,
    string? PlateNumber,
    int? VehicleCapacity);
