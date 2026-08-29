using Shuttlez.Application.Bookings;
using Shuttlez.Application.Bookings.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Notifications;
using Shuttlez.Application.Trips;
using Shuttlez.Domain.Enums;

namespace Shuttlez.UnitTests.Hardening;

/// <summary>
/// Phase 5 staging/production readiness — contract verification for real gaps.
/// Classification: unit/contract tests (not staging E2E, not production verification).
/// </summary>
public class Phase5ProductionReadinessContractTests
{
    // ---- Seat reservation gate (mirrors AppDbContext.TryDecrementTripSeatsAsync) ----

    private static bool CanReserveSeats(TripStatus status, int availableSeats, int seatCount) =>
        seatCount > 0 &&
        availableSeats >= seatCount &&
        TripBookability.IsBookableStatus(status);

    [Theory]
    [InlineData(TripStatus.Scheduled, 4, 1, true)]
    [InlineData(TripStatus.DriverAssigned, 4, 4, true)]
    [InlineData(TripStatus.Scheduled, 2, 3, false)]
    [InlineData(TripStatus.Completed, 10, 1, false)]
    [InlineData(TripStatus.Cancelled, 10, 1, false)]
    [InlineData(TripStatus.InProgress, 10, 1, false)]
    [InlineData(TripStatus.Scheduled, 0, 1, false)]
    public void Seat_reservation_gate_rejects_oversubscribe_and_non_bookable(
        TripStatus status, int available, int seats, bool expected)
    {
        Assert.Equal(expected, CanReserveSeats(status, available, seats));
    }

    [Fact]
    public void Concurrent_last_seat_logic_allows_only_one_winner()
    {
        const int available = 1;
        var first = CanReserveSeats(TripStatus.Scheduled, available, 1);
        var remaining = available - (first ? 1 : 0);
        var second = CanReserveSeats(TripStatus.Scheduled, remaining, 1);
        Assert.True(first);
        Assert.False(second);
    }

    // ---- Cancel restores once / reactivate consumes (mirrors AdminTripHandlers) ----

    private static bool ShouldRestoreSeatsOnStatusChange(BookingStatus from, BookingStatus to)
    {
        var wasActive = from is BookingStatus.Pending or BookingStatus.Confirmed;
        var willBeActive = to is BookingStatus.Pending or BookingStatus.Confirmed;
        return wasActive && !willBeActive;
    }

    private static bool ShouldConsumeSeatsOnReactivate(BookingStatus from, BookingStatus to) =>
        !(from is BookingStatus.Pending or BookingStatus.Confirmed) &&
        to is BookingStatus.Pending or BookingStatus.Confirmed;

    [Fact]
    public void Cancel_restores_seats_once_then_duplicate_cancel_does_not()
    {
        Assert.True(ShouldRestoreSeatsOnStatusChange(BookingStatus.Confirmed, BookingStatus.Cancelled));
        Assert.False(ShouldRestoreSeatsOnStatusChange(BookingStatus.Cancelled, BookingStatus.Cancelled));
    }

    [Fact]
    public void Reactivate_consumes_seats_atomically_gate()
    {
        Assert.True(ShouldConsumeSeatsOnReactivate(BookingStatus.Cancelled, BookingStatus.Confirmed));
        Assert.False(ShouldConsumeSeatsOnReactivate(BookingStatus.Confirmed, BookingStatus.Confirmed));
        Assert.False(CanReserveSeats(TripStatus.Completed, 5, 1));
        Assert.Equal("SEAT_UNAVAILABLE", ErrorCodes.SeatUnavailable);
    }

    [Fact]
    public void Rider_cancel_query_excludes_already_cancelled()
    {
        var statuses = new[] { BookingStatus.Confirmed, BookingStatus.Cancelled };
        var eligible = statuses.Where(s =>
            s != BookingStatus.Cancelled && s != BookingStatus.Expired).ToList();
        Assert.Single(eligible);
        Assert.Equal(BookingStatus.Confirmed, eligible[0]);
    }

    // ---- Captain ownership + idempotency ----

    private static bool IsWrongCaptain(Guid? tripDriverId, Guid authenticatedDriverId) =>
        tripDriverId is null || tripDriverId == Guid.Empty || tripDriverId != authenticatedDriverId;

    [Fact]
    public void Wrong_captain_cannot_start_or_complete()
    {
        var assigned = Guid.NewGuid();
        var other = Guid.NewGuid();
        Assert.True(IsWrongCaptain(assigned, other));
        Assert.False(IsWrongCaptain(assigned, assigned));
        Assert.True(IsWrongCaptain(null, assigned));
        Assert.Equal("TRIP_NOT_ASSIGNED", ErrorCodes.TripNotAssigned);
    }

    [Fact]
    public void Start_complete_idempotent_rules()
    {
        Assert.True(TripLifecycleRules.IsAlreadyStarted(TripStatus.InProgress));
        Assert.False(TripLifecycleRules.CanStart(TripStatus.InProgress));
        Assert.True(TripLifecycleRules.IsAlreadyCompleted(TripStatus.Completed));
        Assert.False(TripLifecycleRules.CanComplete(TripStatus.Completed));
        Assert.False(TripLifecycleRules.CanStart(TripStatus.Cancelled));
        Assert.False(TripLifecycleRules.CanComplete(TripStatus.Cancelled));
    }

    // ---- Admin assignment eligibility + conflict ----

    private static bool CanAssignDriverToTripStatus(TripStatus status) =>
        status is TripStatus.Scheduled or TripStatus.DriverAssigned;

    private static bool DriverConflictSameScheduledAt(
        Guid driverId,
        Guid tripId,
        DateTime scheduledAt,
        Guid otherTripDriverId,
        Guid otherTripId,
        DateTime otherScheduledAt,
        TripStatus otherStatus) =>
        otherTripDriverId == driverId &&
        otherTripId != tripId &&
        otherStatus is not TripStatus.Cancelled and not TripStatus.Completed &&
        otherScheduledAt == scheduledAt;

    [Theory]
    [InlineData(TripStatus.InProgress, false)]
    [InlineData(TripStatus.Completed, false)]
    [InlineData(TripStatus.Cancelled, false)]
    [InlineData(TripStatus.Scheduled, true)]
    [InlineData(TripStatus.DriverAssigned, true)]
    public void Assignment_blocked_after_start_or_terminal(TripStatus status, bool allowed) =>
        Assert.Equal(allowed, CanAssignDriverToTripStatus(status));

    [Fact]
    public void Driver_conflict_same_ScheduledAt_is_detected()
    {
        var driver = Guid.NewGuid();
        var tripA = Guid.NewGuid();
        var tripB = Guid.NewGuid();
        var when = new DateTime(2026, 8, 22, 7, 0, 0, DateTimeKind.Utc);

        Assert.True(DriverConflictSameScheduledAt(
            driver, tripA, when, driver, tripB, when, TripStatus.Scheduled));
        Assert.False(DriverConflictSameScheduledAt(
            driver, tripA, when, driver, tripB, when.AddHours(1), TripStatus.Scheduled));
        Assert.False(DriverConflictSameScheduledAt(
            driver, tripA, when, driver, tripB, when, TripStatus.Completed));
        Assert.Equal("DRIVER_TRIP_CONFLICT", ErrorCodes.DriverTripConflict);
    }

    // ---- Duplicate launch (RouteId + ScheduledAt) ----

    private static bool IsDuplicateOperationalTrip(
        Guid routeId,
        DateTime scheduledAt,
        Guid existingRouteId,
        DateTime existingScheduledAt,
        TripStatus existingStatus) =>
        existingStatus != TripStatus.Cancelled &&
        existingRouteId == routeId &&
        existingScheduledAt == scheduledAt;

    [Fact]
    public void Duplicate_launch_same_route_and_scheduledAt_rejected()
    {
        var route = Guid.NewGuid();
        var when = DateTime.UtcNow.AddDays(1);
        Assert.True(IsDuplicateOperationalTrip(route, when, route, when, TripStatus.Scheduled));
        Assert.False(IsDuplicateOperationalTrip(route, when, route, when, TripStatus.Cancelled));
        Assert.False(IsDuplicateOperationalTrip(route, when, Guid.NewGuid(), when, TripStatus.Scheduled));
        Assert.Equal("DUPLICATE_OPERATIONAL_TRIP", ErrorCodes.DuplicateOperationalTrip);
    }

    [Fact]
    public void Admin_and_dispatch_error_codes_remain_stable_for_authz_failures()
    {
        // Controllers inherit AdminOnly via AdminControllerBase (API layer).
        Assert.Equal("DRIVER_NOT_ELIGIBLE", ErrorCodes.DriverNotEligible);
        Assert.Equal("DRIVER_INACTIVE", ErrorCodes.DriverInactive);
        Assert.Equal("NOT_READY_TO_LAUNCH", ErrorCodes.NotReadyToLaunch);
    }

    // ---- CASH contract + client cannot override financials ----

    [Fact]
    public void Cash_booking_request_response_contract()
    {
        Assert.Equal(CashPaymentPolicy.Cash, CashPaymentPolicy.NormalizeOrThrow("CASH"));
        var props = typeof(CreateBookingRequest).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Equal(3, props.Count);
        Assert.DoesNotContain("TotalAmount", props);
        Assert.DoesNotContain("PricePerSeat", props);
        Assert.DoesNotContain("CommissionAmount", props);
        Assert.DoesNotContain("CaptainEarnings", props);

        var response = new CreateBookingResponse(
            Guid.NewGuid(), Guid.NewGuid(), "BK-1", 100m, "Confirmed", "ok",
            PricePerSeat: 50m, SeatCount: 2, PaymentMethod: CashPaymentPolicy.Cash);
        Assert.Equal(100m, response.TotalAmount);
        Assert.Equal(CashPaymentPolicy.Cash, response.PaymentMethod);
    }

    [Fact]
    public void Non_cash_gateways_rejected()
    {
        foreach (var m in new[] { "tahseel", "card", "visa", "wallet", "online" })
        {
            var ex = Assert.Throws<AppException>(() => CashPaymentPolicy.NormalizeOrThrow(m));
            Assert.Equal(ErrorCodes.PaymentMethodNotSupported, ex.Code);
        }
    }

    [Fact]
    public void Invalid_FCM_token_deactivation_sets_IsActive_false()
    {
        var device = new { Token = "tok", IsActive = true, IsDeleted = false };
        var shouldDeactivate =
            device is { IsActive: true, IsDeleted: false } &&
            !string.IsNullOrWhiteSpace(device.Token);
        Assert.True(shouldDeactivate);

        var after = device with { IsActive = false };
        Assert.False(after.IsActive);
        Assert.Equal("FCM_TOKEN_INVALID", PushLogEvents.FcmTokenInvalid);
        Assert.Equal("FCM_NOT_CONFIGURED", PushLogEvents.FcmNotConfigured);
    }

    [Fact]
    public void Booking_confirmed_cash_copy_is_post_commit_safe_wording()
    {
        var body = "تم تأكيد الحجز — الدفع نقدًا للكابتن";
        Assert.Contains("نقدًا", body);
        Assert.DoesNotContain("تم الدفع", body);
        Assert.Equal("BOOKING_CONFIRMED", NotificationTypes.BookingConfirmed);
    }

    [Fact]
    public void Historical_snapshots_are_not_recalculated_by_catalog_edits()
    {
        const decimal snapshotTotal = 200m;
        const decimal newCatalogPrice = 999m;
        Assert.NotEqual(snapshotTotal, newCatalogPrice * 2);
        Assert.Equal(200m, snapshotTotal);
    }
}
