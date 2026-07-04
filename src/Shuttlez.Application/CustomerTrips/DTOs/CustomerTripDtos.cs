using MediatR;
using Shuttlez.Application.RouteMatching.Models;

namespace Shuttlez.Application.CustomerTrips.DTOs;

public record CreateCustomerTripRequest(
    double OriginLatitude,
    double OriginLongitude,
    double DestinationLatitude,
    double DestinationLongitude,
    string? OriginAddress,
    string? DestinationAddress,
    DateTime? PreferredDepartureTime);

public record GeoCoordinateDto(double Latitude, double Longitude);

public record RouteMatchResultDto(
    Guid RouteId,
    double MatchPercentage,
    double OriginDistanceMeters,
    double DestinationDistanceMeters,
    GeoCoordinateDto OriginClosestPoint,
    GeoCoordinateDto DestinationClosestPoint,
    GeoCoordinateDto SuggestedPickupPoint,
    GeoCoordinateDto SuggestedDropoffPoint,
    DateTime? NextDepartureTime,
    double DeviationMeters,
    double TotalDistanceMeters);

public record CreateCustomerTripResponse(
    bool IsMatched,
    Guid? RouteId,
    Guid? TripId,
    RouteMatchResultDto? Match,
    string Message);
