using Microsoft.Extensions.Options;
using Shuttlez.Application.RouteMatching;
using Shuttlez.Application.RouteMatching.Models;

namespace Shuttlez.UnitTests.RouteMatching;

public class RouteMatchingServiceTests
{
    private static RouteMatchingService CreateService(double maxDistanceMeters = 300) =>
        new(Options.Create(new RouteMatchingOptions
        {
            MaxDistanceMeters = maxDistanceMeters,
            MaxPolylinePointsForMatching = 500,
        }));

    [Fact]
    public void DecodePolyline_DecodesGoogleSample()
    {
        var service = CreateService();
        var decoded = service.DecodePolyline("_p~iF~ps|U_ulLnnqC_mqNvxq`@");

        Assert.True(decoded.Count >= 2);
        Assert.InRange(decoded[0].Latitude, 38.4, 38.6);
        Assert.InRange(decoded[0].Longitude, -120.3, -120.1);
    }

    [Fact]
    public void IsPointNearRoute_ReturnsTrueForPointOnLine()
    {
        var service = CreateService(maxDistanceMeters: 50);
        var polyline = new[]
        {
            new GeoCoordinate(30.0, 31.0),
            new GeoCoordinate(30.01, 31.01),
            new GeoCoordinate(30.02, 31.02),
        };

        var midpoint = new GeoCoordinate(30.01, 31.01);
        Assert.True(service.IsPointNearRoute(midpoint, polyline, 50));
    }

    [Fact]
    public void MatchUserWithExistingRoutes_PrefersClosestRoute()
    {
        var service = CreateService(maxDistanceMeters: 500);
        var origin = new GeoCoordinate(30.09, 31.29);
        var destination = new GeoCoordinate(30.04, 31.36);

        var farRoute = new ActiveRouteMatchInput(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Encode([new GeoCoordinate(29.0, 30.0), new GeoCoordinate(29.5, 30.5)]),
            DateTime.UtcNow.AddHours(5),
            29.0,
            29.5,
            30.0,
            30.5);

        var nearRoute = new ActiveRouteMatchInput(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Encode([
                new GeoCoordinate(30.09, 31.29),
                new GeoCoordinate(30.08, 31.31),
                new GeoCoordinate(30.06, 31.34),
                new GeoCoordinate(30.04, 31.36),
            ]),
            DateTime.UtcNow.AddHours(2),
            30.04,
            30.10,
            31.29,
            31.36);

        var matches = service.MatchUserWithExistingRoutes(
            origin,
            destination,
            [farRoute, nearRoute]);

        Assert.NotEmpty(matches);
        Assert.Equal(nearRoute.RouteId, matches[0].RouteId);
        Assert.True(matches[0].MatchPercentage > 0);
    }

    [Fact]
    public void MatchUserWithExistingRoutes_RejectsWrongDirection()
    {
        var service = CreateService(maxDistanceMeters: 500);
        var origin = new GeoCoordinate(30.06, 31.34);
        var destination = new GeoCoordinate(30.10, 31.30);

        var reversedRoute = new ActiveRouteMatchInput(
            Guid.NewGuid(),
            Encode([
                new GeoCoordinate(30.10, 31.30),
                new GeoCoordinate(30.06, 31.34),
            ]),
            null,
            30.06,
            30.10,
            31.30,
            31.34);

        var matches = service.MatchUserWithExistingRoutes(
            origin,
            destination,
            [reversedRoute]);

        Assert.Empty(matches);
    }

    private static string Encode(IReadOnlyList<GeoCoordinate> points)
    {
        var encoded = new System.Text.StringBuilder();
        var lastLat = 0;
        var lastLng = 0;

        foreach (var point in points)
        {
            var lat = (int)Math.Round(point.Latitude * 1e5, MidpointRounding.AwayFromZero);
            var lng = (int)Math.Round(point.Longitude * 1e5, MidpointRounding.AwayFromZero);
            EncodeComponent(encoded, lat - lastLat);
            EncodeComponent(encoded, lng - lastLng);
            lastLat = lat;
            lastLng = lng;
        }

        return encoded.ToString();
    }

    private static void EncodeComponent(System.Text.StringBuilder encoded, int value)
    {
        var shifted = value << 1;
        if (value < 0)
            shifted = ~shifted;

        while (shifted >= 0x20)
        {
            encoded.Append((char)((0x20 | (shifted & 0x1f)) + 63));
            shifted >>= 5;
        }

        encoded.Append((char)(shifted + 63));
    }
}
