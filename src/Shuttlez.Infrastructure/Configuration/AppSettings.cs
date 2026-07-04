namespace Shuttlez.Infrastructure.Configuration;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Shuttlez";
    public string Audience { get; set; } = "ShuttlezApp";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 30;
}

public class OtpSettings
{
    public const string SectionName = "Otp";
    public int CodeLength { get; set; } = 4;
    public int ExpiryMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public string DevBypassCode { get; set; } = "1234";
    public bool LogCodeInDevelopment { get; set; } = true;
}
