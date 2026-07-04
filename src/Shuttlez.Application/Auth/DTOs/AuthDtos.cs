namespace Shuttlez.Application.Auth.DTOs;

public record SendOtpRequest(string Phone, string Purpose = "login");

public record VerifyOtpRequest(string Phone, string Code, string Purpose = "login");

public record RegisterRequest(
    string Phone,
    string FullName,
    string? Email,
    string Gender,
    string Code);

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
