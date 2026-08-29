using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Infrastructure.Configuration;
using Shuttlez.Infrastructure.Data;

namespace Shuttlez.Infrastructure.Services;

public sealed class FirebasePushNotificationService : IPushNotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<FirebasePushNotificationService> _logger;
    private readonly Lazy<FirebaseApp?> _app;

    public FirebasePushNotificationService(
        AppDbContext db,
        IOptions<FirebaseAuthSettings> settings,
        ILogger<FirebasePushNotificationService> logger)
    {
        _db = db;
        _logger = logger;
        _app = new Lazy<FirebaseApp?>(() => ResolveApp(settings.Value));
    }

    public Task SendToUserAsync(
        Guid userId,
        PushNotificationMessage message,
        CancellationToken cancellationToken = default) =>
        SendToUsersAsync([userId], message, cancellationToken);

    public async Task SendToUsersAsync(
        IEnumerable<Guid> userIds,
        PushNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var tokens = await _db.UserDevicesSet
            .AsNoTracking()
            .Where(d => ids.Contains(d.UserId) && d.IsActive && !d.IsDeleted)
            .Select(d => d.Token)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Legacy fallback
        if (tokens.Count == 0)
        {
            tokens = await _db.UsersSet
                .AsNoTracking()
                .Where(u => ids.Contains(u.Id) && u.FcmToken != null && u.FcmToken != "")
                .Select(u => u.FcmToken!)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        foreach (var token in tokens)
        {
            await SendToDeviceAsync(token, message, cancellationToken);
        }
    }

    public async Task SendToDeviceAsync(
        string token,
        PushNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return;

        var app = _app.Value;
        if (app is null)
        {
            _logger.LogWarning("{Event} FCM skipped — Firebase app not configured", PushLogEvents.FcmNotConfigured);
            return;
        }

        try
        {
            var data = message.Data.ToDictionary(kv => kv.Key, kv => kv.Value);
            if (!data.ContainsKey("type"))
            {
                data["type"] = message.Type;
            }

            var fcm = new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = message.Title,
                    Body = message.Body
                },
                Data = data,
                Android = new AndroidConfig
                {
                    Priority = Priority.High
                }
            };

            await FirebaseMessaging.GetMessaging(app).SendAsync(fcm, cancellationToken);
        }
        catch (FirebaseMessagingException ex) when (
            ex.MessagingErrorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
        {
            _logger.LogInformation(
                "{Event} NotificationType={NotificationType} MessagingErrorCode={MessagingErrorCode}",
                PushLogEvents.FcmTokenInvalid,
                message.Type,
                ex.MessagingErrorCode);
            await DeactivateTokenAsync(token, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "{Event} NotificationType={NotificationType}",
                PushLogEvents.FcmSendFailed,
                message.Type);
        }
    }

    private async Task DeactivateTokenAsync(string token, CancellationToken ct)
    {
        try
        {
            var devices = await _db.UserDevicesSet
                .Where(d => d.Token == token && d.IsActive && !d.IsDeleted)
                .ToListAsync(ct);

            var now = DateTime.UtcNow;
            foreach (var d in devices)
            {
                d.IsActive = false;
                d.UpdatedAt = now;
            }

            if (devices.Count > 0)
            {
                await _db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deactivate FCM token");
        }
    }

    private static FirebaseApp? ResolveApp(FirebaseAuthSettings settings)
    {
        try
        {
            foreach (var name in new[] { "ShuttlezSocialAuth", "[DEFAULT]" })
            {
                try
                {
                    var existing = FirebaseApp.GetInstance(name);
                    if (existing is not null) return existing;
                }
                catch
                {
                }
            }

            if (FirebaseApp.DefaultInstance is not null)
                return FirebaseApp.DefaultInstance;
        }
        catch
        {
        }

        var credentialsPath = settings.CredentialsPath;
        if (string.IsNullOrWhiteSpace(credentialsPath))
        {
            credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        }

        if (string.IsNullOrWhiteSpace(credentialsPath) || !File.Exists(credentialsPath))
        {
            return null;
        }

        var options = new AppOptions
        {
            Credential = GoogleCredential.FromFile(credentialsPath)
        };

        if (!string.IsNullOrWhiteSpace(settings.ProjectId))
        {
            options.ProjectId = settings.ProjectId;
        }

        try
        {
            return FirebaseApp.Create(options, "ShuttlezSocialAuth");
        }
        catch (ArgumentException)
        {
            return FirebaseApp.GetInstance("ShuttlezSocialAuth");
        }
    }
}

public sealed class NullPushNotificationService : IPushNotificationService
{
    public Task SendToUserAsync(Guid userId, PushNotificationMessage message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SendToUsersAsync(IEnumerable<Guid> userIds, PushNotificationMessage message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SendToDeviceAsync(string token, PushNotificationMessage message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
