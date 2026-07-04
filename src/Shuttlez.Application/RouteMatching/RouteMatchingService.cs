using Microsoft.Extensions.Options;
using Shuttlez.Application.RouteMatching.Models;

namespace Shuttlez.Application.RouteMatching;

public sealed class RouteMatchingService : IRouteMatchingService
{
    private const double EarthRadiusMeters = 6_371_000;
    private readonly RouteMatchingOptions _options;

    public RouteMatchingService(IOptions<RouteMatchingOptions> options) =>
        _options = options.Value;

    public IReadOnlyList<GeoCoordinate> DecodePolyline(string encodedPolyline)
    {
        if (string.IsNullOrWhiteSpace(encodedPolyline))
            return [];

        var polyline = new List<GeoCoordinate>();
        var index = 0;
        var lat = 0;
        var lng = 0;

        while (index < encodedPolyline.Length)
        {
            lat += DecodePolylineComponent(encodedPolyline, ref index);
            lng += DecodePolylineComponent(encodedPolyline, ref index);
            polyline.Add(new GeoCoordinate(lat / 1e5, lng / 1e5));
        }

        return polyline;
    }

    public PolylineProximityResult DistanceFromPointToPolyline(
        GeoCoordinate point,
        IReadOnlyList<GeoCoordinate> polyline)
    {
        if (polyline.Count == 0)
        {
            return new PolylineProximityResult(
                double.MaxValue,
                point,
                -1,
                0);
        }

        if (polyline.Count == 1)
        {
            var singleDistance = HaversineMeters(point, polyline[0]);
            return new PolylineProximityResult(
                singleDistance,
                polyline[0],
                0,
                0);
        }

        var bestDistance = double.MaxValue;
        var bestPoint = polyline[0];
        var bestSegmentIndex = 0;
        var bestAlong = 0d;
        var cumulative = 0d;

        for (var i = 0; i < polyline.Count - 1; i++)
        {
            var start = polyline[i];
            var end = polyline[i + 1];
            var segmentLength = HaversineMeters(start, end);

            var projection = ProjectPointOntoSegment(point, start, end);
            if (projection.DistanceMeters < bestDistance)
            {
                bestDistance = projection.DistanceMeters;
                bestPoint = projection.ClosestPoint;
                bestSegmentIndex = i;
                bestAlong = cumulative + projection.FractionAlongSegment * segmentLength;
            }

            cumulative += segmentLength;
        }

        return new PolylineProximityResult(
            bestDistance,
            bestPoint,
            bestSegmentIndex,
            bestAlong);
    }

    public bool IsPointNearRoute(
        GeoCoordinate point,
        IReadOnlyList<GeoCoordinate> polyline,
        double maxDistanceMeters) =>
        DistanceFromPointToPolyline(point, polyline).DistanceMeters <= maxDistanceMeters;

    public IReadOnlyList<RouteMatchResult> MatchUserWithExistingRoutes(
        GeoCoordinate origin,
        GeoCoordinate destination,
        IEnumerable<ActiveRouteMatchInput> activeRoutes)
    {
        var maxDistance = _options.MaxDistanceMeters;
        var corridor = BuildCorridorBounds(origin, destination, maxDistance);
        var decodedCache = new Dictionary<Guid, IReadOnlyList<GeoCoordinate>>();
        var matches = new List<RouteMatchResult>();

        foreach (var route in activeRoutes)
        {
            if (!CorridorIntersectsRouteBounds(corridor, route))
                continue;

            if (!decodedCache.TryGetValue(route.RouteId, out var polyline))
            {
                polyline = SamplePolyline(DecodePolyline(route.EncodedPolyline));
                decodedCache[route.RouteId] = polyline;
            }

            if (polyline.Count < 2)
                continue;

            var originProximity = DistanceFromPointToPolyline(origin, polyline);
            var destinationProximity = DistanceFromPointToPolyline(destination, polyline);

            if (originProximity.DistanceMeters > maxDistance ||
                destinationProximity.DistanceMeters > maxDistance)
            {
                continue;
            }

            if (originProximity.DistanceAlongPolylineMeters >=
                destinationProximity.DistanceAlongPolylineMeters)
            {
                continue;
            }

            var totalDistance = originProximity.DistanceMeters + destinationProximity.DistanceMeters;
            var deviation = Math.Abs(originProximity.DistanceMeters - destinationProximity.DistanceMeters);
            var averageDistance = totalDistance / 2d;
            var matchPercentage = Math.Clamp(
                100d * (1d - averageDistance / maxDistance),
                0d,
                100d);

            matches.Add(new RouteMatchResult(
                route.RouteId,
                Math.Round(matchPercentage, 2),
                Math.Round(originProximity.DistanceMeters, 2),
                Math.Round(destinationProximity.DistanceMeters, 2),
                originProximity.ClosestPoint,
                destinationProximity.ClosestPoint,
                originProximity.ClosestPoint,
                destinationProximity.ClosestPoint,
                route.NextDepartureTime,
                Math.Round(deviation, 2),
                Math.Round(totalDistance, 2)));
        }

        return matches
            .OrderBy(m => m.TotalDistanceMeters)
            .ThenBy(m => m.DeviationMeters)
            .ThenBy(m => m.NextDepartureTime ?? DateTime.MaxValue)
            .ToList();
    }

    public (double MinLatitude, double MaxLatitude, double MinLongitude, double MaxLongitude) ComputeBounds(
        IReadOnlyList<GeoCoordinate> polyline)
    {
        if (polyline.Count == 0)
            return (0, 0, 0, 0);

        var minLat = polyline[0].Latitude;
        var maxLat = polyline[0].Latitude;
        var minLng = polyline[0].Longitude;
        var maxLng = polyline[0].Longitude;

        for (var i = 1; i < polyline.Count; i++)
        {
            minLat = Math.Min(minLat, polyline[i].Latitude);
            maxLat = Math.Max(maxLat, polyline[i].Latitude);
            minLng = Math.Min(minLng, polyline[i].Longitude);
            maxLng = Math.Max(maxLng, polyline[i].Longitude);
        }

        return (minLat, maxLat, minLng, maxLng);
    }

    private IReadOnlyList<GeoCoordinate> SamplePolyline(IReadOnlyList<GeoCoordinate> polyline)
    {
        var maxPoints = _options.MaxPolylinePointsForMatching;
        if (polyline.Count <= maxPoints)
            return polyline;

        var step = (double)polyline.Count / maxPoints;
        var sampled = new List<GeoCoordinate>(maxPoints);

        for (var i = 0; i < maxPoints; i++)
        {
            var index = (int)Math.Floor(i * step);
            if (index >= polyline.Count)
                index = polyline.Count - 1;
            sampled.Add(polyline[index]);
        }

        if (sampled[^1] != polyline[^1])
            sampled.Add(polyline[^1]);

        return sampled;
    }

    private static CorridorBounds BuildCorridorBounds(
        GeoCoordinate origin,
        GeoCoordinate destination,
        double bufferMeters)
    {
        var bufferDegrees = bufferMeters / 111_320d;
        return new CorridorBounds(
            Math.Min(origin.Latitude, destination.Latitude) - bufferDegrees,
            Math.Max(origin.Latitude, destination.Latitude) + bufferDegrees,
            Math.Min(origin.Longitude, destination.Longitude) - bufferDegrees,
            Math.Max(origin.Longitude, destination.Longitude) + bufferDegrees);
    }

    private static bool CorridorIntersectsRouteBounds(
        CorridorBounds corridor,
        ActiveRouteMatchInput route)
    {
        if (route.BoundsMinLatitude is null ||
            route.BoundsMaxLatitude is null ||
            route.BoundsMinLongitude is null ||
            route.BoundsMaxLongitude is null)
        {
            return true;
        }

        return route.BoundsMaxLatitude >= corridor.MinLatitude &&
               route.BoundsMinLatitude <= corridor.MaxLatitude &&
               route.BoundsMaxLongitude >= corridor.MinLongitude &&
               route.BoundsMinLongitude <= corridor.MaxLongitude;
    }

    private static int DecodePolylineComponent(string encodedPolyline, ref int index)
    {
        var result = 0;
        var shift = 0;
        int chunk;

        do
        {
            chunk = encodedPolyline[index++] - 63;
            result |= (chunk & 0x1f) << shift;
            shift += 5;
        } while (chunk >= 0x20);

        return (result & 1) == 1 ? ~(result >> 1) : result >> 1;
    }

    private static SegmentProjection ProjectPointOntoSegment(
        GeoCoordinate point,
        GeoCoordinate segmentStart,
        GeoCoordinate segmentEnd)
    {
        var refLat = (segmentStart.Latitude + segmentEnd.Latitude + point.Latitude) / 3d;
        var cosLat = Math.Cos(refLat * Math.PI / 180d);

        var ax = segmentStart.Longitude * cosLat;
        var ay = segmentStart.Latitude;
        var bx = segmentEnd.Longitude * cosLat;
        var by = segmentEnd.Latitude;
        var px = point.Longitude * cosLat;
        var py = point.Latitude;

        var abx = bx - ax;
        var aby = by - ay;
        var lengthSquared = abx * abx + aby * aby;

        var fraction = lengthSquared <= double.Epsilon
            ? 0d
            : Math.Clamp(((px - ax) * abx + (py - ay) * aby) / lengthSquared, 0d, 1d);

        var closest = new GeoCoordinate(
            ay + aby * fraction,
            (ax + abx * fraction) / cosLat);

        return new SegmentProjection(
            HaversineMeters(point, closest),
            closest,
            fraction);
    }

    private static double HaversineMeters(GeoCoordinate a, GeoCoordinate b)
    {
        var dLat = DegreesToRadians(b.Latitude - a.Latitude);
        var dLng = DegreesToRadians(b.Longitude - a.Longitude);
        var lat1 = DegreesToRadians(a.Latitude);
        var lat2 = DegreesToRadians(b.Latitude);

        var sinDLat = Math.Sin(dLat / 2);
        var sinDLng = Math.Sin(dLng / 2);
        var h = sinDLat * sinDLat +
                Math.Cos(lat1) * Math.Cos(lat2) * sinDLng * sinDLng;

        return 2 * EarthRadiusMeters * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;

    private readonly record struct CorridorBounds(
        double MinLatitude,
        double MaxLatitude,
        double MinLongitude,
        double MaxLongitude);

    private readonly record struct SegmentProjection(
        double DistanceMeters,
        GeoCoordinate ClosestPoint,
        double FractionAlongSegment);
}
