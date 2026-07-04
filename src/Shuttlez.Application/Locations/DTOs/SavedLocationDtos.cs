namespace Shuttlez.Application.Locations.DTOs;

public record SavedLocationDto(
    Guid Id,
    string Label,
    string Address,
    double Latitude,
    double Longitude,
    bool IsFavorite,
    DateTime CreatedAt);

public record CreateSavedLocationRequest(
    string Label,
    string Address,
    double Latitude,
    double Longitude,
    bool IsFavorite = false);

public record UpdateSavedLocationRequest(
    string? Label,
    string? Address,
    double? Latitude,
    double? Longitude,
    bool? IsFavorite);
