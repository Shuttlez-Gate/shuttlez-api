using Shuttlez.Application.Common;
using Shuttlez.Application.CustomerTrips.Commands;
using Shuttlez.Application.CustomerTrips.DTOs;

namespace Shuttlez.UnitTests.Phase6A;

/// <summary>Phase 6A — customer-trips soft-deprecation (not a Ride contract).</summary>
public class CustomerTripsDeprecationTests
{
    [Fact]
    public async Task CreateCustomerTrip_IsSoftDeprecated()
    {
        var handler = new CreateCustomerTripHandler();
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            handler.Handle(
                new CreateCustomerTripCommand(
                    new CreateCustomerTripRequest(30, 31, 30.1, 31.1, null, null, null)),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.CustomerTripsDeprecated, ex.Code);
        Assert.Equal(410, ex.StatusCode);
    }

    [Fact]
    public void ErrorCode_CustomerTripsDeprecated_IsStable()
    {
        Assert.Equal("CUSTOMER_TRIPS_DEPRECATED", ErrorCodes.CustomerTripsDeprecated);
    }
}

/// <summary>
/// Documents STOP conditions: PricingRule cannot price ad-hoc Ride; Group has no product model.
/// Classification: contract documentation tests — not live Ride/Group E2E.
/// </summary>
public class Phase6AFoundationStopConditionTests
{
    [Fact]
    public void PricingRule_IsRouteVehicleCatalog_NotOnDemandOdFare()
    {
        var props = typeof(Shuttlez.Domain.Entities.PricingRule).GetProperties()
            .Select(p => p.Name)
            .ToHashSet();
        Assert.Contains(nameof(Shuttlez.Domain.Entities.PricingRule.RouteId), props);
        Assert.Contains(nameof(Shuttlez.Domain.Entities.PricingRule.VehicleType), props);
        Assert.Contains(nameof(Shuttlez.Domain.Entities.PricingRule.OneWayPrice), props);
        Assert.DoesNotContain("PricePerKm", props);
        Assert.DoesNotContain("DistanceMeters", props);
        Assert.DoesNotContain("SurgeMultiplier", props);
        Assert.DoesNotContain("ProductKind", props);
    }

    [Fact]
    public void Booking_HasNoRideOrGroupDiscriminator()
    {
        var props = typeof(Shuttlez.Domain.Entities.Booking).GetProperties()
            .Select(p => p.Name)
            .ToHashSet();
        Assert.Contains(nameof(Shuttlez.Domain.Entities.Booking.TripId), props);
        Assert.Contains(nameof(Shuttlez.Domain.Entities.Booking.PaymentMethod), props);
        Assert.DoesNotContain("RideId", props);
        Assert.DoesNotContain("GroupId", props);
        Assert.DoesNotContain("ProductKind", props);
    }

    [Fact]
    public void RouteDemandGroupState_IsNotGroupBookingEntity()
    {
        var props = typeof(Shuttlez.Domain.Entities.RouteDemandGroupState).GetProperties()
            .Select(p => p.Name)
            .ToHashSet();
        Assert.Contains(nameof(Shuttlez.Domain.Entities.RouteDemandGroupState.RouteKey), props);
        Assert.DoesNotContain("Members", props);
        Assert.DoesNotContain("OrganizerUserId", props);
        Assert.DoesNotContain("MaxPassengers", props);
    }
}
