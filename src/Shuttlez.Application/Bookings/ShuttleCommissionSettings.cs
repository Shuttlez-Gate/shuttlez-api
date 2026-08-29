namespace Shuttlez.Application.Bookings;

/// Configurable platform commission for Shuttle bookings.
/// PlatformCommissionPercent = 0 → captain receives 100% (launch-friendly default).
public class ShuttleCommissionSettings
{
    public const string SectionName = "ShuttleCommission";

    /// Share kept by Shuttlez (0–100). Captain gets the remainder.
    public decimal PlatformCommissionPercent { get; set; }

    /// When true and CaptainLaunchOffer.ProfitPercent is set, derive platform % as 100 - ProfitPercent.
    public bool PreferLaunchOfferPercent { get; set; } = true;
}
