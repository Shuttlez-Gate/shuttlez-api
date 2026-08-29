using Microsoft.EntityFrameworkCore;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;
using Shuttlez.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace Shuttlez.IntegrationTests.Bookings;

/// <summary>
/// Real PostgreSQL concurrency against atomic seat decrement.
/// Requires Docker (Testcontainers) OR env <c>SHUTTLEZ_INTEGRATION_PG</c> pointing at an
/// isolated database. Does not use InMemory. Skips when infrastructure is unavailable.
/// </summary>
public class SeatConcurrencyPostgresTests
{
    [Fact]
    public async Task ConcurrentLastSeat_OnlyOneSucceeds()
    {
        var externalCs = Environment.GetEnvironmentVariable("SHUTTLEZ_INTEGRATION_PG");
        string connectionString;

        if (!string.IsNullOrWhiteSpace(externalCs))
        {
            connectionString = externalCs;
        }
        else
        {
            PostgreSqlContainer? postgres = null;
            try
            {
                postgres = new PostgreSqlBuilder()
                    .WithImage("postgres:16-alpine")
                    .Build();
                await postgres.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "SKIPPED: Requires isolated PostgreSQL integration environment " +
                    $"(Docker/Testcontainers or SHUTTLEZ_INTEGRATION_PG). {ex.Message}");
                return;
            }

            await using (postgres)
            {
                connectionString = postgres.GetConnectionString();
                await RunConcurrencyAssertionAsync(connectionString);
            }

            return;
        }

        await RunConcurrencyAssertionAsync(connectionString);
    }

    private static async Task RunConcurrencyAssertionAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var setup = new AppDbContext(options);
        await setup.Database.EnsureCreatedAsync();

        var route = new Route
        {
            Id = Guid.NewGuid(),
            Name = "Concurrency Route",
            StartLatitude = 30,
            StartLongitude = 31,
            EndLatitude = 30.1,
            EndLongitude = 31.1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        setup.RoutesSet.Add(route);

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            RouteId = route.Id,
            Status = TripStatus.Scheduled,
            ScheduledAt = DateTime.UtcNow.AddHours(3),
            PricePerSeat = 100m,
            AvailableSeats = 1,
            ReferenceCode = "CONC-1",
            CreatedAt = DateTime.UtcNow
        };
        setup.TripsSet.Add(trip);
        await setup.SaveChangesAsync();

        var tripId = trip.Id;

        async Task<int> AttemptAsync()
        {
            await using var db = new AppDbContext(options);
            return await db.TryDecrementTripSeatsAsync(tripId, 1);
        }

        var results = await Task.WhenAll(AttemptAsync(), AttemptAsync());

        Assert.Equal(1, results.Count(r => r == 1));
        Assert.Equal(1, results.Count(r => r == 0));

        await using var verify = new AppDbContext(options);
        var seats = await verify.TripsSet
            .Where(t => t.Id == tripId)
            .Select(t => t.AvailableSeats)
            .SingleAsync();
        Assert.Equal(0, seats);
    }
}
