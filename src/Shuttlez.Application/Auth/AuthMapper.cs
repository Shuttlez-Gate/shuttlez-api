using Shuttlez.Application.Auth.DTOs;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Auth;

public static class AuthMapper
{
    public static UserProfileDto ToProfileDto(User user) => new(
        user.Id,
        user.Phone,
        user.FullName,
        user.Email,
        user.Gender switch
        {
            Gender.Male => "ذكر",
            Gender.Female => "أنثى",
            _ => null
        },
        user.AvatarUrl,
        user.RatingAverage,
        user.RatingCount,
        user.UserType.ToString().ToLowerInvariant());

    public static Gender? ParseGender(string? gender) => gender?.Trim().ToLowerInvariant() switch
    {
        "male" or "ذكر" => Gender.Male,
        "female" or "أنثى" => Gender.Female,
        _ => null
    };

    public static OtpPurpose ParsePurpose(string purpose) =>
        purpose.Trim().ToLowerInvariant() switch
        {
            "register" => OtpPurpose.Register,
            "social-link" => OtpPurpose.SocialLink,
            _ => OtpPurpose.Login
        };
}
