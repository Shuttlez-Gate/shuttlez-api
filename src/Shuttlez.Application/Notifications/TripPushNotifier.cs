using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shuttlez.Application.Bookings;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Notifications;

/// <summary>
/// Domain event helpers for push — always best-effort after business commit.
/// </summary>
public class TripPushNotifier
{
    private readonly IAppDbContext _db;
    private readonly IPushNotificationService _push;
    private readonly ILogger<TripPushNotifier> _logger;

    public TripPushNotifier(
        IAppDbContext db,
        IPushNotificationService push,
        ILogger<TripPushNotifier> logger)
    {
        _db = db;
        _push = push;
        _logger = logger;
    }

    public async Task NotifyCaptainAssignedAsync(
        Guid driverUserId,
        Guid tripId,
        Guid routeId,
        DateTime scheduledAtUtc,
        string? routeName,
        CancellationToken cancellationToken = default)
    {
        var when = scheduledAtUtc.ToLocalTime().ToString("g");
        var route = string.IsNullOrWhiteSpace(routeName) ? "رحلتك" : routeName.Trim();
        await SafeSendAsync(
            driverUserId,
            new PushNotificationMessage(
                NotificationTypes.CaptainTripAssigned,
                "تم تعيين رحلة جديدة لك",
                $"{route} — {when}",
                BuildTripData(NotificationTypes.CaptainTripAssigned, tripId, routeId, scheduledAtUtc)),
            cancellationToken);
    }

    public async Task NotifyCaptainUnassignedAsync(
        Guid driverUserId,
        Guid tripId,
        Guid routeId,
        DateTime scheduledAtUtc,
        CancellationToken cancellationToken = default)
    {
        await SafeSendAsync(
            driverUserId,
            new PushNotificationMessage(
                NotificationTypes.CaptainTripUnassigned,
                "تم إلغاء تعيين الرحلة منك",
                "تم تحديث حالة رحلتك",
                BuildTripData(NotificationTypes.CaptainTripUnassigned, tripId, routeId, scheduledAtUtc)),
            cancellationToken);
    }

    public async Task NotifyTripCancelledAsync(
        Guid userId,
        Guid tripId,
        Guid routeId,
        DateTime scheduledAtUtc,
        CancellationToken cancellationToken = default)
    {
        await SafeSendAsync(
            userId,
            new PushNotificationMessage(
                NotificationTypes.TripCancelled,
                "تم إلغاء الرحلة",
                "تم إلغاء الرحلة",
                BuildTripData(NotificationTypes.TripCancelled, tripId, routeId, scheduledAtUtc)),
            cancellationToken);
    }

    public async Task NotifyBookingConfirmedAsync(
        Guid userId,
        Guid bookingId,
        Guid tripId,
        int seatCount,
        decimal totalAmount,
        string paymentMethod = "cash",
        CancellationToken cancellationToken = default)
    {
        var isCash = CashPaymentPolicy.IsCash(paymentMethod);
        var title = "تم تأكيد حجزك";
        var body = isCash
            ? "تم تأكيد الحجز — الدفع نقدًا للكابتن"
            : "تم تأكيد الحجز";

        await SafeSendAsync(
            userId,
            new PushNotificationMessage(
                NotificationTypes.BookingConfirmed,
                title,
                body,
                new Dictionary<string, string>
                {
                    ["type"] = NotificationTypes.BookingConfirmed,
                    ["bookingId"] = bookingId.ToString(),
                    ["tripId"] = tripId.ToString(),
                    ["seatCount"] = seatCount.ToString(),
                    ["totalAmount"] = totalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["paymentMethod"] = CashPaymentPolicy.IsCash(paymentMethod)
                        ? CashPaymentPolicy.Cash
                        : paymentMethod
                }),
            cancellationToken);
    }

    public async Task NotifyRidersTripStartedAsync(
        Guid tripId,
        Guid routeId,
        DateTime scheduledAtUtc,
        CancellationToken cancellationToken = default)
    {
        var riderIds = await LoadConfirmedRiderUserIdsAsync(tripId, cancellationToken);
        if (riderIds.Count == 0) return;

        await SafeSendManyAsync(
            riderIds,
            new PushNotificationMessage(
                NotificationTypes.TripStarted,
                "بدأت الرحلة",
                "بدأت الرحلة",
                BuildTripData(NotificationTypes.TripStarted, tripId, routeId, scheduledAtUtc)),
            cancellationToken);
    }

    public async Task NotifyRidersTripCompletedAsync(
        Guid tripId,
        Guid routeId,
        DateTime scheduledAtUtc,
        CancellationToken cancellationToken = default)
    {
        var riderIds = await LoadConfirmedRiderUserIdsAsync(tripId, cancellationToken);
        if (riderIds.Count == 0) return;

        await SafeSendManyAsync(
            riderIds,
            new PushNotificationMessage(
                NotificationTypes.TripCompleted,
                "انتهت الرحلة",
                "انتهت الرحلة",
                BuildTripData(NotificationTypes.TripCompleted, tripId, routeId, scheduledAtUtc)),
            cancellationToken);
    }

    private async Task<List<Guid>> LoadConfirmedRiderUserIdsAsync(Guid tripId, CancellationToken ct)
    {
        return await _db.Bookings
            .Where(b =>
                b.TripId == tripId &&
                !b.IsDeleted &&
                b.Status == BookingStatus.Confirmed)
            .Select(b => b.UserId)
            .Distinct()
            .ToListAsync(ct);
    }

    private static Dictionary<string, string> BuildTripData(
        string type,
        Guid tripId,
        Guid routeId,
        DateTime scheduledAtUtc) =>
        new()
        {
            ["type"] = type,
            ["tripId"] = tripId.ToString(),
            ["routeId"] = routeId.ToString(),
            ["scheduledAt"] = scheduledAtUtc.ToUniversalTime().ToString("O")
        };

    private async Task SafeSendAsync(
        Guid userId,
        PushNotificationMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _push.SendToUserAsync(userId, message, cancellationToken);
            _logger.LogInformation(
                "{Event} NotificationType={NotificationType} UserId={UserId} TripId={TripId} BookingId={BookingId}",
                MapEvent(message.Type),
                message.Type,
                userId,
                message.Data.GetValueOrDefault("tripId"),
                message.Data.GetValueOrDefault("bookingId"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "{Event} NotificationType={NotificationType} UserId={UserId} TripId={TripId}",
                PushLogEvents.FcmSendFailed,
                message.Type,
                userId,
                message.Data.GetValueOrDefault("tripId"));
        }
    }

    private async Task SafeSendManyAsync(
        IReadOnlyList<Guid> userIds,
        PushNotificationMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _push.SendToUsersAsync(userIds, message, cancellationToken);
            _logger.LogInformation(
                "{Event} NotificationType={NotificationType} RecipientCount={RecipientCount} TripId={TripId}",
                MapEvent(message.Type),
                message.Type,
                userIds.Count,
                message.Data.GetValueOrDefault("tripId"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "{Event} NotificationType={NotificationType} RecipientCount={RecipientCount} TripId={TripId}",
                PushLogEvents.FcmSendFailed,
                message.Type,
                userIds.Count,
                message.Data.GetValueOrDefault("tripId"));
        }
    }

    private static string MapEvent(string notificationType) => notificationType switch
    {
        NotificationTypes.CaptainTripAssigned => PushLogEvents.CaptainAssigned,
        NotificationTypes.CaptainTripUnassigned => PushLogEvents.CaptainUnassigned,
        NotificationTypes.BookingConfirmed => PushLogEvents.BookingConfirmedNotification,
        NotificationTypes.TripStarted => PushLogEvents.TripStartedNotification,
        NotificationTypes.TripCompleted => PushLogEvents.TripCompletedNotification,
        NotificationTypes.TripCancelled => PushLogEvents.TripCancelledNotification,
        _ => notificationType
    };
}
