using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class RouteRequest : BaseEntity
{
    public Guid UserId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public double FromLatitude { get; set; }
    public double FromLongitude { get; set; }
    public double ToLatitude { get; set; }
    public double ToLongitude { get; set; }
    public string Status { get; set; } = "pending";
    public string? Notes { get; set; }

    /// <summary>carshuttle | minibus | bus — تفضيل العميل لنوع المركبة.</summary>
    public string PreferredVehicleType { get; set; } = "minibus";

    public User User { get; set; } = null!;
}
