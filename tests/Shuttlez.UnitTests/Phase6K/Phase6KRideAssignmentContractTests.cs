using Microsoft.Extensions.Logging.Abstractions;
using Shuttlez.Application.Bookings;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Notifications;
using Shuttlez.Application.Rides.DTOs;
using Shuttlez.Domain.Enums;

namespace Shuttlez.UnitTests.Phase6K;

/// <summary>Phase 6K — Direct Ride assignment UX contract (unit; not live FCM).</summary>
public class Phase6KRideAssignmentContractTests
{
    [Fact]
    public void RideAssigned_NotificationType_IsStable()
    {
        Assert.Equal("RIDE_ASSIGNED", NotificationTypes.RideAssigned);
    }

    [Fact]
    public void CreateRideRequest_StillCannotSetDriverOrStatusOrFare()
    {
        var props = typeof(CreateRideRequest).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.DoesNotContain("DriverId", props);
        Assert.DoesNotContain("Status", props);
        Assert.DoesNotContain("FareAmount", props);
        Assert.DoesNotContain("TotalAmount", props);
        Assert.DoesNotContain("CommissionAmount", props);
        Assert.DoesNotContain("CaptainEarnings", props);
    }

    [Fact]
    public void AssignRideDriverRequest_IsDriverIdOnly()
    {
        var props = typeof(AssignRideDriverRequest).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(AssignRideDriverRequest.DriverId), props);
        Assert.DoesNotContain("FareAmount", props);
        Assert.DoesNotContain("Status", props);
        Assert.DoesNotContain("RiderUserId", props);
    }

    [Fact]
    public void RideDto_ExposesOptionalCaptainCardFields_WithoutEta()
    {
        var names = typeof(RideDto).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(RideDto.DriverName), names);
        Assert.Contains(nameof(RideDto.DriverPhotoUrl), names);
        Assert.Contains(nameof(RideDto.DriverRatingAverage), names);
        Assert.Contains(nameof(RideDto.DriverRatingCount), names);
        Assert.Contains(nameof(RideDto.VehicleKind), names);
        Assert.Contains(nameof(RideDto.VehicleModel), names);
        Assert.Contains(nameof(RideDto.VehicleColor), names);
        Assert.Contains(nameof(RideDto.PlateNumber), names);
        Assert.DoesNotContain("Eta", names);
        Assert.DoesNotContain("EtaMinutes", names);
        Assert.DoesNotContain("EstimatedArrival", names);
    }

    [Fact]
    public void RideDto_OptionalCaptainFields_DefaultNull_CashPreserved()
    {
        var dto = new RideDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            30, 31, "a",
            30.1, 31.1, "b",
            null, null,
            Guid.NewGuid(),
            RideRequestStatus.Assigned.ToString(),
            99m, 0.1m, 9.9m, 89.1m, 99m,
            CashPaymentPolicy.Cash,
            true,
            Guid.NewGuid(),
            "كابتن",
            DateTime.UtcNow, null, null, null,
            "RD-K",
            DateTime.UtcNow);

        Assert.Equal(CashPaymentPolicy.Cash, dto.PaymentMethod);
        Assert.Equal("كابتن", dto.DriverName);
        Assert.Null(dto.DriverPhotoUrl);
        Assert.Null(dto.PlateNumber);
        Assert.Null(dto.DriverRatingAverage);
    }

    [Fact]
    public void RideStatuses_HaveNoArrivingOrNoDriver()
    {
        var names = Enum.GetNames<RideRequestStatus>().ToHashSet();
        Assert.Contains("Requested", names);
        Assert.Contains("Assigned", names);
        Assert.Contains("InProgress", names);
        Assert.Contains("Completed", names);
        Assert.Contains("Cancelled", names);
        Assert.DoesNotContain("DriverArriving", names);
        Assert.DoesNotContain("NoDriver", names);
    }

    [Fact]
    public async Task RidePushNotifier_FcmFailure_DoesNotThrow()
    {
        var notifier = new RidePushNotifier(
            new ThrowingPushService(),
            NullLogger<RidePushNotifier>.Instance);

        // Soft-fail: assignment path must not see exceptions from FCM.
        await notifier.NotifyRideAssignedAsync(Guid.NewGuid(), Guid.NewGuid());
    }

    private sealed class ThrowingPushService : IPushNotificationService
    {
        public Task SendToUserAsync(
            Guid userId,
            PushNotificationMessage message,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("simulated FCM failure");

        public Task SendToUsersAsync(
            IEnumerable<Guid> userIds,
            PushNotificationMessage message,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("simulated FCM failure");

        public Task SendToDeviceAsync(
            string token,
            PushNotificationMessage message,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("simulated FCM failure");
    }
}
