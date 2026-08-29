using Microsoft.Extensions.Logging;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;

namespace Shuttlez.Application.Notifications;

/// <summary>
/// Ride push helpers — always best-effort AFTER business commit. FCM failure must not roll back assignment.
/// </summary>
public class RidePushNotifier
{
    private readonly IPushNotificationService _push;
    private readonly ILogger<RidePushNotifier> _logger;

    public RidePushNotifier(
        IPushNotificationService push,
        ILogger<RidePushNotifier> logger)
    {
        _push = push;
        _logger = logger;
    }

    /// <summary>
    /// Notify the authenticated Rider (by RiderUserId) that a Captain was assigned.
    /// Payload carries rideId only — client must refresh Ride from API.
    /// </summary>
    public Task NotifyRideAssignedAsync(
        Guid riderUserId,
        Guid rideId,
        CancellationToken cancellationToken = default) =>
        SafeSendAsync(
            riderUserId,
            new PushNotificationMessage(
                NotificationTypes.RideAssigned,
                "تم تعيين الكابتن",
                "تم تعيين كابتن لمشوارك — الدفع نقدًا للكابتن",
                new Dictionary<string, string>
                {
                    ["type"] = NotificationTypes.RideAssigned,
                    ["rideId"] = rideId.ToString()
                }),
            cancellationToken);

    private async Task SafeSendAsync(
        Guid userId,
        PushNotificationMessage message,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) return;

        try
        {
            await _push.SendToUserAsync(userId, message, cancellationToken);
            _logger.LogInformation(
                "{Event} NotificationType={NotificationType} UserId={UserId} RideId={RideId}",
                PushLogEvents.RideAssignedNotification,
                message.Type,
                userId,
                message.Data.GetValueOrDefault("rideId"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "{Event} NotificationType={NotificationType} UserId={UserId} RideId={RideId}",
                PushLogEvents.FcmSendFailed,
                message.Type,
                userId,
                message.Data.GetValueOrDefault("rideId"));
        }
    }
}
