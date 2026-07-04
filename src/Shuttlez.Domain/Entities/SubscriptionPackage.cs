using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class SubscriptionPackage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int TripCount { get; set; }
    public int ValidityDays { get; set; }
    public bool IsActive { get; set; } = true;
}
