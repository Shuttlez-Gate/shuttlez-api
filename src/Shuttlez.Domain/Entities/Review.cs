using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class Review : BaseEntity
{
    public Guid TripId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DriverId { get; set; }
    public int Stars { get; set; }
    public string? Comment { get; set; }

    public User User { get; set; } = null!;
}
