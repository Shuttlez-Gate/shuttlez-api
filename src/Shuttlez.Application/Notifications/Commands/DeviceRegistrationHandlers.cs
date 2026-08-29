using Microsoft.Extensions.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Notifications.Commands;

public record RegisterDeviceCommand(string Token, string Platform) : IRequest<bool>;

public record UnregisterDeviceCommand(string Token) : IRequest<bool>;

public class DeviceRegistrationHandlers :
    IRequestHandler<RegisterDeviceCommand, bool>,
    IRequestHandler<UnregisterDeviceCommand, bool>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<DeviceRegistrationHandlers> _logger;

    public DeviceRegistrationHandlers(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        ILogger<DeviceRegistrationHandlers> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<bool> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        var token = (request.Token ?? string.Empty).Trim();
        if (token.StartsWith("fcm:", StringComparison.OrdinalIgnoreCase))
        {
            token = token[4..].Trim();
        }
        if (string.IsNullOrWhiteSpace(token) || token.Length < 8)
        {
            throw new AppException("رمز الجهاز غير صالح", 400, "INVALID_DEVICE_TOKEN");
        }

        // Ignore install: placeholders from Captain device-binding stubs.
        if (token.StartsWith("install:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var platform = string.IsNullOrWhiteSpace(request.Platform)
            ? "android"
            : request.Platform.Trim().ToLowerInvariant();

        var now = _clock.UtcNow;

        // Same token may move between users (reinstall / account switch).
        var existingByToken = await _db.UserDevices
            .Where(d => d.Token == token && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var other in existingByToken.Where(d => d.UserId != userId))
        {
            other.IsActive = false;
            other.UpdatedAt = now;
            _db.Update(other);
        }

        var mine = existingByToken.FirstOrDefault(d => d.UserId == userId);
        if (mine is null)
        {
            _db.Add(new UserDevice
            {
                UserId = userId,
                Token = token,
                Platform = platform,
                IsActive = true,
                LastSeenAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            mine.Platform = platform;
            mine.IsActive = true;
            mine.LastSeenAt = now;
            mine.UpdatedAt = now;
            mine.IsDeleted = false;
            _db.Update(mine);
        }

        // Keep legacy single-token column in sync for older consumers.
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user is not null)
        {
            user.FcmToken = token;
            user.UpdatedAt = now;
            _db.Update(user);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "{Event} UserId={UserId} Platform={Platform}",
            PushLogEvents.DeviceRegistered,
            userId,
            platform);
        return true;
    }

    public async Task<bool> Handle(UnregisterDeviceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        var token = (request.Token ?? string.Empty).Trim();
        if (token.StartsWith("fcm:", StringComparison.OrdinalIgnoreCase))
        {
            token = token[4..].Trim();
        }
        if (string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        var devices = await _db.UserDevices
            .Where(d => d.UserId == userId && d.Token == token && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
        foreach (var d in devices)
        {
            d.IsActive = false;
            d.UpdatedAt = now;
            _db.Update(d);
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (devices.Count > 0)
        {
            _logger.LogInformation(
                "{Event} UserId={UserId} DeviceCount={DeviceCount}",
                PushLogEvents.DeviceUnregistered,
                userId,
                devices.Count);
        }
        return true;
    }
}
