using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.RouteMatching.Models;
using Shuttlez.Infrastructure.Configuration;

namespace Shuttlez.Infrastructure.Services;

public sealed class GoogleDirectionsService : IGoogleDirectionsService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleMapsSettings _settings;
    private readonly ILogger<GoogleDirectionsService> _logger;

    public GoogleDirectionsService(
        HttpClient httpClient,
        IOptions<GoogleMapsSettings> settings,
        ILogger<GoogleDirectionsService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<DirectionsResult?> GetDirectionsAsync(
        GeoCoordinate origin,
        GeoCoordinate destination,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("Google Maps API key is missing. Using straight-line fallback polyline.");
            return BuildFallbackDirections(origin, destination);
        }

        var url =
            $"{_settings.DirectionsBaseUrl}?origin={FormatCoord(origin)}&destination={FormatCoord(destination)}&key={Uri.EscapeDataString(_settings.ApiKey)}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<GoogleDirectionsResponse>(
                url,
                cancellationToken);

            var route = response?.Routes?.FirstOrDefault();
            var leg = route?.Legs?.FirstOrDefault();
            var polyline = route?.OverviewPolyline?.Points;

            if (string.IsNullOrWhiteSpace(polyline) || leg is null)
            {
                _logger.LogWarning("Google Directions returned no route. Using fallback polyline.");
                return BuildFallbackDirections(origin, destination);
            }

            return new DirectionsResult(
                polyline,
                leg.Distance?.Value ?? 0,
                leg.Duration?.Value ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Google Directions API. Using fallback polyline.");
            return BuildFallbackDirections(origin, destination);
        }
    }

    private static DirectionsResult BuildFallbackDirections(
        GeoCoordinate origin,
        GeoCoordinate destination)
    {
        var encoded = PolylineEncoder.Encode([origin, destination]);
        var distance = (int)Math.Round(
            HaversineMeters(origin, destination),
            MidpointRounding.AwayFromZero);

        return new DirectionsResult(encoded, distance, 0);
    }

    private static string FormatCoord(GeoCoordinate coordinate) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{coordinate.Latitude:0.######},{coordinate.Longitude:0.######}");

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

    private sealed class GoogleDirectionsResponse
    {
        [JsonPropertyName("routes")]
        public List<GoogleRoute>? Routes { get; set; }
    }

    private sealed class GoogleRoute
    {
        [JsonPropertyName("overview_polyline")]
        public GooglePolyline? OverviewPolyline { get; set; }

        [JsonPropertyName("legs")]
        public List<GoogleLeg>? Legs { get; set; }
    }

    private sealed class GooglePolyline
    {
        [JsonPropertyName("points")]
        public string? Points { get; set; }
    }

    private sealed class GoogleLeg
    {
        [JsonPropertyName("distance")]
        public GoogleValue? Distance { get; set; }

        [JsonPropertyName("duration")]
        public GoogleValue? Duration { get; set; }
    }

    private sealed class GoogleValue
    {
        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
}

internal static class PolylineEncoder
{
    public static string Encode(IReadOnlyList<GeoCoordinate> points)
    {
        if (points.Count == 0)
            return string.Empty;

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
