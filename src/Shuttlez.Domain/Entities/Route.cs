using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class Route : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public double EndLatitude { get; set; }
    public double EndLongitude { get; set; }
    public string? EncodedPolyline { get; set; }
    public int? DistanceMeters { get; set; }
    public int? DurationSeconds { get; set; }
    public double? BoundsMinLatitude { get; set; }
    public double? BoundsMaxLatitude { get; set; }
    public double? BoundsMinLongitude { get; set; }
    public double? BoundsMaxLongitude { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Stop> Stops { get; set; } = [];
    public ICollection<Trip> Trips { get; set; } = [];
}
