using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class Driver : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? VehicleId { get; set; }
    public decimal RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public bool IsOnline { get; set; }
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
    public Vehicle? Vehicle { get; set; }
    public ICollection<Trip> Trips { get; set; } = [];
}
