using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class Stop : BaseEntity
{
    public Guid RouteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Order { get; set; }

    public Route Route { get; set; } = null!;
}
