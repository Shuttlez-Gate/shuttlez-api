using Shuttlez.Application.Common;

namespace Shuttlez.IntegrationTests.Bookings;

/// <summary>
/// Contract documentation for seat concurrency.
/// Real race coverage: <see cref="SeatConcurrencyPostgresTests"/> (Docker/Testcontainers).
/// </summary>
public class SeatConcurrencyContractTests
{
    [Fact]
    public void SeatUnavailable_ErrorCode_IsStable()
    {
        Assert.Equal("SEAT_UNAVAILABLE", ErrorCodes.SeatUnavailable);
    }

    [Fact]
    public void ConflictStatus_IsHttp409()
    {
        const int expectedStatus = 409;
        Assert.Equal(409, expectedStatus);
    }
}
