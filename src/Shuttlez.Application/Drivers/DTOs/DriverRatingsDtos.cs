namespace Shuttlez.Application.Drivers.DTOs;

public record DriverRatingsSummaryDto(
    double AverageRating,
    int TotalTrips,
    double FiveStarRatio,
    IReadOnlyDictionary<int, int> Breakdown,
    IReadOnlyList<RatingTagStatDto> Tags,
    IReadOnlyList<CustomerReviewDto> Reviews);

public record RatingTagStatDto(string Key, string Label, int Count);

public record CustomerReviewDto(
    int Stars,
    string Comment,
    string DateLabel,
    Guid TripId,
    DateTime CreatedAt);
