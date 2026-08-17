namespace Shuttlez.Application.Auth.DTOs;

public record SendOtpRequest(string Phone, string Purpose = "login", string? Client = null);

public record SendOtpResponseDto(string Message, string? DebugCode = null);

public record VerifyOtpRequest(string Phone, string Code, string Purpose = "login", string? Client = null);

public record RegisterRequest(
    string Phone,
    string FullName,
    string? Email,
    string Gender,
    string Code,
    string? UserType = null,
    string? DeviceId = null,
    string? Client = null,
    string? NationalId = null,
    string? BirthDate = null,
    string? LicenseNumber = null,
    string? LicenseType = null,
    string? LicenseExpiry = null,
    string? VehicleKind = null,
    string? VehicleModel = null,
    int? ManufactureYear = null,
    string? PlateNumber = null,
    string? VehicleColor = null,
    int? Seats = null);

public record RefreshTokenRequest(string RefreshToken);

public record AuthTokensDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);

public record AuthResponseDto(AuthTokensDto Tokens, UserProfileDto User);

public record UserProfileDto(
    Guid Id,
    string Phone,
    string? FullName,
    string? Email,
    string? Gender,
    string? AvatarUrl,
    decimal RatingAverage,
    int RatingCount,
    string UserType);
