using Shuttlez.Domain.Common;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Domain.Entities;

public class Trip : BaseEntity
{
    public Guid RouteId { get; set; }
    public Guid? DriverId { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Scheduled;
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal PricePerSeat { get; set; }
    public int AvailableSeats { get; set; }
    public string? ReferenceCode { get; set; }

    public Route Route { get; set; } = null!;
    public Driver? Driver { get; set; }
    public ICollection<Booking> Bookings { get; set; } = [];
}
