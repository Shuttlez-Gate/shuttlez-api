using Shuttlez.Application.Common;
using Shuttlez.Application.Rides.DTOs;
using Shuttlez.Application.Rides.Handlers;
using Shuttlez.Domain.Enums;

namespace Shuttlez.UnitTests.Phase6N;

/// Contract / security expectations for Phase 6N captain ride location.
public class Phase6NCaptainLocationContractTests
{
    [Fact]
    public void UpdateRequest_exposes_latitude_longitude_only()
    {
        var req = new UpdateCaptainRideLocationRequest(30.0444, 31.2357);
        Assert.Equal(30.0444, req.Latitude);
        Assert.Equal(31.2357, req.Longitude);
    }

    [Fact]
    public void LocationDto_carries_timestamp_and_status()
    {
        var at = DateTime.UtcNow;
        var dto = new DriverRideLocationDto(
            Guid.NewGuid(),
            30.1,
            31.2,
            at,
            RideRequestStatus.Assigned.ToString());

        Assert.Equal(at, dto.UpdatedAt);
        Assert.Equal("Assigned", dto.Status);
    }

    [Fact]
    public void ErrorCodes_include_location_authorization()
    {
        Assert.Equal("RIDE_LOCATION_NOT_ALLOWED", ErrorCodes.RideLocationNotAllowed);
        Assert.Equal("RIDE_LOCATION_INVALID", ErrorCodes.RideLocationInvalid);
    }

    [Theory]
    [InlineData(RideRequestStatus.Assigned, true)]
    [InlineData(RideRequestStatus.InProgress, true)]
    [InlineData(RideRequestStatus.Requested, false)]
    [InlineData(RideRequestStatus.Completed, false)]
    [InlineData(RideRequestStatus.Cancelled, false)]
    public void Location_allowed_only_while_assigned_or_in_progress(
        RideRequestStatus status,
        bool allowed)
    {
        var canUpdate =
            status is RideRequestStatus.Assigned or RideRequestStatus.InProgress;
        Assert.Equal(allowed, canUpdate);
    }

    [Fact]
    public void UpdateCommand_record_exists()
    {
        var cmd = new UpdateMyDriverRideLocationCommand(Guid.NewGuid(), 1, 2);
        Assert.Equal(1, cmd.Latitude);
        Assert.Equal(2, cmd.Longitude);
    }

    [Fact]
    public void RideDto_includes_optional_captain_location_fields()
    {
        var props = typeof(RideDto).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(RideDto.CaptainLatitude), props);
        Assert.Contains(nameof(RideDto.CaptainLongitude), props);
        Assert.Contains(nameof(RideDto.CaptainLocationUpdatedAt), props);
    }
}
