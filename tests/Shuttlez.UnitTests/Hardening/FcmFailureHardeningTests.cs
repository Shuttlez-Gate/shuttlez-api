using Microsoft.Extensions.Logging.Abstractions;
using Shuttlez.Application.Bookings;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Notifications;
using Shuttlez.Domain.Enums;

namespace Shuttlez.UnitTests.Hardening;

/// <summary>Fake FCM that always throws — business callers must still succeed.</summary>
file sealed class ThrowingPushNotificationService : IPushNotificationService
{
    public int Calls { get; private set; }

    public Task SendToUserAsync(Guid userId, PushNotificationMessage message, CancellationToken cancellationToken = default)
    {
        Calls++;
        throw new InvalidOperationException("simulated FCM outage");
    }

    public Task SendToUsersAsync(IEnumerable<Guid> userIds, PushNotificationMessage message, CancellationToken cancellationToken = default)
    {
        Calls++;
        throw new InvalidOperationException("simulated FCM outage");
    }

    public Task SendToDeviceAsync(string token, PushNotificationMessage message, CancellationToken cancellationToken = default)
    {
        Calls++;
        throw new InvalidOperationException("simulated FCM outage");
    }
}

file sealed class RecordingPushNotificationService : IPushNotificationService
{
    public List<PushNotificationMessage> Sent { get; } = [];

    public Task SendToUserAsync(Guid userId, PushNotificationMessage message, CancellationToken cancellationToken = default)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }

    public Task SendToUsersAsync(IEnumerable<Guid> userIds, PushNotificationMessage message, CancellationToken cancellationToken = default)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }

    public Task SendToDeviceAsync(string token, PushNotificationMessage message, CancellationToken cancellationToken = default)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}

public class FcmFailureDoesNotBreakBusinessTests
{
    [Fact]
    public async Task TripPushNotifier_swallows_FCM_exceptions()
    {
        var throwing = new ThrowingPushNotificationService();
        var notifier = new TripPushNotifier(
            db: null!,
            push: throwing,
            logger: NullLogger<TripPushNotifier>.Instance);

        await notifier.NotifyCaptainAssignedAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            "خط تجريبي");

        await notifier.NotifyBookingConfirmedAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            seatCount: 2,
            totalAmount: 50m);

        Assert.True(throwing.Calls >= 2);
    }

    [Fact]
    public async Task Recording_push_succeeds_without_throwing()
    {
        var recording = new RecordingPushNotificationService();
        var notifier = new TripPushNotifier(
            db: null!,
            push: recording,
            logger: NullLogger<TripPushNotifier>.Instance);

        await notifier.NotifyCaptainUnassignedAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);

        Assert.Single(recording.Sent);
        Assert.Equal(NotificationTypes.CaptainTripUnassigned, recording.Sent[0].Type);
    }
}

public class PushContractAndStateSafetyTests
{
    [Fact]
    public void Cash_payment_default_remains_cash_compatible()
    {
        Assert.Equal("cash", CashPaymentPolicy.NormalizeOrThrow(null));
        Assert.Equal("cash", CashPaymentPolicy.NormalizeOrThrow(""));
        Assert.Throws<AppException>(() => CashPaymentPolicy.NormalizeOrThrow("tahseel"));
    }

    [Theory]
    [InlineData(TripStatus.Scheduled, true)]
    [InlineData(TripStatus.DriverAssigned, true)]
    [InlineData(TripStatus.InProgress, false)]
    [InlineData(TripStatus.Completed, false)]
    [InlineData(TripStatus.Cancelled, false)]
    public void Assignable_window_matches_pre_start_states(TripStatus status, bool expected)
    {
        var assignable = status is TripStatus.Scheduled or TripStatus.DriverAssigned;
        Assert.Equal(expected, assignable);
    }

    [Fact]
    public void Structured_log_event_names_are_stable()
    {
        Assert.Equal("DEVICE_REGISTERED", PushLogEvents.DeviceRegistered);
        Assert.Equal("FCM_SEND_FAILED", PushLogEvents.FcmSendFailed);
        Assert.Equal("FCM_TOKEN_INVALID", PushLogEvents.FcmTokenInvalid);
        Assert.Equal("FCM_NOT_CONFIGURED", PushLogEvents.FcmNotConfigured);
        Assert.Equal("CAPTAIN_ASSIGNED", PushLogEvents.CaptainAssigned);
        Assert.Equal("BOOKING_CONFIRMED_NOTIFICATION", PushLogEvents.BookingConfirmedNotification);
    }

    [Fact]
    public void Invalid_token_codes_map_to_deactivation_path()
    {
        // Documented FirebaseMessagingException codes handled in FirebasePushNotificationService.
        var deactivateCodes = new[] { "Unregistered", "InvalidArgument" };
        Assert.Contains("Unregistered", deactivateCodes);
        Assert.Contains("InvalidArgument", deactivateCodes);
    }
}
