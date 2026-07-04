using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class LandingRouteLead : BaseEntity
{
    public string Phone { get; set; } = string.Empty;
    public string FromCity { get; set; } = string.Empty;
    public string FromRegion { get; set; } = string.Empty;
    public string? FromTime { get; set; }
    public string ToCity { get; set; } = string.Empty;
    public string ToRegion { get; set; } = string.Empty;
    public string? ToTime { get; set; }
    public int WeeklyCount { get; set; } = 5;
    public string? UsageDays { get; set; }
    public string? UsageReason { get; set; }
    public string Source { get; set; } = "landing";
}
