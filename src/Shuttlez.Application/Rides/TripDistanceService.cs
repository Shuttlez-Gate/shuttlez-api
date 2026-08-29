using Shuttlez.Application.Bookings;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.RouteMatching.Models;

namespace Shuttlez.Application.Rides;

public interface ITripDistanceService
{
    Task<decimal> GetDistanceKmAsync(
        GeoCoordinate origin,
        GeoCoordinate destination,
        CancellationToken cancellationToken = default);
}

public sealed class TripDistanceService : ITripDistanceService
{
    private readonly IGoogleDirectionsService _directions;

    public TripDistanceService(IGoogleDirectionsService directions) => _directions = directions;

    public async Task<decimal> GetDistanceKmAsync(
        GeoCoordinate origin,
        GeoCoordinate destination,
        CancellationToken cancellationToken = default)
    {
        var directions = await _directions.GetDirectionsAsync(origin, destination, null, cancellationToken);

        if (directions is not null && directions.DistanceMeters > 0)
        {
            return ShuttleFinancialCalculator.NormalizeMoney(directions.DistanceMeters / 1000.0m);
        }

        var haversineMeters = HaversineMeters(origin, destination);
        if (haversineMeters > 0)
        {
            return ShuttleFinancialCalculator.NormalizeMoney((decimal)(haversineMeters / 1000.0));
        }

        throw new AppException(
            "تعذر حساب مسافة الرحلة. يرجى التحقق من إحداثيات الانطلاق والوجهة.",
            400,
            ErrorCodes.TripDistanceNotCalculated);
    }

    private static double HaversineMeters(GeoCoordinate a, GeoCoordinate b)
    {
        const double earthRadius = 6_371_000;
        var dLat = DegreesToRadians(b.Latitude - a.Latitude);
        var dLng = DegreesToRadians(b.Longitude - a.Longitude);
        var lat1 = DegreesToRadians(a.Latitude);
        var lat2 = DegreesToRadians(b.Latitude);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * earthRadius * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
