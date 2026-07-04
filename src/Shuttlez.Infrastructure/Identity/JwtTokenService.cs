using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;
using Shuttlez.Infrastructure.Configuration;

namespace Shuttlez.Infrastructure.Identity;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly IDateTimeProvider _clock;

    public JwtTokenService(IOptions<JwtSettings> settings, IDateTimeProvider clock)
    {
        _settings = settings.Value;
        _clock = clock;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.PhoneNumber, user.Phone),
            new(ClaimTypes.MobilePhone, user.Phone),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.UserType.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = _clock.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public Guid? GetUserIdFromExpiredToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret)),
            ValidateLifetime = false
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);
            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }
}
