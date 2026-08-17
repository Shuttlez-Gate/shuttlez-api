using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Services;

namespace Shuttlez.UnitTests.Admin;

public class SeatDemandDistributorTests
{
    [Fact]
    public void Distribute_UsesPreferredTypeThenEscalates()
    {
        var fleet = new List<FleetAvailabilityDto>
        {
            new(
                "minibus",
                1,
                14,
                [new FleetVehicleItemDto(Guid.NewGuid(), "MB-1", "Hiace", 14, null, null)]),
            new(
                "bus",
                1,
                40,
                [new FleetVehicleItemDto(Guid.NewGuid(), "BUS-1", "Coach", 40, null, null)]),
        };

        var seats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["carshuttle"] = 5,
            ["minibus"] = 10,
        };

        var result = SeatDemandDistributor.Distribute(seats, fleet);

        Assert.Equal(2, result.Count);
        var car = result.Single(r => r.PreferredVehicleType == "carshuttle");
        Assert.Equal(5, car.SeatsCovered);
        Assert.Equal(0, car.SeatsShortfall);
        Assert.Equal("minibus", car.AssignedVehicleType);

        var mini = result.Single(r => r.PreferredVehicleType == "minibus");
        Assert.True(mini.SeatsCovered > 0);
    }

    [Fact]
    public void BuildProposedTrips_CreatesOneTripPerAssignedVehicle()
    {
        var vehicleId = Guid.NewGuid();
        var fleet = new List<FleetAvailabilityDto>
        {
            new(
                "minibus",
                1,
                14,
                [new FleetVehicleItemDto(vehicleId, "MB-1", "Hiace", 14, null, null)]),
        };

        var assignments = SeatDemandDistributor.Distribute(
            new Dictionary<string, int> { ["minibus"] = 8 },
            fleet);

        var trips = SeatDemandDistributor.BuildProposedTrips(assignments, fleet);

        Assert.Single(trips);
        Assert.Equal(vehicleId, trips[0].VehicleId);
        Assert.Equal(8, trips[0].AvailableSeats);
    }
}
