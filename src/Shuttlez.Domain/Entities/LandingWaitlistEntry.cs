using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class LandingWaitlistEntry : BaseEntity
{
    public string Phone { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Source { get; set; } = "landing";
}
