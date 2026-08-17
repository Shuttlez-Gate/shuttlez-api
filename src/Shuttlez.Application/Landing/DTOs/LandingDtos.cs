namespace Shuttlez.Application.Landing.DTOs;

public record PopularRouteDto(string From, string To, int RequestCount);

public record LandingRouteDto(
    Guid Id,
    string From,
    string To,
    int StopsCount,
    string LineCode,
    string BadgeTone,
    string NearestStreet,
    string MeetingPoint,
    int WaitlistCount);

public record LandingMapPointDto(string Label, double Lat, double Lng, string Time);

public record LandingMapStopDto(string Label, double Lat, double Lng, int Order);

public record LandingRouteMapDto(
    Guid Id,
    string From,
    string To,
    LandingMapPointDto Source,
    LandingMapPointDto Destination,
    IReadOnlyList<LandingMapStopDto> Stops);

public record LandingVehicleOptionDto(
    string Value,
    string LabelAr,
    string LabelEn);

public record CaptainLaunchOfferDto(
    int TotalSlots,
    int RegisteredCount,
    int RemainingSlots,
    int ProfitPercent,
    int FirstTrips,
    int DurationMonths);

public record LandingPageConfigDto(
    IReadOnlyList<LandingVehicleOptionDto> VehicleOptions,
    string WaitlistSuccessMessage,
    string CaptainSuccessMessage,
    string RouteRequestSuccessMessage,
    int WaitlistCount,
    CaptainLaunchOfferDto CaptainOffer);

public record LandingRouteLeadDto(
    string Phone,
    string FromCity,
    string FromRegion,
    string? FromTime,
    string ToCity,
    string ToRegion,
    string? ToTime,
    int WeeklyCount,
    string? UsageDays,
    string? UsageReason);

public record LandingWaitlistDto(
    string Phone,
    string? FullName,
    Guid? RouteId = null,
    string? RouteFrom = null,
    string? RouteTo = null);

public record LandingCaptainLeadDto(
    string Phone,
    string? FullName,
    string? VehicleType,
    string? Notes);

public record LandingSubmitResponse(Guid Id, string Message);
