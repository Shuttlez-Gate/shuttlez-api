namespace Shuttlez.Infrastructure.Configuration;

public class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "Shuttlez";

    public string[] AllowedOrigins { get; set; } = [];
}
