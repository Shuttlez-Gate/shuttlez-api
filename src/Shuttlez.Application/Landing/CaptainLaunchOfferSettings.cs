namespace Shuttlez.Application.Landing;

public class CaptainLaunchOfferSettings
{
    public const string SectionName = "CaptainLaunchOffer";

    public int TotalSlots { get; set; } = 500;
    public int ProfitPercent { get; set; } = 100;
    public int FirstTrips { get; set; } = 100;
    public int DurationMonths { get; set; } = 3;
}
