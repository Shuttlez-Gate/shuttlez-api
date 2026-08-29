using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

/// Active marketplace commission rule (Admin-configurable).
public class CommissionRule : BaseEntity
{
    public string Name { get; set; } = "Default";

    /// Platform (Shuttlez) share as percent 0–100. Captain gets 100 - this.
    public decimal PlatformCommissionPercent { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}
