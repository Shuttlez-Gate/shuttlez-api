namespace Shuttlez.Application.RouteRequests.DTOs;

public record RouteRequestOptionsDto(
    IReadOnlyList<string> Cities,
    IReadOnlyDictionary<string, IReadOnlyList<string>> RegionsByCity,
    IReadOnlyList<string> UsageDayOptions,
    IReadOnlyList<string> UsageReasonOptions);

public record CreateRouteRequestDto(
    string FromCity,
    string FromRegion,
    string? FromTime,
    string ToCity,
    string ToRegion,
    string? ToTime,
    int WeeklyCount,
    string? UsageDays,
    string? UsageReason);

public record CreateRouteRequestResponse(
    Guid Id,
    string Message);
