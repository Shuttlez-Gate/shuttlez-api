using Shuttlez.Application.RouteMatching.Models;

namespace Shuttlez.Application.Common.Interfaces;

public interface IGoogleDirectionsService
{
    Task<DirectionsResult?> GetDirectionsAsync(
        GeoCoordinate origin,
        GeoCoordinate destination,
        CancellationToken cancellationToken = default);
}

public sealed record DirectionsResult(
    string EncodedPolyline,
    int DistanceMeters,
    int DurationSeconds);
