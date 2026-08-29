using Shuttlez.Application.Bookings;
using Shuttlez.Application.Common;
using Shuttlez.Application.Groups.DTOs;
using Shuttlez.Application.Rides.DTOs;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.UnitTests.Phase6C;

/// <summary>Phase 6C — Ride + Group contract tests (unit/contract; no full-backend mocks).</summary>
public class Phase6CRideGroupContractTests
{
    [Fact]
    public void CreateRideRequest_ExposesNoClientFinancialFields()
    {
        var props = typeof(CreateRideRequest).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(CreateRideRequest.PickupLatitude), props);
        Assert.Contains(nameof(CreateRideRequest.DestinationLatitude), props);
        Assert.Contains(nameof(CreateRideRequest.PaymentMethod), props);
        Assert.DoesNotContain("FareAmount", props);
        Assert.DoesNotContain("TotalAmount", props);
        Assert.DoesNotContain("CommissionAmount", props);
        Assert.DoesNotContain("CommissionRate", props);
        Assert.DoesNotContain("CaptainEarnings", props);
        Assert.DoesNotContain("DriverId", props);
        Assert.DoesNotContain("Status", props);
        Assert.DoesNotContain("RiderUserId", props);
        Assert.DoesNotContain("UserId", props);
    }

    [Fact]
    public void CreateGroupRequest_ExposesNoClientFinancialFields()
    {
        var props = typeof(CreateGroupRequest).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(CreateGroupRequest.Capacity), props);
        Assert.Contains(nameof(CreateGroupRequest.PickupLatitude), props);
        Assert.DoesNotContain("FareAmount", props);
        Assert.DoesNotContain("TotalAmount", props);
        Assert.DoesNotContain("CommissionAmount", props);
        Assert.DoesNotContain("DriverId", props);
        Assert.DoesNotContain("OrganizerUserId", props);
    }

    [Fact]
    public void ConfirmGroupCashRequest_CashOnlyShape()
    {
        var props = typeof(ConfirmGroupCashRequest).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(ConfirmGroupCashRequest.PaymentMethod), props);
        Assert.DoesNotContain("TotalAmount", props);
        Assert.DoesNotContain("FareAmount", props);
    }

    [Fact]
    public void RideDto_ContainsAuthoritativeFinancialSnapshot()
    {
        var dto = new RideDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            30, 31, "a",
            30.1, 31.1, "b",
            null, null,
            Guid.NewGuid(),
            RideRequestStatus.Requested.ToString(),
            200m, 0.1m, 20m, 180m, 200m,
            CashPaymentPolicy.Cash,
            true,
            null, null,
            null, null, null, null,
            "RD-1",
            DateTime.UtcNow);

        Assert.Equal(200m, dto.TotalAmount);
        Assert.Equal(CashPaymentPolicy.Cash, dto.PaymentMethod);
        Assert.True(dto.IsCashConfirmed);
    }

    [Theory]
    [InlineData("tahseel")]
    [InlineData("card")]
    [InlineData("wallet")]
    [InlineData("online")]
    public void CashPolicy_RejectsOnlineForRideGroupPath(string raw)
    {
        var ex = Assert.Throws<AppException>(() => CashPaymentPolicy.NormalizeOrThrow(raw));
        Assert.Equal(ErrorCodes.PaymentMethodNotSupported, ex.Code);
    }

    [Fact]
    public void SplitEarnings_ServerOwned_NoHardcodedPercentsInCalculator()
    {
        var (rate, commission, captain) = ShuttleFinancialCalculator.SplitEarnings(200m, 10m);
        Assert.Equal(0.10m, rate);
        Assert.Equal(20m, commission);
        Assert.Equal(180m, captain);
    }

    [Fact]
    public void RideFareRule_HasNoHardcodedSeedConstants()
    {
        var rule = new RideFareRule();
        Assert.Equal(0m, rule.FlatFare);
        Assert.Equal(0m, rule.BaseFare);
        Assert.Equal(0m, rule.PricePerKm);
        Assert.True(rule.IsActive);
    }

    [Fact]
    public void GroupFareRule_RequiresAdminConfiguredMaxMembersDefaultSafe()
    {
        var rule = new GroupFareRule();
        Assert.Equal(0m, rule.CharterFlatFare);
        Assert.Equal(0m, rule.BaseFare);
        Assert.Equal(0m, rule.PricePerKm);
        Assert.Equal(1, rule.MaxMembers);
    }

    [Fact]
    public void ErrorCodes_Phase6C_AreStable()
    {
        Assert.Equal("RIDE_FARE_NOT_CONFIGURED", ErrorCodes.RideFareNotConfigured);
        Assert.Equal("GROUP_FARE_NOT_CONFIGURED", ErrorCodes.GroupFareNotConfigured);
        Assert.Equal("GROUP_MEMBERSHIP_LOCKED", ErrorCodes.GroupMembershipLocked);
        Assert.Equal("GROUP_CAPACITY_EXCEEDED", ErrorCodes.GroupCapacityExceeded);
        Assert.Equal("GROUP_DUPLICATE_MEMBER", ErrorCodes.GroupDuplicateMember);
        Assert.Equal("RIDE_NOT_ASSIGNED", ErrorCodes.RideNotAssigned);
        Assert.Equal("CUSTOMER_TRIPS_DEPRECATED", ErrorCodes.CustomerTripsDeprecated);
    }

    [Fact]
    public void RideLifecycle_EnumMinimumStatuses()
    {
        Assert.Equal(1, (int)RideRequestStatus.Requested);
        Assert.Equal(2, (int)RideRequestStatus.Assigned);
        Assert.Equal(3, (int)RideRequestStatus.InProgress);
        Assert.Equal(4, (int)RideRequestStatus.Completed);
        Assert.Equal(5, (int)RideRequestStatus.Cancelled);
    }

    [Fact]
    public void GroupLifecycle_EnumMinimumStatuses()
    {
        Assert.Equal(1, (int)GroupRequestStatus.Draft);
        Assert.Equal(2, (int)GroupRequestStatus.Confirmed);
        Assert.Equal(3, (int)GroupRequestStatus.Assigned);
        Assert.Equal(4, (int)GroupRequestStatus.InProgress);
        Assert.Equal(5, (int)GroupRequestStatus.Completed);
        Assert.Equal(6, (int)GroupRequestStatus.Cancelled);
    }

    [Fact]
    public void RouteDemandGroupState_IsNotGroupBookingEntity()
    {
        var props = typeof(RouteDemandGroupState).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(RouteDemandGroupState.RouteKey), props);
        Assert.DoesNotContain("OrganizerUserId", props);
        Assert.DoesNotContain("Members", props);
        Assert.DoesNotContain("Capacity", props);
        Assert.DoesNotContain("TotalAmount", props);
    }

    [Fact]
    public void GroupRequest_HasMembershipLockAndOrganizerFields()
    {
        var props = typeof(GroupRequest).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(GroupRequest.OrganizerUserId), props);
        Assert.Contains(nameof(GroupRequest.MembershipLocked), props);
        Assert.Contains(nameof(GroupRequest.JoinedMemberCount), props);
        Assert.Contains(nameof(GroupRequest.Capacity), props);
        Assert.Contains(nameof(GroupRequest.IsCashConfirmed), props);
    }

    [Fact]
    public void RideAndGroupHandlers_AreRegisteredInApplicationAssembly()
    {
        var asm = typeof(CreateRideRequest).Assembly;
        var names = asm.GetTypes().Select(t => t.Name).ToHashSet();
        Assert.Contains("RideHandlers", names);
        Assert.Contains("GroupHandlers", names);
        Assert.Contains("AdminRideFareHandlers", names);
        Assert.Contains("AdminGroupFareHandlers", names);
        Assert.Contains("DriverRideHandlers", names);
        Assert.Contains("DriverGroupHandlers", names);
    }
}
