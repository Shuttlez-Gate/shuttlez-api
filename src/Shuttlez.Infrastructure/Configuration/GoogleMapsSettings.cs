namespace Shuttlez.Infrastructure.Configuration;

public class GoogleMapsSettings
{
    public const string SectionName = "GoogleMaps";

    public string ApiKey { get; set; } = string.Empty;
    public string DirectionsBaseUrl { get; set; } = "https://maps.googleapis.com/maps/api/directions/json";
}
