using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;
using Shuttlez.Infrastructure.Configuration;

namespace Shuttlez.Infrastructure.Services;

public class OtpService : IOtpService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly OtpSettings _settings;
    private readonly IHostEnvironment _env;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IAppDbContext db,
        IDateTimeProvider clock,
        IOptions<OtpSettings> settings,
        IHostEnvironment env,
        ILogger<OtpService> logger)
    {
        _db = db;
        _clock = clock;
        _settings = settings.Value;
        _env = env;
        _logger = logger;
    }

    public async Task SendOtpAsync(string phone, OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = PhoneNormalizer.Normalize(phone);
        var code = _env.IsDevelopment()
            ? _settings.DevBypassCode
            : GenerateCode(_settings.CodeLength);

        var otp = new OtpRequest
        {
            Phone = normalizedPhone,
            CodeHash = HashCode(code),
            Purpose = purpose,
            ExpiresAt = _clock.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            Attempts = 0,
            IsUsed = false
        };

        _db.Add(otp);
        await _db.SaveChangesAsync(cancellationToken);

        if (_env.IsDevelopment() && _settings.LogCodeInDevelopment)
        {
            _logger.LogWarning("DEV OTP for {Phone}: {Code}", normalizedPhone, code);
        }
        else
        {
            // TODO: integrate SMS provider (Unifonic / Twilio)
            _logger.LogInformation("OTP sent to {Phone}", normalizedPhone);
        }
    }

    public async Task<bool> VerifyOtpAsync(
        string phone,
        string code,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = PhoneNormalizer.Normalize(phone);

        if (_env.IsDevelopment() && code == _settings.DevBypassCode)
        {
            return true;
        }

        var otp = await _db.OtpRequests
            .Where(o => o.Phone == normalizedPhone && o.Purpose == purpose && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || otp.ExpiresAt < _clock.UtcNow)
        {
            return false;
        }

        otp.Attempts++;
        if (otp.Attempts > _settings.MaxAttempts)
        {
            otp.IsUsed = true;
            await _db.SaveChangesAsync(cancellationToken);
            return false;
        }

        var valid = otp.CodeHash == HashCode(code);
        if (valid)
        {
            otp.IsUsed = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return valid;
    }

    private static string GenerateCode(int length)
    {
        var max = (int)Math.Pow(10, length);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString($"D{length}");
    }

    private static string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }
}
